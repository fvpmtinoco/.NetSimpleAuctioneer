using MediatR;
using NetSimpleAuctioneer.API.Features.Shared;

namespace NetSimpleAuctioneer.API.Features.Auctions.CloseAuction
{
    public record CloseAuctionCommand(Guid VehicleId) : IRequest<VoidOrError<CloseAuctionErrorCode>>;

    public class CloseAuctionHandler : IRequestHandler<CloseAuctionCommand, VoidOrError<CloseAuctionErrorCode>>
    {
        public async Task<VoidOrError<CloseAuctionErrorCode>> Handle(CloseAuctionCommand request, CancellationToken cancellationToken)
        {
            //// Retrieve the active auction for the vehicle
            //var auction = await _auctionRepository.GetActiveAuctionByVehicleIdAsync(request.VehicleId);
            //if (auction == null)
            //{
            //    return VoidOrError<CloseAuctionErrorCode>.Failure(CloseAuctionErrorCode.AuctionNotFound);
            //}

            //// Check if the auction is already closed
            //if (!auction.IsActive)
            //{
            //    return VoidOrError<CloseAuctionErrorCode>.Failure(CloseAuctionErrorCode.AuctionAlreadyClosed);
            //}

            //// Close the auction
            //auction.IsActive = false;
            //auction.EndTime = DateTime.UtcNow;

            //await _auctionRepository.UpdateAuctionAsync(auction);

            return VoidOrError<CloseAuctionErrorCode>.Success();
        }
    }

}
