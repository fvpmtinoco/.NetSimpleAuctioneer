using Microsoft.EntityFrameworkCore;
using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Infrastructure.Data;

namespace NetSimpleAuctioneer.API.Features.Auctions.CloseAuction
{
    public interface ICloseAuctionRepository
    {
        /// <summary>
        /// Closes an auction by updating its end date
        /// </summary>
        /// <param name="auctionId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>> CloseAuctionAsync(Guid auctionId, CancellationToken cancellationToken);

        /// <summary>
        /// Get auction by it's identification
        /// </summary>
        /// <param name="auctionId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<(Guid? auctionId, DateTime? endDate)?> GetAuctionByIdAsync(Guid auctionId, CancellationToken cancellationToken);
    }

    public class CloseAuctionRepository(AuctioneerDbContext context, ILogger<CloseAuctionRepository> logger) : ICloseAuctionRepository
    {
        public async Task<SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>> CloseAuctionAsync(Guid auctionId, CancellationToken cancellationToken)
        {
            try
            {
                // Double checking
                var auction = await context.Auctions.SingleOrDefaultAsync(a => a.Id == auctionId, cancellationToken);
                if (auction == null)
                    return SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>.Failure(CloseAuctionErrorCode.InvalidAuction);
                if (auction.EndDate != null)
                    return SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>.Failure(CloseAuctionErrorCode.AuctionAlreadyClosed);

                auction.EndDate = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);

                return SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>.Success(new CloseAuctionCommandResult(auctionId));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error closing auction with ID: {AuctionId}", auctionId);
                return SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>.Failure(CloseAuctionErrorCode.InternalError);
            }
        }

        public async Task<(Guid? auctionId, DateTime? endDate)?> GetAuctionByIdAsync(Guid auctionId, CancellationToken cancellationToken)
        {
            try
            {
                var auction = await context.Auctions
                    .Where(a => a.Id == auctionId)
                    .Select(a => new { a.Id, a.EndDate })
                    .SingleOrDefaultAsync(cancellationToken);

                if (auction == null)
                    return (null, null);

                return (auction.Id, auction.EndDate);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving information from auction with ID: {AuctionId}", auctionId);
                return null;
            }
        }
    }
}