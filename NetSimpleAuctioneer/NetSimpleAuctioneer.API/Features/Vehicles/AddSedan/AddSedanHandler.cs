using MediatR;
using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Database;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;

namespace NetSimpleAuctioneer.API.Features.Vehicles.AddSedan
{
    public record AddSedanCommand(Guid Id, string Manufacturer, string Model, int Year, decimal StartingBid, int NumberOfDoors) : IRequest<VoidOrError<AddVehicleErrorCode>>;

    public class AddSedanHandler(IVehicleRepository commonRepository, IVehicleService vehicleService) : IRequestHandler<AddSedanCommand, VoidOrError<AddVehicleErrorCode>>
    {
        public async Task<VoidOrError<AddVehicleErrorCode>> Handle(AddSedanCommand request, CancellationToken cancellationToken)
        {
            if (!vehicleService.IsVehicleYearValid(request.Year))
                return VoidOrError<AddVehicleErrorCode>.Failure(AddVehicleErrorCode.InvalidYear);

            var sedan = new Vehicle
            {
                Id = request.Id,
                Manufacturer = request.Manufacturer,
                Model = request.Model,
                Year = request.Year,
                StartingBid = request.StartingBid,
                VehicleType = (int)VehicleType.Sedan,
                NumberOfDoors = request.NumberOfDoors
            };

            var result = await commonRepository.AddVehicleAsync(sedan, cancellationToken);

            if (result == null)
                return VoidOrError<AddVehicleErrorCode>.Success();

            return result;
        }
    }
}
