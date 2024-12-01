# .NetSimpleAuctioneer

# Key Design Features
## 1. Vertical Slice Architecture (VSA):
	The project employs Vertical Slice Architecture (VSA) to organize all feature-specific logic (commands, handlers, models, validations, etc.) into modular slices. This approach ensures a maintainable and cohesive structure.

	Each endpoint (e.g., AddTruck, AddSUV, StartAuction) is fully self-contained, minimizing dependencies and simplifying development. VSA was chosen over Clean Architecture because it provides a lower overhead solution better suited to the project's scope, which includes only eight endpoints and relatively straightforward business logic. For projects with more complex requirements, Clean Architecture might be a more appropriate choice.

	Common services, repositories, and shared logic are housed in a dedicated Shared folder, fostering reusability and consistency across features.
	