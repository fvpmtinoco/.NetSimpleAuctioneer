using Dapper;
using Microsoft.EntityFrameworkCore;
using NetSimpleAuctioneer.API.Database;
using NetSimpleAuctioneer.API.Features.Shared;
using Npgsql;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace NetSimpleAuctioneer.API.Features.Auctions.StartAuction
{
    public interface IStartAuctionRepository
    {
        /// <summary>
        /// Add a new auction to the database
        /// </summary>
        /// <param name="vehicleId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>> StartAuctionAsync(Guid vehicleId, CancellationToken cancellationToken);

        /// <summary>
        /// Check for active auction for a vehicle
        /// </summary>
        /// <param name="vehicleId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<Guid?> GetActiveAuctionByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken);

        /// <summary>
        /// Get vehicle by it's identifier
        /// </summary>
        /// <param name="vehicleId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<Vehicle?> GetVehicleByIdAsync(Guid vehicleId, CancellationToken cancellationToken);
    }
    public class StartAuctionRepository(AuctioneerDbContext context, ILogger<StartAuctionRepository> logger) : IStartAuctionRepository
    {
        public async Task<SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>> StartAuctionAsync(Guid vehicleId, CancellationToken cancellationToken)
        {
            var auction = new Auction
            {
                VehicleId = vehicleId,
                StartDate = DateTime.UtcNow,
            };

            try
            {
                // Retry the database operation with Polly policy
                await Policy.WrapAsync(retryPolicy, circuitBreakerPolicy).ExecuteAsync(async () =>
                {
                    // Attempt to add the auction and save changes to the database
                    await context.Auctions.AddAsync(auction, cancellationToken);
                    await context.SaveChangesAsync(cancellationToken);

                    // Logging the successful addition of the auction
                    logger.LogInformation("Auction successfully added for vehicle ID: {VehicleId}", auction.VehicleId);
                });

                return SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>.Success(new StartAuctionCommandResult(auction.Id));
            }
            catch (DbUpdateException ex)
            {
                // Handle the unique constraint violation created on ModelCreating (i.e., auction already exists for this vehicle)
                if (ex.InnerException is PostgresException postgresEx && postgresEx.SqlState == "23505")
                {
                    logger.LogWarning("Attempted to create an auction for a vehicle with an active auction. VehicleId: {VehicleId}", auction.VehicleId);
                    return SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>.Failure(StartAuctionErrorCode.AuctionAlreadyActive);
                }

                logger.LogError(ex, "Database connection error while creating auction for vehicle ID: {VehicleId}", auction.VehicleId);
                return SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>.Failure(StartAuctionErrorCode.InternalError);
            }
        }

        public async Task<Guid?> GetActiveAuctionByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(GetConnectionString());
            await connection.OpenAsync(cancellationToken);

            var query = "SELECT id FROM auction WHERE vehicleid = @VehicleId AND enddate IS NULL";

            // Use CommandDefinition to pass the cancellation token
            var command = new CommandDefinition(query, new { VehicleId = vehicleId }, cancellationToken: cancellationToken);
            return await connection.QuerySingleOrDefaultAsync<Guid?>(command);
        }

        public async Task<Vehicle?> GetVehicleByIdAsync(Guid vehicleId, CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(GetConnectionString());
            await connection.OpenAsync(cancellationToken);

            var query = "SELECT id, manufacturer, model, year, startingbid, vehicletype, numberofdoors, numberofseats, loadcapacity FROM vehicle WHERE id = @VehicleId";

            var command = new CommandDefinition(query, new { VehicleId = vehicleId }, cancellationToken: cancellationToken);
            return await connection.QuerySingleOrDefaultAsync<Vehicle?>(command);
        }

        private string GetConnectionString()
        {
            // Access the connection string directly
            var connection = context.Database.GetDbConnection();
            return connection.ConnectionString;
        }

        // Handle DbUpdateException, which could occur if there's a database constraint violation or update failure
        // Retry up to 3 times with an exponential backoff (e.g., retry after 2, 4, and 8 seconds)
        private readonly AsyncRetryPolicy retryPolicy = Policy.Handle<DbUpdateException>(ex => !(ex.InnerException is PostgresException postgresEx && postgresEx.SqlState == "23505"))
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
