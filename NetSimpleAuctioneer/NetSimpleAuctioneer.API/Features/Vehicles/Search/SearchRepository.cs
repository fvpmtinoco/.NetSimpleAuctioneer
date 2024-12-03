using Dapper;
using Microsoft.Extensions.Options;
using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Application.Policies;
using NetSimpleAuctioneer.API.Database;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;
using Npgsql;
using Polly;

namespace NetSimpleAuctioneer.API.Features.Vehicles.Search
{
    public interface ISearchRepository
    {
        /// <summary>
        /// Search for vehicles based on the provided parameters
        /// </summary>
        /// <param name="manufacturer"></param>
        /// <param name="model"></param>
        /// <param name="year"></param>
        /// <param name="vehicleType"></param>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<SuccessOrError<IEnumerable<SearchVehicleResult>, SearchVehicleErrorCode>> SearchVehiclesAsync(string? manufacturer, string? model, int? year, VehicleType? vehicleType, int pageNumber, int pageSize, CancellationToken cancellationToken);
    }

    public class SearchRepository(ILogger<SearchRepository> logger, IPolicyProvider policyProvider, IOptions<ConnectionStrings> connectionStrings, IDatabaseConnection dbConnection) : ISearchRepository
    {
        public async Task<SuccessOrError<IEnumerable<SearchVehicleResult>, SearchVehicleErrorCode>> SearchVehiclesAsync(string? manufacturer, string? model, int? year, VehicleType? vehicleType, int pageNumber, int pageSize, CancellationToken cancellationToken)
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
                    AND (a.vehicleid IS NULL OR a.enddate IS NOT NULL)
                    ORDER BY v.id -- Ordering by id ensures unique pagination
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY"; // Pagination

            // Calculate the offset for pagination
            var offset = (pageNumber - 1) * pageSize;

            await using var connection = new NpgsqlConnection(connectionStrings.Value.AuctioneerDBConnectionString);
            var command = new CommandDefinition(query, new { manufacturer, model, year, vehicleType, offset, pageSize }, cancellationToken: cancellationToken);

            try
            {
                // Retrieve policies from the PolicyProvider
                var retryPolicy = policyProvider.GetRetryPolicyWithoutConcurrencyException();
                var circuitBreakerPolicy = policyProvider.GetCircuitBreakerPolicy();

                // Retry the database operation with Polly policy
                var result = await Policy.WrapAsync(retryPolicy, circuitBreakerPolicy).ExecuteAsync(async () =>
                {
                    return await dbConnection.QueryAsync<SearchVehicleResult>(command);
                });

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
