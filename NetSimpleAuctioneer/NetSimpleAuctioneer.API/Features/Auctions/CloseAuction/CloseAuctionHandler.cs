using MediatR;
using NetSimpleAuctioneer.API.Application;

namespace NetSimpleAuctioneer.API.Features.Auctions.CloseAuction
{
    public record CloseAuctionCommand(Guid AuctionId) : IRequest<SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>>;
    public record CloseAuctionCommandResult(Guid AuctionId);

    public class CloseAuctionHandler(ICloseAuctionService closeAuctionService) : IRequestHandler<CloseAuctionCommand, SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>>
    {
        public async Task<SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>> Handle(CloseAuctionCommand request, CancellationToken cancellationToken)
        {
            var error = await closeAuctionService.ValidateAuctionAsync(request.AuctionId, cancellationToken);

            if (error != null)
                return SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>.Failure(error.Value);

            var result = await closeAuctionService.CloseAuctionAsync(request.AuctionId, cancellationToken);

            return result;
        }
    }
}
