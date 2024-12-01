using MediatR;
using NetSimpleAuctioneer.API.Features.Shared;

namespace NetSimpleAuctioneer.API.Features.Auctions.StartAuction
{
    public record StartAuctionCommand(Guid VehicleId) : IRequest<VoidOrError<StartAuctionErrorCode>>;

    public class StartAuctionHandler : IRequestHandler<StartAuctionCommand, VoidOrError<StartAuctionErrorCode>>
    {
        public async Task<VoidOrError<StartAuctionErrorCode>> Handle(StartAuctionCommand request, CancellationToken cancellationToken)
        {
            //// Check if the vehicle exists in the inventory
            //var vehicle = await _vehicleRepository.GetVehicleByIdAsync(request.VehicleId);
            //if (vehicle == null)
            //{
            //    return VoidOrError<StartAuctionErrorCode>.Failure(StartAuctionErrorCode.VehicleNotFound);
            //}

            //// Check if the vehicle already has an active auction
            //var existingAuction = await _auctionRepository.GetActiveAuctionByVehicleIdAsync(request.VehicleId);
            //if (existingAuction != null)
            //{
            //    return VoidOrError<StartAuctionErrorCode>.Failure(StartAuctionErrorCode.AuctionAlreadyActive);
            //}

            //// Create and start the auction
            //var auction = new Auction
            //{
            //    VehicleId = request.VehicleId,
            //    StartTime = DateTime.UtcNow,
            //    IsActive = true
            //};

            //await _auctionRepository.AddAuctionAsync(auction);
            return VoidOrError<StartAuctionErrorCode>.Success();
        }
    }
}
