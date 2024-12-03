using MediatR;
using NetSimpleAuctioneer.API.Features.Shared;

namespace NetSimpleAuctioneer.API.Features.Auctions.CloseAuction
{
    public record CloseAuctionCommand(Guid AuctionId) : IRequest<SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>>;
    public record CloseAuctionCommandResult(Guid AuctionId);

    public class CloseAuctionHandler(ICloseAuctionRepository closeAuctionRepository) : IRequestHandler<CloseAuctionCommand, SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>>
    {
        public async Task<SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>> Handle(CloseAuctionCommand request, CancellationToken cancellationToken)
        {
            // Delegate the work to the repository
            var result = await closeAuctionRepository.CloseAuctionAsync(request.AuctionId, cancellationToken);

            return result;
        }
    }
}
