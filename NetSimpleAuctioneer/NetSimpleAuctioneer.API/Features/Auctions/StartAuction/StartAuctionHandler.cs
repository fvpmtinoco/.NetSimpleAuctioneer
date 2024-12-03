using MediatR;
using NetSimpleAuctioneer.API.Features.Shared;

namespace NetSimpleAuctioneer.API.Features.Auctions.StartAuction
{
    public record StartAuctionCommand(Guid VehicleId) : IRequest<SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>>;
    public record StartAuctionCommandResult(Guid AuctionId);

    public class StartAuctionHandler(IStartAuctionRepository startAuctionRepository) : IRequestHandler<StartAuctionCommand, SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>>
    {
        public async Task<SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>> Handle(StartAuctionCommand request, CancellationToken cancellationToken)
        {
            var result = await startAuctionRepository.StartAuctionAsync(request.VehicleId, cancellationToken);

            return result;
        }
    }
}
