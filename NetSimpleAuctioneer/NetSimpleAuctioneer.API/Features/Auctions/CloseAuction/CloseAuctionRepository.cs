using Microsoft.EntityFrameworkCore;
using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Application.Policies;
using NetSimpleAuctioneer.API.Database;
using Polly;

namespace NetSimpleAuctioneer.API.Features.Auctions.CloseAuction
{
    public interface ICloseAuctionRepository
    {
        /// <summary>
        /// Closes an auction by updating its end date
        /// </summary>
        /// <param name="auctionId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>> CloseAuctionAsync(Guid auctionId, CancellationToken cancellationToken);
    }

    public class CloseAuctionRepository(AuctioneerDbContext context, ILogger<CloseAuctionRepository> logger, IPolicyProvider policyProvider) : ICloseAuctionRepository
    {
        public async Task<SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>> CloseAuctionAsync(Guid auctionId, CancellationToken cancellationToken)
        {
            try
            {
                // Retrieve policies from the PolicyProvider
                var retryPolicy = policyProvider.GetRetryPolicyWithoutConcurrencyException();
                var circuitBreakerPolicy = policyProvider.GetCircuitBreakerPolicy();

                // Retry the database operation with Polly policy - cannot return a value directly, so an exception is thrown and caught
                var result = await Policy.WrapAsync(retryPolicy, circuitBreakerPolicy).ExecuteAsync(async ct =>
                {
                    // Get the auction to check if it's still open
                    var auction = await context.Auctions.SingleOrDefaultAsync(a => a.Id == auctionId, ct);

                    if (auction == null)
                    {
                        logger.LogWarning("Auction with ID {AuctionId} not found or already closed.", auctionId);
                        return SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>.Failure(CloseAuctionErrorCode.InvalidAuction);
                    }
                    if (auction.EndDate != null)
                    {
                        logger.LogWarning("Auction with ID {AuctionId} is already closed.", auctionId);
                        return SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>.Failure(CloseAuctionErrorCode.AuctionAlreadyClosed);
                    }

                    // Set the EndDate to close the auction
                    auction.EndDate = DateTime.UtcNow;

                    // Save changes to the database
                    await context.SaveChangesAsync(ct);

                    logger.LogInformation("Auction with ID {AuctionId} closed successfully.", auctionId);
                    return SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>.Success(new CloseAuctionCommandResult(auctionId));
                }, cancellationToken);

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error closing auction with ID: {AuctionId}", auctionId);
                return SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>.Failure(CloseAuctionErrorCode.InternalError);
            }
        }
    }
}