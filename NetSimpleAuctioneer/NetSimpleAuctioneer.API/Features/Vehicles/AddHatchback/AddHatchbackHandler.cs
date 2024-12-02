using MediatR;
using NetSimpleAuctioneer.API.Database;
using NetSimpleAuctioneer.API.Features.Shared;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;

namespace NetSimpleAuctioneer.API.Features.Vehicles.AddHatchback
{
    public record AddHatchbackCommand(Guid Id, string Manufacturer, string Model, int Year, decimal StartingBid, int NumberOfDoors) : IRequest<VoidOrError<AddVehicleErrorCode>>;

    public class AddHatchbackHandler(IVehicleRepository commonRepository) : IRequestHandler<AddHatchbackCommand, VoidOrError<AddVehicleErrorCode>>
    {
        public async Task<VoidOrError<AddVehicleErrorCode>> Handle(AddHatchbackCommand request, CancellationToken cancellationToken)
        {
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

            var result = await commonRepository.AddWithRetryAsync(hatchback, cancellationToken);

            if (result == null)
                return VoidOrError<AddVehicleErrorCode>.Success();

            return VoidOrError<AddVehicleErrorCode>.Failure(result.Value);
        }
    }
}
