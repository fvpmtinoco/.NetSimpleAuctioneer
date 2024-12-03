using System.ComponentModel;

namespace NetSimpleAuctioneer.API.Features.Vehicles.Shared
{
    public enum AddVehicleErrorCode
    {
        [Description("The vehicle's year cannot be above the current year")]
        InvalidYear,
        [Description("A vehicle with the same identifier already exists")]
        DuplicatedVehicle,
        [Description("An unknown error occurred")]
        InternalError
    }
}
