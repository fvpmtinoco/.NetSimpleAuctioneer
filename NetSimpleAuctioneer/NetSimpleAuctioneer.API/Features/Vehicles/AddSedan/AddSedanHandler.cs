using MediatR;
using NetSimpleAuctioneer.API.Features.Shared;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;

namespace NetSimpleAuctioneer.API.Features.Vehicles.AddSedan
{
    public record AddSedanCommand(Guid Id, string Manufacturer, string Model, int Year, decimal StartingBid, int NumberOfDoors) : IRequest<VoidOrError<AddVehicleErrorCode>>;

    public class AddSedanHandler : IRequestHandler<AddSedanCommand, VoidOrError<AddVehicleErrorCode>>
    {
        public Task<VoidOrError<AddVehicleErrorCode>> Handle(AddSedanCommand request, CancellationToken cancellationToken)
        {
            var hatchback = new Sedan(request.Id, request.Manufacturer, request.Model, request.Year, request.StartingBid, request.NumberOfDoors);

            // Check if hatchback already exists
            // Common service / repository

            // Any SysClKey? 

            // Save to database

            return Task.FromResult(VoidOrError<AddVehicleErrorCode>.Success());
        }
    }
}
