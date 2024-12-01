using NetSimpleAuctioneer.API.Features.Vehicles.Shared;

namespace NetSimpleAuctioneer.API.Features.Vehicles.AddHatchback
{
    public record Hatchback(long Id, string Manufacturer, string Model, int Year, decimal StartingBid, int NumberOfDoors) : IVehicle;
}
