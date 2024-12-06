using Microsoft.EntityFrameworkCore;
using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Domain;
using NetSimpleAuctioneer.API.Infrastructure.Data;

namespace NetSimpleAuctioneer.API.Features.Auctions.PlaceBid
{
    public interface IPlaceBidRepository
    {
        /// <summary>
        /// Stores a bid
        /// </summary>
        /// <param name="bid"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>> PlaceBidAsync(Bid bid, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves an auction by its ID
        /// </summary>
        /// <param name="auctionId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<(Guid? auctionId, DateTime? endDate, decimal? minimumBid)?> GetAuctionByIdAsync(Guid auctionId, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves the highest bid for an auction
        /// </summary>
        /// <param name="auctionId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<(decimal? lastBid, string? bidderEmail)?> GetHighestBidForAuctionAsync(Guid auctionId, CancellationToken cancellationToken);
    }

    public class PlaceBidRepository(AuctioneerDbContext context, ILogger<PlaceBidRepository> logger) : IPlaceBidRepository
    {
        public async Task<SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>> PlaceBidAsync(Bid bid, CancellationToken cancellationToken)
        {
            try
            {
                await context.Bids.AddAsync(bid, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);

                logger.LogInformation("Bid with {Id} placed successfully for auction {AuctionId} by bidder {BidderEmail}.", bid.Id, bid.AuctionId, bid.BidderEmail);
                return SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>.Success(new PlaceBidCommandResult(bid.Id));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error placing bid for auction with ID: {AuctionId}", bid.AuctionId);
                return SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>.Failure(PlaceBidErrorCode.InternalError);
            }
        }

        public async Task<(Guid? auctionId, DateTime? endDate, decimal? minimumBid)?> GetAuctionByIdAsync(Guid auctionId, CancellationToken cancellationToken)
        {
            try
            {
                var auction = await context.Auctions
                    .Where(a => a.Id == auctionId)
                    .Select(a => new
                    {
                        a.Id,
                        a.EndDate,
                        MinimumBid = a.Vehicle!.StartingBid
                    })
                    .SingleOrDefaultAsync(cancellationToken);

                if (auction == null)
                    return null;

                return (auction.Id, auction.EndDate, auction.MinimumBid);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving information from auction with ID: {AuctionId} when placing bid", auctionId);
                return null;
            }
        }

        public async Task<(decimal? lastBid, string? bidderEmail)?> GetHighestBidForAuctionAsync(Guid auctionId, CancellationToken cancellationToken)
        {
            try
            {
                var highestBid = await context.Bids
                    .Where(b => b.AuctionId == auctionId)
                    .OrderByDescending(b => b.BidAmount)
                    .Select(b => new { b.BidAmount, b.BidderEmail })
                    .FirstOrDefaultAsync(cancellationToken);

                // No bids
                if (highestBid == null)
                    return (null, null);

                return (highestBid.BidAmount, highestBid.BidderEmail);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving highest bid for auction with ID: {AuctionId}", auctionId);
                return null;
            }
        }
    }
}
