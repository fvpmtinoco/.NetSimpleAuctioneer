using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Application.Policies;
using NetSimpleAuctioneer.API.Domain;
using Polly;

namespace NetSimpleAuctioneer.API.Features.Auctions.StartAuction
{
    public interface IStartAuctionService
    {
        /// <summary>
        /// Validates if an auction can be started for a vehicle.
        /// </summary>
        /// <param name="vehicleId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>> StartAuctionAsync(Guid vehicleId, CancellationToken cancellationToken);

        /// <summary>
        /// Validates if an auction can be started for a vehicle.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<StartAuctionErrorCode?> ValidateAuctionAsync(StartAuctionCommand command, CancellationToken cancellationToken);
    }

    public class StartAuctionService(IStartAuctionRepository startAuctionRepository, ILogger<StartAuctionService> logger, IPolicyProvider policyProvider) : IStartAuctionService
    {
        public async Task<SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>> StartAuctionAsync(Guid vehicleId, CancellationToken cancellationToken)
        {
            try
            {
                // Retrieve policies from the PolicyProvider
                var retryPolicy = policyProvider.GetRetryPolicyWithoutConcurrencyException();
                var circuitBreakerPolicy = policyProvider.GetCircuitBreakerPolicy();

                // Wrap policies
                var result = await Policy.WrapAsync(retryPolicy, circuitBreakerPolicy).ExecuteAsync(async ct =>
                {
                    var auction = new Auction
                    {
                        VehicleId = vehicleId,
                        StartDate = DateTime.UtcNow
                    };

                    // Create new auction
                    var auctionResult = await startAuctionRepository.StartAuctionAsync(auction, ct);
                    return auctionResult;

                }, cancellationToken);

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while creating auction for vehicle ID: {VehicleId}", vehicleId);
                return SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>.Failure(StartAuctionErrorCode.InternalError);
            }
        }

        public async Task<StartAuctionErrorCode?> ValidateAuctionAsync(StartAuctionCommand command, CancellationToken cancellationToken)
        {
            var vehicleExists = await startAuctionRepository.VehicleExistsAsync(command.VehicleId, cancellationToken);
            if (vehicleExists is null)
                return StartAuctionErrorCode.InternalError;

            if (!vehicleExists.Value)
            {
                logger.LogWarning("Vehicle with ID {VehicleId} not found.", command.VehicleId);
                return StartAuctionErrorCode.InvalidVehicle;
            }

            var activeAuctionForVehicle = await startAuctionRepository.ActiveAuctionForVehicleExistsAsync(command.VehicleId, cancellationToken);
            if (activeAuctionForVehicle is null)
                return StartAuctionErrorCode.InternalError;

            if (activeAuctionForVehicle.Value)
            {
                logger.LogWarning("An active auction already exists for vehicle with ID {VehicleId}.", command.VehicleId);
                return StartAuctionErrorCode.AuctionForVehicleAlreadyActive;
            }

            // All ok
            return null;
        }
    }
}
