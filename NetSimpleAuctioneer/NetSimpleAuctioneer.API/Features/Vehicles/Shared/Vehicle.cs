using NetSimpleAuctioneer.API.Database;

namespace NetSimpleAuctioneer.API.Features.Vehicles.Shared
{
    public interface IVehicle
    {
        Guid Id { get; set; }
        string Manufacturer { get; set; }
        string Model { get; set; }
        int Year { get; set; }
        decimal StartingBid { get; set; }
        VehicleType VehicleType { get; set; }

        void MapToEntitySpecificProperties(Vehicle vehicleEntity);
    }
}