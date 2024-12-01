using MediatR;
using NetSimpleAuctioneer.API.Features.Shared;

namespace NetSimpleAuctioneer.API.Features.Auctions.PlaceBid
{
    public record PlaceBidCommand(Guid VehicleId, string BidderName, decimal BidAmount) : IRequest<VoidOrError<PlaceBidErrorCode>>;

    public class PlaceBidHandler : IRequestHandler<PlaceBidCommand, VoidOrError<PlaceBidErrorCode>>
    {
        public async Task<VoidOrError<PlaceBidErrorCode>> Handle(PlaceBidCommand request, CancellationToken cancellationToken)
        {
            //// Retrieve the active auction for the vehicle
            //var auction = await _auctionRepository.GetActiveAuctionByVehicleIdAsync(request.VehicleId);
            //if (auction == null)
            //{
            //    return VoidOrError<PlaceBidErrorCode>.Failure(PlaceBidErrorCode.AuctionNotFound);
            //}

            //// Check if the auction is active
            //if (!auction.IsActive)
            //{
            //    return VoidOrError<PlaceBidErrorCode>.Failure(PlaceBidErrorCode.AuctionNotActive);
            //}

            //// Validate the bid amount
            //var currentHighestBid = auction.Bids.Any() ? auction.Bids.Max(b => b.Amount) : auction.StartingBid;
            //if (request.BidAmount <= currentHighestBid)
            //{
            //    return VoidOrError<PlaceBidErrorCode>.Failure(PlaceBidErrorCode.BidAmountTooLow);
            //}

            //// Place the bid
            //var bid = new Bid
            //{
            //    AuctionId = auction.Id,
            //    BidderName = request.BidderName,
            //    Amount = request.BidAmount,
            //    TimePlaced = DateTime.UtcNow
            //};

            //auction.Bids.Add(bid);
            //await _auctionRepository.UpdateAuctionAsync(auction);

            return VoidOrError<PlaceBidErrorCode>.Success();
        }
    }

}
