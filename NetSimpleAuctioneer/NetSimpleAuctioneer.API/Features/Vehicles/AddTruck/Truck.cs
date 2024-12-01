using NetSimpleAuctioneer.API.Features.Vehicles.Shared;

namespace NetSimpleAuctioneer.API.Features.Vehicles.AddTruck
{
    public record Truck(Guid Id, string Manufacturer, string Model, int Year, decimal StartingBid, int LoadCapacity) : IVehicle;
}
