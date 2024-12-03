using Dapper;
using Microsoft.EntityFrameworkCore;
using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Application.Policies;
using NetSimpleAuctioneer.API.Database;
using Npgsql;
using Polly;

namespace NetSimpleAuctioneer.API.Features.Auctions.StartAuction
{
    public interface IStartAuctionRepository
    {
        /// <summary>
        /// Start an auction for a vehicle
        /// </summary>
        /// <param name="vehicleId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>> StartAuctionAsync(Guid vehicleId, CancellationToken cancellationToken);

        /// <summary>
        /// Check if vehicle exists
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="vehicleId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<SuccessOrError<bool, StartAuctionErrorCode>> VehicleExistsAsync(NpgsqlConnection connection, Guid vehicleId, CancellationToken cancellationToken);
    }
    public class StartAuctionRepository(AuctioneerDbContext context, ILogger<StartAuctionRepository> logger, IPolicyProvider policyProvider, IDatabaseConnection dbConnection) : IStartAuctionRepository
    {
        public async Task<SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>> StartAuctionAsync(Guid vehicleId, CancellationToken cancellationToken)
        {
            Auction auction = null!;

            try
            {
                // Retrieve policies from the PolicyProvider
                var retryPolicy = policyProvider.GetRetryPolicyWithoutConcurrencyException();
                var circuitBreakerPolicy = policyProvider.GetCircuitBreakerPolicy();

                // Retry the database operation with Polly policy - cannot return a value directly, so an exception is thrown, caught and handled
                var result = await Policy.WrapAsync(retryPolicy, circuitBreakerPolicy).ExecuteAsync(async ct =>
                {
                    // Check if there is already an active auction for this vehicle
                    var existingAuction = await context.Auctions.SingleOrDefaultAsync(a => a.VehicleId == vehicleId && a.EndDate == null, cancellationToken);

                    if (existingAuction != null)
                    {
                        logger.LogWarning("Attempted to create an auction for a vehicle with an active auction. VehicleId: {VehicleId}", vehicleId);
                        return SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>.Failure(StartAuctionErrorCode.AuctionForVehicleAlreadyActive);
                    }

                    // Create a new auction record
                    auction = new Auction
                    {
                        VehicleId = vehicleId,
                        StartDate = DateTime.UtcNow
                    };

                    // Add the new auction and save changes
                    await context.Auctions.AddAsync(auction, ct);
                    await context.SaveChangesAsync(ct);

                    logger.LogInformation("Auction successfully added for vehicle ID: {VehicleId}", auction.VehicleId);
                    return SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>.Success(new StartAuctionCommandResult(auction.Id));

                }, cancellationToken);

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while creating auction for vehicle ID: {VehicleId}", vehicleId);
                return SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>.Failure(StartAuctionErrorCode.InternalError);
            }
        }

        public async Task<SuccessOrError<bool, StartAuctionErrorCode>> VehicleExistsAsync(NpgsqlConnection connection, Guid vehicleId, CancellationToken cancellationToken)
        {
            string query = "SELECT EXISTS (SELECT 1 FROM vehicle WHERE id = @vehicleId)";

            // Retrieve policies from the PolicyProvider
            var retryPolicy = policyProvider.GetRetryPolicyWithoutConcurrencyException();
            var circuitBreakerPolicy = policyProvider.GetCircuitBreakerPolicy();

            try
            {
                var command = new CommandDefinition(query, new { vehicleId }, cancellationToken: cancellationToken);

                // Retry the database operation with Polly policy
                var result = await Policy.WrapAsync(retryPolicy, circuitBreakerPolicy).ExecuteAsync(async () =>
                {
                    return await dbConnection.QuerySingleOrDefaultAsync<bool>(command);
                });

                return SuccessOrError<bool, StartAuctionErrorCode>.Success(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while searching for vehicles.");
                return SuccessOrError<bool, StartAuctionErrorCode>.Failure(StartAuctionErrorCode.InternalError);
            }
        }
    }

    public class AuctionAlreadyActiveException() : Exception { }
}
