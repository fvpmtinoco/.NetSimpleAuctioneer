using MediatR;
using NetSimpleAuctioneer.API.Database;
using NetSimpleAuctioneer.API.Features.Shared;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;

namespace NetSimpleAuctioneer.API.Features.Vehicles.AddSedan
{
    public record AddSedanCommand(Guid Id, string Manufacturer, string Model, int Year, decimal StartingBid, int NumberOfDoors) : IRequest<VoidOrError<AddVehicleErrorCode>>;

    public class AddSedanHandler(IVehicleRepository commonRepository) : IRequestHandler<AddSedanCommand, VoidOrError<AddVehicleErrorCode>>
    {
        public async Task<VoidOrError<AddVehicleErrorCode>> Handle(AddSedanCommand request, CancellationToken cancellationToken)
        {
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

            var result = await commonRepository.AddWithRetryAsync(sedan, cancellationToken);

            if (result == null)
                return VoidOrError<AddVehicleErrorCode>.Success();

            return VoidOrError<AddVehicleErrorCode>.Failure(result.Value);
        }
    }
}
