using Microsoft.EntityFrameworkCore;
using NetSimpleAuctioneer.API.Application.Policies;
using NetSimpleAuctioneer.API.Database;
using NetSimpleAuctioneer.API.Features.Auctions.Shared;
using NetSimpleAuctioneer.API.Features.Shared;
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
            // Retrieve policies from the PolicyProvider - cannot return a value directly, so an exception is thrown, caught and handled
            var retryPolicy = policyProvider.GetRetryPolicyWithoutConcurrencyException();
            var circuitBreakerPolicy = policyProvider.GetCircuitBreakerPolicy();

            try
            {
                // Retry the database operation with Polly policy - cannot return a value directly, so an exception is thrown and caught
                await Policy.WrapAsync(retryPolicy, circuitBreakerPolicy).ExecuteAsync(async () =>
                {
                    // Get the auction to check if it's still open
                    var auction = await context.Auctions.SingleOrDefaultAsync(a => a.Id == auctionId, cancellationToken);

                    if (auction == null)
                    {
                        logger.LogWarning("Auction with ID {AuctionId} not found or already closed.", auctionId);
                        throw new AuctionNotFoundException();
                    }
                    if (auction.EndDate != null)
                    {
                        logger.LogWarning("Auction with ID {AuctionId} is already closed.", auctionId);
                        throw new AuctionAlreadyClosedException();
                    }

                    // Set the EndDate to close the auction
                    auction.EndDate = DateTime.UtcNow;

                    // Save changes to the database
                    await context.SaveChangesAsync(cancellationToken);

                    logger.LogInformation("Auction with ID {AuctionId} closed successfully.", auctionId);
                });

                return SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>.Success(new CloseAuctionCommandResult(auctionId));
            }
            catch (AuctionNotFoundException) { return SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>.Failure(CloseAuctionErrorCode.AuctionNotFound); }
            catch (AuctionAlreadyClosedException) { return SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>.Failure(CloseAuctionErrorCode.AuctionAlreadyClosed); }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error closing auction with ID: {AuctionId}", auctionId);
                return SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>.Failure(CloseAuctionErrorCode.InternalError);
            }
        }
    }
}