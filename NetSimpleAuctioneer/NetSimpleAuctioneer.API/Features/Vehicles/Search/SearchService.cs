using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Application.Policies;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;
using Polly;

namespace NetSimpleAuctioneer.API.Features.Vehicles.Search
{
    public interface ISearchService
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

    public class SearchService(ISearchRepository searchRepository, ILogger<SearchService> logger, IPolicyProvider policyProvider) : ISearchService
    {
        public async Task<SuccessOrError<IEnumerable<SearchVehicleResult>, SearchVehicleErrorCode>> SearchVehiclesAsync(string? manufacturer, string? model, int? year, VehicleType? vehicleType, int pageNumber, int pageSize, CancellationToken cancellationToken)
        {
            try
            {
                // Retrieve policies from the PolicyProvider
                var retryPolicy = policyProvider.GetRetryPolicyWithoutConcurrencyException();
                var circuitBreakerPolicy = policyProvider.GetCircuitBreakerPolicy();

                // Wrap policies
                var result = await Policy.WrapAsync(retryPolicy, circuitBreakerPolicy).ExecuteAsync(async ct =>
                {
                    var searchResult = await searchRepository.SearchVehiclesAsync(manufacturer, model, year, vehicleType, pageNumber, pageSize, ct);
                    return searchResult;
                }, cancellationToken);

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while searching for vehicles");
                return SuccessOrError<IEnumerable<SearchVehicleResult>, SearchVehicleErrorCode>.Failure(SearchVehicleErrorCode.InternalError);
            }
        }
    }
}
