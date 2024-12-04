using MediatR;
using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;

namespace NetSimpleAuctioneer.API.Features.Vehicles.AddTruck
{
    public record AddTruckCommand(Guid Id, string Manufacturer, string Model, int Year, decimal StartingBid, int LoadCapacity) : IRequest<VoidOrError<AddVehicleErrorCode>>;

    public class AddTruckHandler(IVehicleService vehicleService) : IRequestHandler<AddTruckCommand, VoidOrError<AddVehicleErrorCode>>
    {
        public async Task<VoidOrError<AddVehicleErrorCode>> Handle(AddTruckCommand command, CancellationToken cancellationToken)
        {
            var validationResult = await vehicleService.ValidateVehicleAsync(command.Id, command.Year);

            if (validationResult != null)
                return VoidOrError<AddVehicleErrorCode>.Failure(validationResult.Value);

            var truck = new Truck
            {
                Id = command.Id,
                Manufacturer = command.Manufacturer,
                Model = command.Model,
                Year = command.Year,
                StartingBid = command.StartingBid,
                VehicleType = VehicleType.Truck,
                LoadCapacity = command.LoadCapacity
            };

            var result = await vehicleService.AddVehicleAsync(truck, cancellationToken);

            if (result == null)
                return VoidOrError<AddVehicleErrorCode>.Success();

            return result;
        }
    }
}
