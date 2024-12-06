using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Application.Policies;
using NetSimpleAuctioneer.API.Domain;
using Polly;

namespace NetSimpleAuctioneer.API.Features.Auctions.PlaceBid
{
    public interface IPlaceBidService
    {
        /// <summary>
        /// Places a bid on an auction.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>> PlaceBidAsync(PlaceBidCommand command, CancellationToken cancellationToken);

        /// <summary>
        /// Validates if a bid can be placed on an auction.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<PlaceBidErrorCode?> ValidateAuctionAsync(PlaceBidCommand command, CancellationToken cancellationToken);
    }

    public class PlaceBidService(IPlaceBidRepository repository, IPolicyProvider policyProvider, ILogger<PlaceBidService> logger) : IPlaceBidService
    {
        public async Task<SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>> PlaceBidAsync(PlaceBidCommand command, CancellationToken cancellationToken)
        {
            try
            {
                // Retrieve the policies for retry and circuit breaker
                var retryPolicy = policyProvider.GetRetryPolicy();
                var circuitBreakerPolicy = policyProvider.GetCircuitBreakerPolicy();

                // Apply the policies for resilience (retry and circuit breaker)
                var result = await Policy.WrapAsync(retryPolicy, circuitBreakerPolicy).ExecuteAsync(async ct =>
                {
                    var bid = new Bid
                    {
                        AuctionId = command.AuctionId,
                        BidderEmail = command.BidderEmail,
                        BidAmount = command.BidAmount,
                        Timestamp = DateTime.UtcNow
                    };

                    // Pass validated data to repository to actually place the bid
                    return await repository.PlaceBidAsync(bid, ct);
                }, cancellationToken);

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error placing bid for auction with ID: {AuctionId}", command.AuctionId);
                return SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>.Failure(PlaceBidErrorCode.InternalError);
            }
        }

        public async Task<PlaceBidErrorCode?> ValidateAuctionAsync(PlaceBidCommand command, CancellationToken cancellationToken)
        {
            // Validate auction status: Ensure auction exists and is open
            var auctionInfo = await repository.GetAuctionByIdAsync(command.AuctionId, cancellationToken);
            if (auctionInfo is null)
                return PlaceBidErrorCode.InternalError;

            if (!auctionInfo.Value.auctionId.HasValue)
            {
                logger.LogWarning("Auction with ID {AuctionId} not found.", command.AuctionId);
                return PlaceBidErrorCode.InvalidAuction;
            }

            if (auctionInfo.Value.endDate.HasValue)
            {
                logger.LogWarning("Auction with ID {AuctionId} is already closed.", command.AuctionId);
                return PlaceBidErrorCode.AuctionAlreadyClosed;
            }

            if (auctionInfo.Value.minimumBid > command.BidAmount)
            {
                logger.LogWarning("Bid amount {BidAmount} is lower than the minimum bid {MinimumBid} for auction {AuctionId}.", command.BidAmount, auctionInfo.Value.minimumBid, command.AuctionId);
                return PlaceBidErrorCode.BidAmountTooLow;
            }

            // Validate bid amount against the current highest bid
            var bidInformation = await repository.GetHighestBidForAuctionAsync(command.AuctionId, cancellationToken);
            if (bidInformation is null)
                return PlaceBidErrorCode.InternalError;

            if (bidInformation.Value.lastBid >= command.BidAmount)
            {
                logger.LogWarning("Bid amount {BidAmount} is lower than the current highest bid {HighestBid} for auction {AuctionId}.", command.BidAmount, bidInformation.Value.lastBid, command.AuctionId);
                return PlaceBidErrorCode.ExistingHigherBid;
            }

            if (command.BidderEmail.Equals(bidInformation.Value.bidderEmail, StringComparison.InvariantCultureIgnoreCase))
            {
                logger.LogWarning("Bidder {BidderEmail} already has the highest bid for auction {AuctionId}.", command.BidderEmail, command.AuctionId);
                return PlaceBidErrorCode.BidderHasHigherBid;
            }

            // All ok
            return null;
        }
    }
}
