using MediatR;
using NetSimpleAuctioneer.API.Features.Shared;

namespace NetSimpleAuctioneer.API.Features.Auctions.PlaceBid
{
    public record PlaceBidCommand(Guid AuctionId, string BidderEmail, decimal BidAmount) : IRequest<SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>>;
    public record PlaceBidCommandResult(Guid BidId);


    public class PlaceBidHandler(IPlaceBidRepository placeBidRepository) : IRequestHandler<PlaceBidCommand, SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>>
    {
        public async Task<SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>> Handle(PlaceBidCommand request, CancellationToken cancellationToken)
        {
            var result = await placeBidRepository.PlaceBidAsync(request.AuctionId, request.BidderEmail, request.BidAmount, cancellationToken);

            return result;
        }
    }
}
