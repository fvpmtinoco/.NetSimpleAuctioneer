using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Application.Policies;
using Polly;

namespace NetSimpleAuctioneer.API.Features.Auctions.CloseAuction
{
    public interface ICloseAuctionService
    {
        /// <summary>
        /// Closes an auction by updating its end date
        /// </summary>
        /// <param name="auctionId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>> CloseAuctionAsync(Guid auctionId, CancellationToken cancellationToken);

        /// <summary>
        /// Validates if an auction exists and it isn't already closed
        /// </summary>
        /// <param name="auctionId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<CloseAuctionErrorCode?> ValidateAuctionAsync(Guid auctionId, CancellationToken cancellationToken);
    }

    public class CloseAuctionService(ICloseAuctionRepository repository, IPolicyProvider policyProvider, ILogger<CloseAuctionService> logger) : ICloseAuctionService
    {
        public async Task<SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>> CloseAuctionAsync(Guid auctionId, CancellationToken cancellationToken)
        {
            try
            {
                // Retrieve the policies for retry and circuit breaker
                var retryPolicy = policyProvider.GetRetryPolicyWithoutConcurrencyException();
                var circuitBreakerPolicy = policyProvider.GetCircuitBreakerPolicy();

                // Apply the policies for resilience
                var result = await Policy.WrapAsync(retryPolicy, circuitBreakerPolicy).ExecuteAsync(async ct =>
                {
                    // Pass validated auction to repository to actually close it
                    return await repository.CloseAuctionAsync(auctionId, ct);
                }, cancellationToken);

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error closing auction with ID: {AuctionId}", auctionId);
                return SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>.Failure(CloseAuctionErrorCode.InternalError);
            }
        }

        public async Task<CloseAuctionErrorCode?> ValidateAuctionAsync(Guid auctionId, CancellationToken cancellationToken)
        {
            // Validate auction status: Ensure auction exists and is open
            var auctionInfo = await repository.GetAuctionByIdAsync(auctionId, cancellationToken);
            if (auctionInfo is null)
                return CloseAuctionErrorCode.InternalError;

            if (!auctionInfo.Value.auctionId.HasValue)
            {
                logger.LogWarning("Auction with ID {AuctionId} not found.", auctionId);
                return CloseAuctionErrorCode.InvalidAuction;
            }

            if (auctionInfo.Value.endDate.HasValue)
            {
                logger.LogWarning("Auction with ID {AuctionId} is already closed.", auctionId);
                return CloseAuctionErrorCode.AuctionAlreadyClosed;
            }

            return null;
        }
    }
}
