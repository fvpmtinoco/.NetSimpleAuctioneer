using NetSimpleAuctioneer.API.Domain;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;

namespace NetSimpleAuctioneer.API.Features.Vehicles.AddSUV
{
    /// <summary>
    /// Represents a SUV, a specific type of vehicle with additional properties
    /// Implements the IVehicle interface.
    /// </summary>
    public class SUV : IVehicle
    {
        public Guid Id { get; set; }
        public string Manufacturer { get; set; } = default!;
        public string Model { get; set; } = default!;
        public int Year { get; set; }
        public decimal StartingBid { get; set; }
        public VehicleType VehicleType { get; set; }
        public int NumberOfSeats { get; set; }

        public void MapToEntitySpecificProperties(Vehicle vehicleEntity)
        {
            vehicleEntity.NumberOfSeats = this.NumberOfSeats;
        }
    }
}
