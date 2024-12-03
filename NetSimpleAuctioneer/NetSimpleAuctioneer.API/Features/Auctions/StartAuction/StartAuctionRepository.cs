using Microsoft.EntityFrameworkCore;
using NetSimpleAuctioneer.API.Application.Policies;
using NetSimpleAuctioneer.API.Database;
using NetSimpleAuctioneer.API.Features.Shared;
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
    }
    public class StartAuctionRepository(AuctioneerDbContext context, ILogger<StartAuctionRepository> logger, IPolicyProvider policyProvider) : IStartAuctionRepository
    {
        public async Task<SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>> StartAuctionAsync(Guid vehicleId, CancellationToken cancellationToken)
        {
            // Retrieve policies from the PolicyProvider
            var retryPolicy = policyProvider.GetRetryPolicyWithoutConcurrencyException();
            var circuitBreakerPolicy = policyProvider.GetCircuitBreakerPolicy();

            try
            {
                Auction auction = null!;

                // Retry the database operation with Polly policy - cannot return a value directly, so an exception is thrown, caught and handled
                await Policy.WrapAsync(retryPolicy, circuitBreakerPolicy).ExecuteAsync(async () =>
                {
                    // Check if there is already an active auction for this vehicle
                    var existingAuction = await context.Auctions.SingleOrDefaultAsync(a => a.VehicleId == vehicleId && a.EndDate == null, cancellationToken);

                    if (existingAuction != null)
                    {
                        logger.LogWarning("Attempted to create an auction for a vehicle with an active auction. VehicleId: {VehicleId}", vehicleId);
                        throw new AuctionAlreadyActiveException();
                    }

                    // Create a new auction record
                    var auction = new Auction
                    {
                        VehicleId = vehicleId,
                        StartDate = DateTime.UtcNow
                    };

                    // Add the new auction and save changes
                    await context.Auctions.AddAsync(auction, cancellationToken);
                    await context.SaveChangesAsync(cancellationToken);

                    logger.LogInformation("Auction successfully added for vehicle ID: {VehicleId}", auction.VehicleId);
                });

                return SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>.Success(new StartAuctionCommandResult(auction.Id));
            }
            catch (AuctionAlreadyActiveException)
            {
                return SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>.Failure(StartAuctionErrorCode.AuctionAlreadyActive);
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error while creating auction for vehicle ID: {VehicleId}", vehicleId);
                return SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>.Failure(StartAuctionErrorCode.InternalError);
            }
        }
    }

    public class AuctionAlreadyActiveException() : Exception { }
}
