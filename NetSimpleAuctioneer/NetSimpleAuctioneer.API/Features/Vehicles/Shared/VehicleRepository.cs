using Microsoft.EntityFrameworkCore;
using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Domain;
using NetSimpleAuctioneer.API.Infrastructure.Data;
using Npgsql;

namespace NetSimpleAuctioneer.API.Features.Vehicles.Shared
{
    public interface IVehicleRepository
    {
        /// <summary>
        /// Adds a vehicle to the database
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<VoidOrError<AddVehicleErrorCode>> AddVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken);

        /// <summary>
        /// Check if a vehicle exists in the database
        /// </summary>
        /// <param name="vehicleId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool?> VehicleExistsAsync(Guid vehicleId, CancellationToken cancellationToken);
    }

    public class VehicleRepository(AuctioneerDbContext context, ILogger<VehicleRepository> logger) : IVehicleRepository
    {
        public async Task<VoidOrError<AddVehicleErrorCode>> AddVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken)
        {
            try
            {
                context.Vehicles.Add(vehicle);
                await context.SaveChangesAsync(cancellationToken);

                return VoidOrError<AddVehicleErrorCode>.Success();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
            {
                // Specific constraint error in PostGresSQL, just to prevent racing conditions
                logger.LogWarning("Attempted to add a vehicle with a duplicate ID {VehicleId}", vehicle.Id);
                return VoidOrError<AddVehicleErrorCode>.Failure(AddVehicleErrorCode.DuplicatedVehicle);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unknown error occurred while adding a vehicle with ID {VehicleId}", vehicle.Id);
                return VoidOrError<AddVehicleErrorCode>.Failure(AddVehicleErrorCode.InternalError);
            }
        }

        public async Task<bool?> VehicleExistsAsync(Guid vehicleId, CancellationToken cancellationToken)
        {
            try
            {
                return await context.Auctions.AnyAsync(a => a.VehicleId == vehicleId && a.EndDate == null, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while checking for active auction for vehicle with ID: {VehicleId}", vehicleId);
                return null;
            }
        }
    }
}
