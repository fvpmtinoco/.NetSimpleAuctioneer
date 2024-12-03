using MediatR;
using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Database;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;

namespace NetSimpleAuctioneer.API.Features.Vehicles.AddHatchback
{
    public record AddHatchbackCommand(Guid Id, string Manufacturer, string Model, int Year, decimal StartingBid, int NumberOfDoors) : IRequest<VoidOrError<AddVehicleErrorCode>>;

    public class AddHatchbackHandler(IVehicleRepository commonRepository, IVehicleService vehicleService) : IRequestHandler<AddHatchbackCommand, VoidOrError<AddVehicleErrorCode>>
    {
        public async Task<VoidOrError<AddVehicleErrorCode>> Handle(AddHatchbackCommand request, CancellationToken cancellationToken)
        {
            if (!vehicleService.IsVehicleYearValid(request.Year))
                return VoidOrError<AddVehicleErrorCode>.Failure(AddVehicleErrorCode.InvalidYear);

            var hatchback = new Vehicle
            {
                Id = request.Id,
                Manufacturer = request.Manufacturer,
                Model = request.Model,
                Year = request.Year,
                StartingBid = request.StartingBid,
                VehicleType = (int)VehicleType.Hatchback,
                NumberOfDoors = request.NumberOfDoors
            };

            var result = await commonRepository.AddVehicleAsync(hatchback, cancellationToken);

            return result;
        }
    }
}
