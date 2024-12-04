using Dapper;
using Microsoft.Extensions.Options;
using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Database;
using Npgsql;

namespace NetSimpleAuctioneer.API.Features.Auctions.StartAuction
{
    public interface IStartAuctionRepository
    {
        /// <summary>
        /// Stores an auction in the database
        /// </summary>
        /// <param name="auction"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>> StartAuctionAsync(Auction auction, CancellationToken cancellationToken);

        /// <summary>
        /// Check if vehicle exists
        /// </summary>
        /// <param name="vehicleId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool?> VehicleExistsAsync(Guid vehicleId, CancellationToken cancellationToken);

        /// <summary>
        /// Check if an active auction exists for a vehicle
        /// </summary>
        /// <param name="vehicleId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool?> ActiveAuctionForVehicleExistsAsync(Guid vehicleId, CancellationToken cancellationToken);
    }
    public class StartAuctionRepository(AuctioneerDbContext context, ILogger<StartAuctionRepository> logger, IOptions<ConnectionStrings> connectionStrings, IDatabaseConnection dbConnection) : IStartAuctionRepository
    {
        public async Task<SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>> StartAuctionAsync(Auction auction, CancellationToken cancellationToken)
        {
            try
            {
                // Add the new auction and save changes
                await context.Auctions.AddAsync(auction, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);

                logger.LogInformation("Auction successfully added for vehicle ID: {VehicleId}", auction.VehicleId);
                return SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>.Success(new StartAuctionCommandResult(auction.Id));

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while creating auction for vehicle ID: {VehicleId}", auction.VehicleId);
                return SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>.Failure(StartAuctionErrorCode.InternalError);
            }
        }

        public async Task<bool?> VehicleExistsAsync(Guid vehicleId, CancellationToken cancellationToken)
        {
            try
            {
                string query = "SELECT EXISTS (SELECT 1 FROM vehicle WHERE id = @vehicleId)";
                await using var connection = new NpgsqlConnection(connectionStrings.Value.AuctioneerDBConnectionString);
                var command = new CommandDefinition(query, new { vehicleId }, cancellationToken: cancellationToken);

                var result = await dbConnection.QuerySingleOrDefaultAsync<bool>(command);

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while retrieving information for vehicle with ID: {VehicleId}", vehicleId);
                return null;
            }
        }

        public async Task<bool?> ActiveAuctionForVehicleExistsAsync(Guid vehicleId, CancellationToken cancellationToken)
        {
            try
            {
                string query = "SELECT EXISTS (SELECT 1 FROM auction WHERE vehicleid = @vehicleId and enddate is null)";
                await using var connection = new NpgsqlConnection(connectionStrings.Value.AuctioneerDBConnectionString);
                var command = new CommandDefinition(query, new { vehicleId }, cancellationToken: cancellationToken);

                var result = await dbConnection.QuerySingleOrDefaultAsync<bool>(command);

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while checking for active auction for vehicle with ID: {VehicleId}", vehicleId);
                return null;
            }
        }
    }
}
