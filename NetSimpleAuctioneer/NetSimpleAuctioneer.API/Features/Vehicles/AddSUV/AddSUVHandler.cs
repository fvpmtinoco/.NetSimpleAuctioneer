using MediatR;
using NetSimpleAuctioneer.API.Features.Shared;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;

namespace NetSimpleAuctioneer.API.Features.Vehicles.AddSUV
{
    public record AddSUVCommand(Guid Id, string Manufacturer, string Model, int Year, decimal StartingBid, int NumberofSeats) : IRequest<VoidOrError<AddVehicleErrorCode>>;

    public class AddSUVHandler : IRequestHandler<AddSUVCommand, VoidOrError<AddVehicleErrorCode>>
    {
        public Task<VoidOrError<AddVehicleErrorCode>> Handle(AddSUVCommand request, CancellationToken cancellationToken)
        {
            var suv = new SUV(request.Id, request.Manufacturer, request.Model, request.Year, request.StartingBid, request.NumberofSeats);

            return Task.FromResult(VoidOrError<AddVehicleErrorCode>.Success());
        }
    }
}
