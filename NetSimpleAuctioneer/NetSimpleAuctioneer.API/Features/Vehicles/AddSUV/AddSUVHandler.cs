using MediatR;
using NetSimpleAuctioneer.API.Database;
using NetSimpleAuctioneer.API.Features.Shared;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;

namespace NetSimpleAuctioneer.API.Features.Vehicles.AddSUV
{
    public record AddSUVCommand(Guid Id, string Manufacturer, string Model, int Year, decimal StartingBid, int NumberOfSeats) : IRequest<VoidOrError<AddVehicleErrorCode>>;

    public class AddSUVHandler(IVehicleRepository commonRepository) : IRequestHandler<AddSUVCommand, VoidOrError<AddVehicleErrorCode>>
    {
        public async Task<VoidOrError<AddVehicleErrorCode>> Handle(AddSUVCommand request, CancellationToken cancellationToken)
        {
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

            var result = await commonRepository.AddWithRetryAsync(suv, cancellationToken);

            if (result == null)
                return VoidOrError<AddVehicleErrorCode>.Success();

            return VoidOrError<AddVehicleErrorCode>.Failure(result.Value);
        }
    }
}
