using MediatR;
using NetSimpleAuctioneer.API.Features.Shared;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;

namespace NetSimpleAuctioneer.API.Features.Vehicles.AddTruck
{
    public record AddTruckCommand(Guid Id, string Manufacturer, string Model, int Year, decimal StartingBid, int LoadCapacity) : IRequest<VoidOrError<AddVehicleErrorCode>>;

    public class AddTruckHandler : IRequestHandler<AddTruckCommand, VoidOrError<AddVehicleErrorCode>>
    {
        public Task<VoidOrError<AddVehicleErrorCode>> Handle(AddTruckCommand request, CancellationToken cancellationToken)
        {
            var truck = new Truck(request.Id, request.Manufacturer, request.Model, request.Year, request.StartingBid, request.LoadCapacity);

            return Task.FromResult(VoidOrError<AddVehicleErrorCode>.Success());
        }
    }
}
