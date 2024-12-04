using MediatR;
using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;

namespace NetSimpleAuctioneer.API.Features.Vehicles.AddHatchback
{
    public record AddHatchbackCommand(Guid Id, string Manufacturer, string Model, int Year, decimal StartingBid, int NumberOfDoors) : IRequest<VoidOrError<AddVehicleErrorCode>>;

    public class AddHatchbackHandler(IVehicleService vehicleService) : IRequestHandler<AddHatchbackCommand, VoidOrError<AddVehicleErrorCode>>
    {
        public async Task<VoidOrError<AddVehicleErrorCode>> Handle(AddHatchbackCommand command, CancellationToken cancellationToken)
        {
            var validationResult = await vehicleService.ValidateVehicleAsync(command.Id, command.Year);

            if (validationResult != null)
                return VoidOrError<AddVehicleErrorCode>.Failure(validationResult.Value);

            var hatchback = new Hatchback
            {
                Id = command.Id,
                Manufacturer = command.Manufacturer,
                Model = command.Model,
                Year = command.Year,
                StartingBid = command.StartingBid,
                VehicleType = (int)VehicleType.Hatchback,
                NumberOfDoors = command.NumberOfDoors
            };

            var result = await vehicleService.AddVehicleAsync(hatchback, cancellationToken);

            return result;
        }
    }
}
