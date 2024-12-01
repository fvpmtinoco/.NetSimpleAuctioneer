# .NetSimpleAuctioneer

## Key Design Features

### 1. Vertical Slice Architecture (VSA)
The project employs [Vertical Slice Architecture (VSA)](https://www.milanjovanovic.tech/blog/vertical-slice-architecture) to organize all feature-specific logic (e.g., commands, handlers, models, validations) into modular slices. This approach ensures a maintainable and cohesive structure.

- **Self-Contained Endpoints**: Each endpoint (e.g., `AddTruck`, `AddSUV`, `StartAuction`) is fully self-contained, minimizing dependencies and simplifying development.
- **VSA** was chosen over Clean Architecture due to its lower overhead, making it better suited for this project's scope, which includes only eight endpoints and relatively straightforward business logic.
- **When to Consider Clean Architecture**: For projects with more complex requirements, Clean Architecture might be a more appropriate choice.

**Shared Resources**:  
Common services, repositories, and shared logic are housed in a dedicated `Shared` folder, fostering reusability and consistency across features.
