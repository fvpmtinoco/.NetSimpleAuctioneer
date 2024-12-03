namespace NetSimpleAuctioneer.API.Features.Vehicles.Shared
{
    public interface IVehicle
    {
        Guid Id { get; }
        string Manufacturer { get; }
        string Model { get; }
        int Year { get; }
        decimal StartingBid { get; }
    }
}