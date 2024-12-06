using Microsoft.EntityFrameworkCore;
using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;
using NetSimpleAuctioneer.API.Infrastructure.Data;

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

    public class SearchRepository(AuctioneerDbContext context, ILogger<SearchRepository> logger) : ISearchRepository
    {
        public async Task<SuccessOrError<IEnumerable<SearchVehicleResult>, SearchVehicleErrorCode>> SearchVehiclesAsync(
             string? manufacturer,
             string? model,
             int? year,
             VehicleType? vehicleType,
             int pageNumber,
             int pageSize,
             CancellationToken cancellationToken)
        {
            // Calculate the offset for pagination
            var offset = (pageNumber - 1) * pageSize;

            try
            {
                var query = context.Vehicles
                    .Where(v =>
                        (manufacturer == null || v.Manufacturer.ToLower() == manufacturer.ToLower()) &&
                        (model == null || v.Model.ToLower() == model.ToLower()) &&
                        (year == null || v.Year == year) &&
                        (vehicleType == null || v.VehicleType == (int)vehicleType))
                    .Select(v => new
                    {
                        v.Id,
                        v.Manufacturer,
                        v.Model,
                        v.Year,
                        v.StartingBid,
                        v.VehicleType,
                        AuctionId = v.Auctions
                            .Where(a => a.EndDate == null)  // Active auction with NULL EndDate
                            .Select(a => a.Id)
                            .FirstOrDefault()  // If no auction, will be 0 or null (depending on nullable types)
                    })
                    .OrderBy(v => v.Id) // Ordering by id ensures unique pagination
                    .Skip(offset)
                    .Take(pageSize);

                // Execute the query asynchronously and map to result
                var result = await query
                    .AsNoTracking() // Optional: Use AsNoTracking if you don't need change tracking for this query
                    .ToListAsync(cancellationToken);

                // Map the results to the result type (SearchVehicleResult)
                var mappedResults = result.Select(v => new SearchVehicleResult
                {
                    Id = v.Id,
                    Manufacturer = v.Manufacturer,
                    Model = v.Model,
                    Year = v.Year,
                    StartingBid = v.StartingBid,
                    VehicleType = (VehicleType)v.VehicleType,
                    AuctionId = v.AuctionId
                }).ToList();

                return SuccessOrError<IEnumerable<SearchVehicleResult>, SearchVehicleErrorCode>.Success(mappedResults);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while searching for vehicles.");
                return SuccessOrError<IEnumerable<SearchVehicleResult>, SearchVehicleErrorCode>.Failure(SearchVehicleErrorCode.InternalError);
            }
        }
    }
}
