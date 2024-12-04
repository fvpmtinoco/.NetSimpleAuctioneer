using MediatR;
using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;

namespace NetSimpleAuctioneer.API.Features.Vehicles.AddSUV
{
    public record AddSUVCommand(Guid Id, string Manufacturer, string Model, int Year, decimal StartingBid, int NumberOfSeats) : IRequest<VoidOrError<AddVehicleErrorCode>>;

    public class AddSUVHandler(IVehicleService vehicleService) : IRequestHandler<AddSUVCommand, VoidOrError<AddVehicleErrorCode>>
    {
        public async Task<VoidOrError<AddVehicleErrorCode>> Handle(AddSUVCommand command, CancellationToken cancellationToken)
        {
            var validationResult = await vehicleService.ValidateVehicleAsync(command.Id, command.Year);

            if (validationResult != null)
                return VoidOrError<AddVehicleErrorCode>.Failure(validationResult.Value);

            var suv = new SUV
            {
                Id = command.Id,
                Manufacturer = command.Manufacturer,
                Model = command.Model,
                Year = command.Year,
                StartingBid = command.StartingBid,
                VehicleType = VehicleType.SUV,
                NumberOfSeats = command.NumberOfSeats
            };

            var result = await vehicleService.AddVehicleAsync(suv, cancellationToken);

            if (result == null)
                return VoidOrError<AddVehicleErrorCode>.Success();

            return result;
        }
    }
}
