using Microsoft.EntityFrameworkCore;
using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Application.Policies;
using NetSimpleAuctioneer.API.Database;
using Npgsql;
using Polly;

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
    }

    public class VehicleRepository(AuctioneerDbContext context, ILogger<VehicleRepository> logger, IPolicyProvider policyProvider) : IVehicleRepository
    {
        public async Task<VoidOrError<AddVehicleErrorCode>> AddVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken)
        {
            try
            {
                // Retrieve policies from the PolicyProvider
                var retryPolicy = policyProvider.GetRetryPolicyWithoutConcurrencyException();
                var circuitBreakerPolicy = policyProvider.GetCircuitBreakerPolicy();

                // Define your custom result type (success/failure) for Polly's ExecuteAsync
                var result = await Policy.WrapAsync(retryPolicy, circuitBreakerPolicy).ExecuteAsync(async ct =>
                    {
                        // Check if the vehicle exists
                        var existingVehicle = await context.Vehicles
                            .SingleOrDefaultAsync(v => v.Id == vehicle.Id, ct);

                        if (existingVehicle != null)
                        {
                            logger.LogWarning("Attempted to add a vehicle with a duplicate ID {VehicleId}", vehicle.Id);
                            return VoidOrError<AddVehicleErrorCode>.Failure(AddVehicleErrorCode.DuplicatedVehicle);
                        }

                        // If not duplicated, save the vehicle
                        context.Vehicles.Add(vehicle);
                        await context.SaveChangesAsync(ct);

                        logger.LogInformation("Added vehicle with ID {VehicleId}", vehicle.Id);
                        return VoidOrError<AddVehicleErrorCode>.Success();
                    }, cancellationToken);

                return result;
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
    }
}
