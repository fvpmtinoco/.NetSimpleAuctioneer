using MediatR;
using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Database;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;

namespace NetSimpleAuctioneer.API.Features.Vehicles.AddSUV
{
    public record AddSUVCommand(Guid Id, string Manufacturer, string Model, int Year, decimal StartingBid, int NumberOfSeats) : IRequest<VoidOrError<AddVehicleErrorCode>>;

    public class AddSUVHandler(IVehicleRepository commonRepository, IVehicleService vehicleService) : IRequestHandler<AddSUVCommand, VoidOrError<AddVehicleErrorCode>>
    {
        public async Task<VoidOrError<AddVehicleErrorCode>> Handle(AddSUVCommand request, CancellationToken cancellationToken)
        {
            if (!vehicleService.IsVehicleYearValid(request.Year))
                return VoidOrError<AddVehicleErrorCode>.Failure(AddVehicleErrorCode.InvalidYear);

            var suv = new Vehicle
            {
                Id = request.Id,
                Manufacturer = request.Manufacturer,
                Model = request.Model,
                Year = request.Year,
                StartingBid = request.StartingBid,
                VehicleType = (int)VehicleType.SUV,
                NumberOfSeats = request.NumberOfSeats
            };

            var result = await commonRepository.AddVehicleAsync(suv, cancellationToken);

            if (result == null)
                return VoidOrError<AddVehicleErrorCode>.Success();

            return result;
        }
    }
}
