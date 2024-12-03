using Dapper;
using Microsoft.EntityFrameworkCore;
using NetSimpleAuctioneer.API.Database;
using NetSimpleAuctioneer.API.Features.Shared;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;

namespace NetSimpleAuctioneer.API.Features.Vehicles.Search
{
    public interface ISearchRepository
    {
        /// <summary>
        /// Search for vehicles based on the provided parameters.
        /// </summary>
        /// <param name="manufacturer"></param>
        /// <param name="model"></param>
        /// <param name="year"></param>
        /// <param name="vehicleType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<SuccessOrError<IEnumerable<SearchVehicleResult>, SearchVehicleErrorCode>> SearchVehiclesAsync(string? manufacturer, string? model, int? year, VehicleType? vehicleType, CancellationToken cancellationToken);
    }

    public class SearchRepository(AuctioneerDbContext context, ILogger<SearchRepository> logger) : ISearchRepository
    {
        public async Task<SuccessOrError<IEnumerable<SearchVehicleResult>, SearchVehicleErrorCode>> SearchVehiclesAsync(string? manufacturer, string? model, int? year, VehicleType? vehicleType, CancellationToken cancellationToken)
        {
            var query = @"
                SELECT v.id, v.manufacturer, v.model, v.year, v.startingbid, v.vehicletype, a.id AS auctionid
                FROM vehicle v
                LEFT JOIN auction a ON v.id = a.vehicleid AND a.enddate IS NULL -- Only join on auctions with enddate NULL (active auctions)
                WHERE 
                    (@Manufacturer IS NULL OR LOWER(manufacturer) = LOWER(@manufacturer)) 
                    AND (@Model IS NULL OR LOWER(model) = LOWER(@model)) 
                    AND (@Year IS NULL OR year = @year) 
                    AND (@VehicleType IS NULL OR vehicleType = @vehicleType)
                    AND (a.vehicleid IS NULL OR a.enddate IS NOT NULL)";

            try
            {
                using var connection = context.Database.GetDbConnection();
                var command = new CommandDefinition(query, new { manufacturer, model, year, vehicleType }, cancellationToken: cancellationToken);
                var result = await connection.QueryAsync<SearchVehicleResult>(command);
                return SuccessOrError<IEnumerable<SearchVehicleResult>, SearchVehicleErrorCode>.Success(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while searching for vehicles.");
                return SuccessOrError<IEnumerable<SearchVehicleResult>, SearchVehicleErrorCode>.Failure(SearchVehicleErrorCode.InternalError);
            }
        }
    }
}
