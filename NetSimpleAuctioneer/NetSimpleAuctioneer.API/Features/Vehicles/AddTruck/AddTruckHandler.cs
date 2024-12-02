using MediatR;
using NetSimpleAuctioneer.API.Database;
using NetSimpleAuctioneer.API.Features.Shared;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;

namespace NetSimpleAuctioneer.API.Features.Vehicles.AddTruck
{
    public record AddTruckCommand(Guid Id, string Manufacturer, string Model, int Year, decimal StartingBid, int LoadCapacity) : IRequest<VoidOrError<AddVehicleErrorCode>>;

    public class AddTruckHandler(IVehicleRepository commonRepository) : IRequestHandler<AddTruckCommand, VoidOrError<AddVehicleErrorCode>>
    {
        public async Task<VoidOrError<AddVehicleErrorCode>> Handle(AddTruckCommand request, CancellationToken cancellationToken)
        {
            var truck = new Vehicle
            {
                Id = request.Id,
                Manufacturer = request.Manufacturer,
                Model = request.Model,
                Year = request.Year,
                StartingBid = request.StartingBid,
                VehicleType = (int)VehicleType.Truck,
                LoadCapacity = request.LoadCapacity
            };

            var result = await commonRepository.AddWithRetryAsync(truck, cancellationToken);

            if (result == null)
                return VoidOrError<AddVehicleErrorCode>.Success();

            return VoidOrError<AddVehicleErrorCode>.Failure(result.Value);
        }
    }
}
