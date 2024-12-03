using MediatR;
using NetSimpleAuctioneer.API.Features.Shared;

namespace NetSimpleAuctioneer.API.Features.Auctions.StartAuction
{
    public record StartAuctionCommand(Guid VehicleId) : IRequest<SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>>;
    public record StartAuctionCommandResult(Guid AuctionId);

    public class StartAuctionHandler(IStartAuctionRepository startAuctionRepository, ILogger<IStartAuctionRepository> logger) : IRequestHandler<StartAuctionCommand, SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>>
    {
        public async Task<SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>> Handle(StartAuctionCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await ValidateRequestAsync(request.VehicleId, cancellationToken);

            if (validationResult != null)
                return validationResult;

            var result = await startAuctionRepository.StartAuctionAsync(request.VehicleId, cancellationToken);

            if (result.HasError)
                logger.LogError("Failed to start auction for vehicle ID {VehicleId}. Error: {ErrorCode}.", request.VehicleId, result.Error);
            else
                logger.LogInformation("Auction successfully started for vehicle ID {VehicleId}.", request.VehicleId);

            return result;
        }

        private async Task<SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>?> ValidateRequestAsync(Guid vehicleId, CancellationToken cancellationToken)
        {
            // Check if the vehicle exists
            var vehicle = await startAuctionRepository.GetVehicleByIdAsync(vehicleId, cancellationToken);
            if (vehicle == null)
            {
                logger.LogWarning("Vehicle with ID {VehicleId} not found.", vehicleId);
                return SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>.Failure(StartAuctionErrorCode.VehicleNotFound);
            }

            // Check if there is already an active auction for the vehicle
            var existingAuction = await startAuctionRepository.GetActiveAuctionByVehicleIdAsync(vehicleId, cancellationToken);
            if (existingAuction != null)
            {
                logger.LogWarning("An active auction already exists for vehicle ID {VehicleId}.", vehicleId);
                return SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>.Failure(StartAuctionErrorCode.AuctionAlreadyActive);
            }

            return null;
        }
    }
}
