using Dapper;
using Microsoft.EntityFrameworkCore;
using NetSimpleAuctioneer.API.Database;
using NetSimpleAuctioneer.API.Features.Auctions.Shared;
using NetSimpleAuctioneer.API.Features.Shared;
using Npgsql;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace NetSimpleAuctioneer.API.Features.Auctions.CloseAuction
{
    public interface ICloseAuctionRepository
    {
        /// <summary>
        /// Closes an auction by updating its EndDate
        /// </summary>
        /// <param name="auctionId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>> CloseAuctionAsync(Guid auctionId, CancellationToken cancellationToken);

        /// <summary>
        /// Get auction by its identifier
        /// </summary>
        /// <param name="auctionId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<Auction?> GetAuctionByIdAsync(Guid auctionId, CancellationToken cancellationToken);
    }

    public class CloseAuctionRepository(AuctioneerDbContext context, ILogger<CloseAuctionRepository> logger) : ICloseAuctionRepository
    {
        public async Task<SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>> CloseAuctionAsync(Guid auctionId, CancellationToken cancellationToken)
        {
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
            catch (AuctionNotFoundException)
            {
                return SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>.Failure(CloseAuctionErrorCode.AuctionNotFound);
            }
            catch (AuctionAlreadyClosedException)
            {
                return SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>.Failure(CloseAuctionErrorCode.AuctionAlreadyClosed);
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error closing auction with ID: {AuctionId}", auctionId);
                return SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>.Failure(CloseAuctionErrorCode.InternalError);
            }
        }

        public async Task<Auction?> GetAuctionByIdAsync(Guid auctionId, CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(GetConnectionString());
            await connection.OpenAsync(cancellationToken);

            var query = "SELECT id, vehicleid, startdate, enddate FROM auction WHERE id = @AuctionId";

            var command = new CommandDefinition(query, new { AuctionId = auctionId }, cancellationToken: cancellationToken);
            return await connection.QuerySingleOrDefaultAsync<Auction?>(command);
        }

        private string GetConnectionString()
        {
            // Access the connection string directly
            var connection = context.Database.GetDbConnection();
            return connection.ConnectionString;
        }

        // Handle DbUpdateException, which could occur if there's a database constraint violation or update failure
        private readonly AsyncRetryPolicy retryPolicy = Policy.Handle<DbUpdateException>()
                                                              .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                                                                                (exception, timeSpan, attempt, context) =>
                                                                                {
                                                                                    logger.LogWarning("Attempt {Attempt} failed due to exception: {Exception}. Retrying in {TimeSpan} seconds.",
                                                                                        attempt, exception.Message, timeSpan.TotalSeconds);
                                                                                });

        // Handle DbUpdateException here as well, which can happen during retries or once the circuit is broken
        // Break the circuit after 2 consecutive failures and keep it broken for 1 minute
        private readonly AsyncCircuitBreakerPolicy circuitBreakerPolicy = Policy.Handle<DbUpdateException>()
                                                                                .CircuitBreakerAsync(2, TimeSpan.FromMinutes(1),
                                                                                    onBreak: (exception, timespan) =>
                                                                                    {
                                                                                        logger.LogError("Circuit breaker triggered due to multiple failures: {Exception}. The system will be paused for {TimeSpan} minutes.",
                                                                                            exception.Message, timespan.TotalMinutes);
                                                                                    },
                                                                                    // Logging when the circuit is reset and retries are allowed again
                                                                                    onReset: () =>
                                                                                    {
                                                                                        logger.LogInformation("Circuit breaker reset. Retrying operations after 1 minute.");
                                                                                    });
    }
}