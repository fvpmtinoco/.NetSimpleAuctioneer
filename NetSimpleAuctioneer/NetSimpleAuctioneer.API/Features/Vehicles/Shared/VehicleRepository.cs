using Microsoft.EntityFrameworkCore;
using NetSimpleAuctioneer.API.Application.Policies;
using NetSimpleAuctioneer.API.Database;
using NetSimpleAuctioneer.API.Features.Shared;
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

    public class VehicleRepository(AuctioneerDbContext dbContext, ILogger<VehicleRepository> logger, IPolicyProvider policyProvider) : IVehicleRepository
    {
        public async Task<VoidOrError<AddVehicleErrorCode>> AddVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken)
        {
            // Retrieve policies from the PolicyProvider
            var retryPolicy = policyProvider.GetRetryPolicyWithoutConcurrencyException();
            var circuitBreakerPolicy = policyProvider.GetCircuitBreakerPolicy();

            try
            {
                // Retry the database operation with Polly policy
                await Policy.WrapAsync(retryPolicy, circuitBreakerPolicy).ExecuteAsync(async () =>
                {
                    // Add and save changes within the policy execution block to avoid side effects
                    dbContext.Vehicles.Add(vehicle);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    logger.LogInformation("Added vehicle with ID {VehicleId}", vehicle.Id);
                });
                return VoidOrError<AddVehicleErrorCode>.Success();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
            {
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
