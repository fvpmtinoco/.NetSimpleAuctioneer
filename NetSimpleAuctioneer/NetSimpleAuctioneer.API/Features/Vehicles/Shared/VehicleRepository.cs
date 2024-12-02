using Microsoft.EntityFrameworkCore;
using NetSimpleAuctioneer.API.Database;
using Npgsql;

namespace NetSimpleAuctioneer.API.Features.Vehicles.Shared
{
    public interface IVehicleRepository
    {
        /// <summary>
        /// Adds a vehicle to the database with retry logic. Returns an error code if the operation fails.
        /// </summary>
        /// <param name="vehicle">The vehicle to add.</param>
        /// <returns>A nullable <see cref="AddVehicleErrorCode"/> indicating the result of the operation. Null if successful.</returns>
        Task<AddVehicleErrorCode?> AddWithRetryAsync(Vehicle vehicle, CancellationToken cancellationToken);
    }

    public class VehicleRepository(AuctioneerDbContext dbContext, ILogger<VehicleRepository> logger) : IVehicleRepository
    {
        public async Task<AddVehicleErrorCode?> AddWithRetryAsync(Vehicle vehicle, CancellationToken cancellationToken)
        {
            //        var retryPolicy = Policy
            //.Handle<DbUpdateException>(ex =>
            //    !(ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")) // Skip unique constraint violations
            //.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(100));
            try
            {
                dbContext.Vehicles.Add(vehicle);
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Added vehicle with ID {VehicleId}", vehicle.Id);
                return null;
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
            {
                logger.LogWarning("Attempted to add a vehicle with a duplicate ID {VehicleId}", vehicle.Id);
                return AddVehicleErrorCode.DuplicatedVehicle;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unknown error occurred while adding a vehicle with ID {VehicleId}", vehicle.Id);
                return AddVehicleErrorCode.UnknownError;
            }
        }
    }
}
