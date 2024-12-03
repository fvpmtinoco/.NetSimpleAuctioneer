using MediatR;
using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;

namespace NetSimpleAuctioneer.API.Features.Vehicles.Search
{
    public record SearchVehicleResult
    {
        public Guid Id { get; init; }
        public VehicleType VehicleType { get; init; }
        public string Manufacturer { get; init; } = default!;
        public string Model { get; init; } = default!;
        public int Year { get; init; }
        public decimal StartingBid { get; init; }
        public Guid? AuctionId { get; init; }
    }

    public record SearchVehicleQuery(VehicleType? VehicleType, string? Manufacturer, string? Model, int? Year, int PageNumber, int PageSize) : IRequest<SuccessOrError<IEnumerable<SearchVehicleResult>, SearchVehicleErrorCode>>;

    public class SearchHandler(ISearchRepository searchRepository, IVehicleService vehicleService) : IRequestHandler<SearchVehicleQuery, SuccessOrError<IEnumerable<SearchVehicleResult>, SearchVehicleErrorCode>>
    {
        public async Task<SuccessOrError<IEnumerable<SearchVehicleResult>, SearchVehicleErrorCode>> Handle(SearchVehicleQuery request, CancellationToken cancellationToken)
        {
            if (request.Year.HasValue && !vehicleService.IsVehicleYearValid(request.Year.Value))
                return SuccessOrError<IEnumerable<SearchVehicleResult>, SearchVehicleErrorCode>.Failure(SearchVehicleErrorCode.InvalidYear);

            var result = await searchRepository.SearchVehiclesAsync(request.Manufacturer, request.Model, request.Year, request.VehicleType, request.PageNumber, request.PageSize, cancellationToken);

            return result;
        }
    }
}
