# .NetSimpleAuctioneer

## Assumptions
1. A vehicle can participate in multiple auctions, as long as these auctions occur at different times. For example, once an auction closes, the vehicle is considered "released" and can be placed in another auction at a later time.
2. The system will maintain only two core domains: Vehicles and Auction. No additional domains will be introduced.

## Key Design Features

### 1. Vertical Slice Architecture (VSA)
The project employs [Vertical Slice Architecture (VSA)](https://www.milanjovanovic.tech/blog/vertical-slice-architecture) to organize all feature-specific logic (e.g., commands, handlers, models, validations) into modular slices. This approach ensures a maintainable and cohesive structure.

- **Self-Contained Endpoints**: Each endpoint (e.g., `AddTruck`, `AddSUV`, `StartAuction`) is fully self-contained, minimizing dependencies and simplifying development.
- **VSA** was chosen over Clean Architecture due to its lower overhead, making it better suited for this project's scope, which includes only eight endpoints and relatively straightforward business logic.
- **When to Consider Clean Architecture**: For projects with more complex requirements, Clean Architecture might be a more appropriate choice.
- **Shared Resources**:  
Common services, repositories, and shared logic are housed in a dedicated `Shared` folder, intended for reusability and consistency across features.

### 2. Resilience in Database Connection (Polly)
The project uses Polly for resilience, implementing retry and circuit breaker policies for handling transient database failures. Retry attempts are made a set number of times with exponential backoff, while the circuit breaker prevents further retries after repeated failures.

### 3. Mediator Pattern (Mediatr)
The Mediator pattern is implemented using Mediatr, which decouples components by sending requests through a mediator. Each request (e.g., AddTruckCommand) is handled by a dedicated handler, ensuring a clear separation of concerns and making the codebase more maintainable.
Additionally, a logging pipeline behavior was introduced, which logs the requests and responses for better traceability and debugging.


## Improvements to consider
### Concurrency
#### **Current Implementation**
The system ensures integrity and consistency using **database transactions**. This approach:
- Validates the bid amount and ensures it exceeds the current highest bid.
- Inserts the bid into the database atomically within a single transaction, preventing race conditions.
- Is effective for moderate traffic scenarios with a single API instance.
- Provides immediate feedback (bid accepted/rejected)

#### **Scalable Architecture Consideration**
For distributed and highly scalable scenarios in cloud environments, an alternative approach is to use a queue messaging system. In AWS services, **AWS SQS** and **Lambda functions** could be a good option:
- The API publishes bid requests to an SQS queue.
- A Lambda function validates and processes bids asynchronously, ensuring consistency and scalability.
- With this approach, maybe use SNS, or a push notification service to inform the user if the bid was successfully (or not) processed. 


While the current implementation maybe sufficient for most use cases, **SQS + Lambda** is better suited for systems with high traffic and distributed infrastructure.

### Validations
#### **Current Implementation**
This API uses ASP.NET Core's built-in validation with Data Annotations. Considered the approach suitable for this project because it is a simple API with no complex validation rules. 
All validation logic is defined directly in the model classes using attributes like `[Required]`, `[EmailAddress]`, and `[Range]`. This keeps the implementation straightforward and easy to maintain.

### **More complex validations***
For more complex scenarios or reusable validation logic, external libraries like FluentValidation could be considered in future updates.



