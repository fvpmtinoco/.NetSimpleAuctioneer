# .NetSimpleAuctioneer

## Assumptions
1. A vehicle can participate in multiple auctions, as long as these auctions occur at different times. For example, once an auction closes, the vehicle is considered "released" and can be placed in another auction at a later time.
2. The system will maintain only two core functionalities, auction and vehicle management. No additional domains will be introduced.
3. Search results will exclude vehicles associated with closed auctions, unless they are also part of another open auction.

## Key Design Features ##

### Vertical Slice Architecture (VSA)
The project employs [Vertical Slice Architecture (VSA)](https://www.milanjovanovic.tech/blog/vertical-slice-architecture) to organize all feature-specific logic (e.g., commands, handlers, models, validations) into modular slices. This approach ensures a maintainable and cohesive structure.

- **Self-Contained Endpoints**: Each endpoint (e.g., `AddTruck`, `AddSUV`, `StartAuction`) is fully self-contained, minimizing dependencies and simplifying development.
- **VSA** was chosen over Clean Architecture due to its lower overhead, making it better suited for this project's scope, which includes only eight endpoints and relatively straightforward business logic.
- **Shared Resources**: Common services, repositories, and shared logic are housed in a dedicated `Shared` folder, intended for reusability and consistency across features.

  ### Why was not Clean Architecture applied? ###
  - Although considered, given assumption #2, I considered VSA a suitable choice for this scenario. However, if the complexity was to increase beyond this scope, Clean Architecture would help enforce better separation of concerns to avoid tight coupling between layers, ensuring that the core business logic remains stable even as the application layer evolves more frequently.

### Resilience in Database Connection (Polly)
The project uses Polly for resilience, implementing retry and circuit breaker policies for handling transient database failures. Retry attempts are made a set number of times with exponential backoff, while the circuit breaker prevents further retries after repeated failures.

### Mediator Pattern (Mediatr)
The Mediator pattern is implemented using Mediatr, which decouples components by sending requests through a mediator. Each request (e.g., AddTruckCommand) is handled by a dedicated handler, ensuring a clear separation of concerns and making the codebase more maintainable.
Additionally, a logging pipeline behavior was introduced, which logs the requests and responses for better traceability and debugging.


## Improvements to consider

### **1. Separation of Vehicle Addition with Unified Entity for Storage**
Vehicles are added through separate endpoints for each type (e.g., `AddTruck`, `AddSedan`), while all vehicle data is stored in a single `Vehicle` entity that includes both common and specific properties. 
For this purpose a relational database (PostgreSQL) was chosen for simplicity. Tried to establish a clear separation of concerns, allowing the application layer to remain unchanged if the data persistence model is switched.

  ##### Benefits:
  - **Separate Endpoints**: Each vehicle type has its own endpoint, ensuring only relevant fields are sent.
  - **Simplified Validation**: Each vehicle type is validated individually.
  - **Single Storage Entity**: A single `Vehicle` entity stores all vehicle data, simplifying the database schema.
  
  ##### Drawbacks:
  - **Redundant Fields**: Some fields in the entity may remain empty for certain vehicle types, leading to inefficiency.
  - **Mapping Complexity**: Mapping vehicle-specific data to a generic entity can become complex as new vehicle types are added.
  - **Scalability**: Adding more vehicle types requires modifying the entity, mappings, and endpoints.
  
  ##### Possible Refactoring:
  - **Separate Tables**: In a relational database, using a single `Vehicle` table to store all vehicle types can result in redundant fields. A possible improvement would be to use separate tables for each vehicle type (e.g., `Truck`, `Sedan`) to avoid unnecessary fields in the `Vehicle` table.
  
  ##### NoSQL Approach (Alternative):
  - If a **NoSQL database** was used (e.g., MongoDB, DynamoDB), the approach would differ. Each vehicle type could be stored in its own collection (e.g., `Trucks`, `Sedans`, `SUVs`), with documents containing only the relevant fields for that vehicle type. This would avoid redundancy in data storage, as each document could have a flexible schema tailored to the specific vehicle type. NoSQL would allow for a more scalable, schema-less design, where each vehicle type can evolve independently without affecting others. However, the search vehicles use case could be more complex, as would require careful planning for queries across different collections

### **2. Concurrency**
The current implementation is not prepared for high concurrency, especially in the bidding use case. Using multiple instances of the API, in the current scenario if two bidders place a bid almost simultaneously, they both can be notified that their bid was inserted correctly. On possible approach was to implement a queue base system, similar to this:
  
  - Bid Submission:
    A user submits a bid, which is then added to a message queue (e.g., a RabbitMQ, AWS SQS queue, ...). The bid enters the queue, awaiting processing. 
  - Bid Processing:
    A worker process (or multiple workers) pulls the bid from the queue and checks whether it is the highest bid. The worker retrieves the current highest bid from the database. Makes the validations. If the bid is higher, it inserts the bid into the database. If the bid is not higher, it rejects the      bid (e.g., by returning an error to the user or logging it). If multiple workers are to process the queue in parallel, have to ensure that each worker processes one bid at a time to avoid concurrency issues.
    Sequential
  - Handling: Since the queue processes bids in the order they were received, there’s a high mitigation of two bids conflicting when being inserted into the database. The system will handle the bids one at a time, in the correct order.
  - Feedback to Users: Once the bid is processed and inserted, the worker can notify the user whether their bid was successful or not (via callback, event, or polling).

### **3. Caching**
Caching was not implemented due to time constraints, but it would be a logical approach in the search functionality, assuming filter fields are limited to predefined options (not free text search). In a distributed system, using a caching solution like Redis could be highly beneficial.
Implementing caching would improve frequently requested search results, especially those with common filters. 
Incorporating cache expiration or invalidation strategies would ensure that the data remains reasonably up to date without causing excessive load on the database. Proper cache invalidation mechanisms would be necessary to handle changes in the underlying data. In this API, where only adding vehicles is possible, invalidation would have to occur after adding a new vehicle and closing an auction. Fine grained cache invalidation could be considered in this scenario, depending on its complexity.

### **4. Validations**
This API uses ASP.NET Core's built-in validation with Data Annotations. Considered the approach suitable for this project because it is a simple API with no complex validation rules. 
All validation logic is defined directly in the model classes using attributes like `[Required]`, `[EmailAddress]`, and `[Range]`. This keeps the implementation straightforward and easy to maintain.
For more complex scenarios or reusable validation logic, external libraries like FluentValidation could be considered in future updates. Due to lack of time, I implemented validations in a straightforward manner, querying the repository. Fluent validations could also be used in validations like checking for highest bid, existance of vehicle,... This would keep validation separate from business logic.

### **5. Pagination**
Pagination is used in the search vehicle functionality. However, due to lack of time, it does not provide feedback to client regarding total count records neither total pages. It also don't retrieve the specific attributes for each vehicle.
Also, if a vehicle is inserted between the fetching, it might not be returned. In this case, I considered a vehicle listing does not need to reflect real-time accuracy or immediate consistency.

### **6. Tests**
Extensive unit and integration tests were written that did their purpose: Detect issues and certifying that some refactors made did not break the functionalities. However the test code coverage could be improved. Due to lack of time not all implementations are backed with tests.  

### **7. Problematic coupling**
Although using VSA, tried to keep Clean Architecture principles in mind. And there's at least one issue that arises. Using the same classes of result (SuccessOrError and VoidOrError) across all layers (application, and domain) is generally not considered ideal because it can create tight coupling between layers and break the principle of separation of concerns. Particularly the domain layer that should remain decoupled from any concerns about how data is presented. 
The ideal was the repository to have its own response object, to be mapped to the application's own result object.



