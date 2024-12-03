using System.ComponentModel;

namespace NetSimpleAuctioneer.API.Features.Vehicles.Shared
{
    /// <summary>
    /// Represents the type of vehicle
    /// </summary>
    public enum VehicleType
    {
        [Description("Hacthback vehicle")]
        Hatchback,
        [Description("Sedan vehicle")]
        Sedan,
        [Description("SUV vehicle")]
        SUV,
        [Description("Truck vehicle")]
        Truck
    }
}