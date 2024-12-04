using MediatR;
using NetSimpleAuctioneer.API.Application;

namespace NetSimpleAuctioneer.API.Features.Auctions.StartAuction
{
    public record StartAuctionCommand(Guid VehicleId) : IRequest<SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>>;
    public record StartAuctionCommandResult(Guid AuctionId);

    public class StartAuctionHandler(IStartAuctionService startAuctionService) : IRequestHandler<StartAuctionCommand, SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>>
    {
        public async Task<SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>> Handle(StartAuctionCommand command, CancellationToken cancellationToken)
        {
            var validationResult = await startAuctionService.ValidateAuctionAsync(command, cancellationToken);

            if (validationResult.HasValue)
                return SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>.Failure(validationResult.Value);

            var result = await startAuctionService.StartAuctionAsync(command.VehicleId, cancellationToken);

            return result;
        }
    }
}
