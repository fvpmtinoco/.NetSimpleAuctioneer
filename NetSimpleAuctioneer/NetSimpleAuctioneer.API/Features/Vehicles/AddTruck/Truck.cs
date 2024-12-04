using NetSimpleAuctioneer.API.Features.Vehicles.Shared;

namespace NetSimpleAuctioneer.API.Features.Vehicles.AddTruck
{
    /// <summary>
    /// Represents a truck, a specific type of vehicle with additional properties
    /// Implements the IVehicle interface.
    /// </summary>
    public class Truck : IVehicle
    {
        public Guid Id { get; set; }
        public string Manufacturer { get; set; } = default!;
        public string Model { get; set; } = default!;
        public int Year { get; set; }
        public decimal StartingBid { get; set; }
        public VehicleType VehicleType { get; set; }
        public int LoadCapacity { get; set; }
    }
}
