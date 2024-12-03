using Dapper;
using Microsoft.EntityFrameworkCore;
using NetSimpleAuctioneer.API.Application.Policies;
using NetSimpleAuctioneer.API.Database;
using NetSimpleAuctioneer.API.Features.Auctions.Shared;
using NetSimpleAuctioneer.API.Features.Shared;
using Npgsql;
using Polly;

namespace NetSimpleAuctioneer.API.Features.Auctions.PlaceBid
{
    public interface IPlaceBidRepository
    {
        /// <summary>
        /// Get the auction information for the given auction ID
        /// </summary>
        /// <param name="auctionId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<AuctionInformation?> GetAuctionInformation(Guid auctionId, CancellationToken cancellationToken);

        /// <summary>
        /// Get the auction information for the given auction ID
        /// </summary>
        /// <param name="auctionId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<BidInformation?> GetHighestBidForAuctionAsync(Guid auctionId, CancellationToken cancellationToken);

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
        public async Task<AuctionInformation?> GetAuctionInformation(Guid auctionId, CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(GetConnectionString());
            await connection.OpenAsync(cancellationToken);

            var query = "SELECT v.startingbid MinimumBidAmount, a.EndDate FROM auction a JOIN vehicle v ON v.id = a.vehicleid WHERE a.id = @auctionId;";

            // Use CommandDefinition to pass the cancellation token
            var command = new CommandDefinition(query, new { auctionId }, cancellationToken: cancellationToken);
            return await connection.QuerySingleOrDefaultAsync<AuctionInformation?>(command);
        }

        public async Task<BidInformation?> GetHighestBidForAuctionAsync(Guid auctionId, CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(GetConnectionString());
            await connection.OpenAsync(cancellationToken);

            var query = "SELECT bidamount, biddersemail FROM bid WHERE auctionid = @auctionId ORDER BY bidamount DESC LIMIT 1";

            // Use CommandDefinition to pass the cancellation token
            var command = new CommandDefinition(query, new { auctionId }, cancellationToken: cancellationToken);
            return await connection.QuerySingleOrDefaultAsync<BidInformation?>(command);
        }

        public async Task<SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>> PlaceBidAsync(Guid auctionId, string bidderEmail, decimal bidAmount, CancellationToken cancellationToken)
        {
            try
            {
                // Retrieve policies from the PolicyProvider
                var retryPolicy = policyProvider.GetRetryPolicy();
                var circuitBreakerPolicy = policyProvider.GetCircuitBreakerPolicy();

                // Retry the database operation with Polly policy - cannot return a value directly, so an exception is thrown and caught
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

        private string GetConnectionString()
        {
            // Access the connection string directly
            var connection = context.Database.GetDbConnection();
            return connection.ConnectionString;
        }
    }

    public record AuctionInformation
    {
        public decimal MinimumBidAmount { get; init; }
        public DateTime? EndDate { get; init; }
    }

    public record BidInformation
    {
        public decimal BidAmount { get; init; }
        public string BiddersEmail { get; init; } = default!;
    }
}
