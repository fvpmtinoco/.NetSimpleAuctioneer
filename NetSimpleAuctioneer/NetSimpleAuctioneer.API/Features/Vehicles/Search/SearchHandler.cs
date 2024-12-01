using MediatR;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;

namespace NetSimpleAuctioneer.API.Features.Vehicles.Search
{
    internal class SearchVehicleResult(Guid id, string type, string manufacturer, string model, int year, Dictionary<string, object> attributes)
    {
        public Guid Id { get; } = id;
        public string Type { get; } = type;
        public string Manufacturer { get; } = manufacturer;
        public string Model { get; } = model;
        public int Year { get; } = year;
        public Dictionary<string, object> Attributes { get; } = attributes;
    }

    public record SearchVehicleQuery(VehicleType? VehicleType, string? Manufacturer, string? Model, int? Year) : IRequest<SearchVehicleResult>;

    public class SearchHandler : IRequestHandler<SearchVehicleQuery, SearchVehicleResult>
    {
        Task<SearchVehicleResult> IRequestHandler<SearchVehicleQuery, SearchVehicleResult>.Handle(SearchVehicleQuery request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
