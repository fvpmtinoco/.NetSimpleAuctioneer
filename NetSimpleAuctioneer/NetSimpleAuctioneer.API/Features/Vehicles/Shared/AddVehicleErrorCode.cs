using System.ComponentModel;

namespace NetSimpleAuctioneer.API.Features.Vehicles.Shared
{
    public enum AddVehicleErrorCode
    {
        [Description("A vehicle with the same identifier already exists")]
        DuplicatedVehicle = 0
    }
}
