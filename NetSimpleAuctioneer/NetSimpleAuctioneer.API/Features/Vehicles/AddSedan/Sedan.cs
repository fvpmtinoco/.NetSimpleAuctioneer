using NetSimpleAuctioneer.API.Features.Vehicles.Shared;

namespace NetSimpleAuctioneer.API.Features.Vehicles.AddSedan
{
    /// <summary>
    /// Represents a Sedan, a specific type of vehicle with additional properties
    /// Implements the IVehicle interface.
    /// </summary>
    public class Sedan : IVehicle
    {
        public Guid Id { get; set; }
        public string Manufacturer { get; set; } = default!;
        public string Model { get; set; } = default!;
        public int Year { get; set; }
        public decimal StartingBid { get; set; }
        public VehicleType VehicleType { get; set; }
        public int NumberOfDoors { get; set; }
    }
}
