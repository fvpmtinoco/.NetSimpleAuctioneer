using MediatR;
using NetSimpleAuctioneer.API.Application;

namespace NetSimpleAuctioneer.API.Features.Auctions.PlaceBid
{
    public record PlaceBidCommand(Guid AuctionId, string BidderEmail, decimal BidAmount) : IRequest<SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>>;
    public record PlaceBidCommandResult(Guid BidId);

    public class PlaceBidHandler(IPlaceBidService placeBidService) : IRequestHandler<PlaceBidCommand, SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>>
    {
        public async Task<SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>> Handle(PlaceBidCommand command, CancellationToken cancellationToken)
        {
            var validationResult = await placeBidService.ValidateAuctionAsync(command, cancellationToken);

            if (validationResult.HasValue)
                return SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>.Failure(validationResult.Value);

            var result = await placeBidService.PlaceBidAsync(command, cancellationToken);

            return result;
        }
    }
}
