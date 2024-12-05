using Dapper;
using Microsoft.Extensions.Options;
using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Domain;
using NetSimpleAuctioneer.API.Infrastructure.Configuration;
using NetSimpleAuctioneer.API.Infrastructure.Data;
using Npgsql;

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
        Task<(decimal? lastBid, string? bidderemail)?> GetHighestBidForAuctionAsync(Guid auctionId, CancellationToken cancellationToken);
    }

    public class PlaceBidRepository(AuctioneerDbContext context, ILogger<PlaceBidRepository> logger, IOptions<ConnectionStrings> connectionStrings, IDatabaseConnection dbConnection) : IPlaceBidRepository
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
                var query = @"SELECT id, enddate, minimumbid FROM auction WHERE id = @auctionId";
                await using var connection = new NpgsqlConnection(connectionStrings.Value.AuctioneerDBConnectionString);
                var command = new CommandDefinition(query, new { auctionId }, cancellationToken: cancellationToken);

                var result = await dbConnection.QuerySingleOrDefaultAsync<(Guid? id, DateTime? enddate, decimal? minimumBid)>(command);

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving information from auction with ID: {AuctionId} when placing bid", auctionId);
                return null;
            }
        }

        public async Task<(decimal? lastBid, string? bidderemail)?> GetHighestBidForAuctionAsync(Guid auctionId, CancellationToken cancellationToken)
        {
            try
            {
                var query = "SELECT bidamount, bidderemail FROM bids WHERE auctionid = @auctionId ORDER BY bidamount DESC LIMIT 1";
                await using var connection = new NpgsqlConnection(connectionStrings.Value.AuctioneerDBConnectionString);
                var command = new CommandDefinition(query, new { auctionId }, cancellationToken: cancellationToken);

                var result = await dbConnection.QuerySingleOrDefaultAsync<(decimal? lastBid, string? bidderemail)>(command);

                return result;

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving information from auction with ID: {AuctionId} when placing bid", auctionId);
                return null;
            }
        }
    }
}
