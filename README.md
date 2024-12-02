# .NetSimpleAuctioneer

## Key Design Features

### 1. Vertical Slice Architecture (VSA)
The project employs [Vertical Slice Architecture (VSA)](https://www.milanjovanovic.tech/blog/vertical-slice-architecture) to organize all feature-specific logic (e.g., commands, handlers, models, validations) into modular slices. This approach ensures a maintainable and cohesive structure.

- **Self-Contained Endpoints**: Each endpoint (e.g., `AddTruck`, `AddSUV`, `StartAuction`) is fully self-contained, minimizing dependencies and simplifying development.
- **VSA** was chosen over Clean Architecture due to its lower overhead, making it better suited for this project's scope, which includes only eight endpoints and relatively straightforward business logic.
- **When to Consider Clean Architecture**: For projects with more complex requirements, Clean Architecture might be a more appropriate choice.

**Shared Resources**:  
Common services, repositories, and shared logic are housed in a dedicated `Shared` folder, intended for reusability and consistency across features.

## Considerations

This API uses ASP.NET Core's built-in validation with Data Annotations. Considered the approach suitable for this project because it is a simple API with no complex validation rules. 
All validation logic is defined directly in the model classes using attributes like `[Required]`, `[EmailAddress]`, and `[Range]`. This keeps the implementation straightforward and easy to maintain.

For more complex scenarios or reusable validation logic, external libraries like FluentValidation could be considered in future updates.

# Auctioneer API - Database Setup

This guide provides the steps to set up the PostgreSQL database for the Auctioneer API using the existing migrations.

---

## Prerequisites

1. **Docker**:
   - Ensure Docker is installed and running.
   - Start the PostgreSQL container using Docker Compose:
     ```bash
     docker-compose up -d
     ```
2. **Entity Framework Core Tools**:
   - Install EF Core CLI tools globally (if not already installed)

3. **Connection String**:
   - Ensure the connection string in `appsettings.json` matches your database setup:
     ```json
     {
       "ConnectionStrings": {
         "DefaultConnection": "Host=localhost;Port=5432;Database=AuctioneerDB;Username=postgres;Password=postgres"
       }
     }
     ```

---

## Step 1: Apply Migrations

To update the PostgreSQL database schema based on the existing migrations, run:

```bash
dotnet ef database update
```

## Step 2: Verify the Database was created along with the tables (using Powershell)

Check if the PostgreSQL container is running
   ```bash
   docker ps
   ```
### Using powershell:
1. Enter the container bash
   ```bash
   docker exec -it auctioneerdb bash
   ```
2. Access the PostgreSQL shell:
   ```bash
   psql -U postgres -d AuctioneerDB
   ```
3. Verify that the tables were created
    ```bash
    \dt
    ```
    ![image](https://github.com/user-attachments/assets/da34003b-42a7-4ac4-b61c-e066bbe7b583)

### Using [pgAgmin](https://www.postgresql.org/ftp/pgadmin/pgadmin4/v8.13/windows/):
1. Register new server
   
   ![image](https://github.com/user-attachments/assets/41dbef7b-1301-4e39-be35-37843b3312c7)
2. Verify both database and corresponding tables were created
   
   ![image](https://github.com/user-attachments/assets/d5d90cde-28fc-463d-9a0d-815239de9c23)

