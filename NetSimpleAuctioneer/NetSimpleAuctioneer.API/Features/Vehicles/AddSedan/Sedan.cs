using NetSimpleAuctioneer.API.Features.Vehicles.Shared;

namespace NetSimpleAuctioneer.API.Features.Vehicles.AddSedan
{
    public sealed record Sedan(long Id, string Manufacturer, string Model, int Year, decimal StartingBid, int NumberOfDoors) : IVehicle;
}
