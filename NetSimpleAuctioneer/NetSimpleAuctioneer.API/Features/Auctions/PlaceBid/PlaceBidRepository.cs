using Microsoft.EntityFrameworkCore;
using NetSimpleAuctioneer.API.Application.Policies;
using NetSimpleAuctioneer.API.Database;
using NetSimpleAuctioneer.API.Features.Auctions.Shared;
using NetSimpleAuctioneer.API.Features.Shared;
using Polly;

namespace NetSimpleAuctioneer.API.Features.Auctions.PlaceBid
{
    public interface IPlaceBidRepository
    {
        /// <summary>
        /// Places a bid for the given auction
        /// </summary>
        /// <param name="auctionId"></param>
        /// <param name="bidderEmail"></param>
        /// <param name="bidAmount"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>> PlaceBidAsync(Guid auctionId, string bidderEmail, decimal bidAmount, CancellationToken cancellationToken);
    }

    public class PlaceBidRepository(AuctioneerDbContext context, ILogger<PlaceBidRepository> logger, IPolicyProvider policyProvider) : IPlaceBidRepository
    {
        public async Task<SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>> PlaceBidAsync(Guid auctionId, string bidderEmail, decimal bidAmount, CancellationToken cancellationToken)
        {
            // Retrieve policies from the PolicyProvider
            var retryPolicy = policyProvider.GetRetryPolicy();
            var circuitBreakerPolicy = policyProvider.GetCircuitBreakerPolicy();

            try
            {
                // Retry the database operation with Polly policy - cannot return a value directly, so an exception is thrown, caught and handled
                await Policy.WrapAsync(retryPolicy, circuitBreakerPolicy).ExecuteAsync(async () =>
                {
                    // Check if the auction exists
                    var auction = await context.Auctions.SingleOrDefaultAsync(a => a.Id == auctionId, cancellationToken);
                    if (auction == null)
                    {
                        logger.LogWarning("Auction with ID {AuctionId} not found.", auctionId);
                        throw new AuctionNotFoundException();
                    }

                    // Check if the auction is still open
                    if (auction.EndDate != null)
                    {
                        logger.LogWarning("Auction with ID {AuctionId} is already closed.", auctionId);
                        throw new AuctionAlreadyClosedException();
                    }

                    // Check if the bid amount is higher than the current highest bid
                    var highestBid = await context.Bids
                                                  .Where(b => b.AuctionId == auctionId)
                                                  .OrderByDescending(b => b.BidAmount)
                                                  .FirstOrDefaultAsync(cancellationToken);

                    if (highestBid != null)
                    {
                        if (bidAmount <= highestBid.BidAmount)
                        {
                            logger.LogWarning("Bid amount {BidAmount} is equal or lower than the highest bid {HighestBidAmount} for auction {AuctionId}.", bidAmount, highestBid.BidAmount, auctionId);
                            throw new BidAmountTooLowException();
                        }
                        if (bidderEmail.ToLowerInvariant() == highestBid.BidderEmail)
                        {
                            logger.LogWarning("Bidder {BidderEmail} already has the highest bid for auction {AuctionId}.", bidderEmail, auctionId);
                            throw new BidderHasHigherBidException();
                        }
                    }

                    // Add the new bid to the database
                    var bid = new Bid
                    {
                        AuctionId = auctionId,
                        BidderEmail = bidderEmail,
                        BidAmount = bidAmount,
                        Timestamp = DateTime.UtcNow
                    };

                    await context.Bids.AddAsync(bid, cancellationToken);
                    await context.SaveChangesAsync(cancellationToken);

                    logger.LogInformation("Bid placed successfully for auction {AuctionId} by bidder {BidderEmail}.", auctionId, bidderEmail);
                });

                return SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>.Success(new PlaceBidCommandResult(auctionId));
            }
            catch (AuctionNotFoundException) { return SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>.Failure(PlaceBidErrorCode.AuctionNotFound); }
            catch (AuctionAlreadyClosedException) { return SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>.Failure(PlaceBidErrorCode.AuctionAlreadyClosed); }
            catch (BidAmountTooLowException) { return SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>.Failure(PlaceBidErrorCode.BidAmountTooLow); }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error placing bid for auction with ID: {AuctionId}", auctionId);
                return SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>.Failure(PlaceBidErrorCode.InternalError);
            }
        }
    }

    public class BidderHasHigherBidException() : Exception { }
    public class BidAmountTooLowException() : Exception { }
}
