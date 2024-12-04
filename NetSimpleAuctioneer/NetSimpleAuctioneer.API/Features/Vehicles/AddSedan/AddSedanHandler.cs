using MediatR;
using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;

namespace NetSimpleAuctioneer.API.Features.Vehicles.AddSedan
{
    public record AddSedanCommand(Guid Id, string Manufacturer, string Model, int Year, decimal StartingBid, int NumberOfDoors) : IRequest<VoidOrError<AddVehicleErrorCode>>;

    public class AddSedanHandler(IVehicleService vehicleService) : IRequestHandler<AddSedanCommand, VoidOrError<AddVehicleErrorCode>>
    {
        public async Task<VoidOrError<AddVehicleErrorCode>> Handle(AddSedanCommand command, CancellationToken cancellationToken)
        {
            var validationResult = await vehicleService.ValidateVehicleAsync(command.Id, command.Year);

            if (validationResult != null)
                return VoidOrError<AddVehicleErrorCode>.Failure(validationResult.Value);

            var sedan = new Sedan
            {
                Id = command.Id,
                Manufacturer = command.Manufacturer,
                Model = command.Model,
                Year = command.Year,
                StartingBid = command.StartingBid,
                VehicleType = VehicleType.Sedan,
                NumberOfDoors = command.NumberOfDoors
            };

            var result = await vehicleService.AddVehicleAsync(sedan, cancellationToken);

            return result;
        }
    }
}
