using Microsoft.EntityFrameworkCore;
using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Domain;
using NetSimpleAuctioneer.API.Infrastructure.Data;

namespace NetSimpleAuctioneer.API.Features.Auctions.StartAuction
{
    public interface IStartAuctionRepository
    {
        /// <summary>
        /// Stores an auction in the database
        /// </summary>
        /// <param name="auction"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>> StartAuctionAsync(Auction auction, CancellationToken cancellationToken);

        /// <summary>
        /// Check if vehicle exists
        /// </summary>
        /// <param name="vehicleId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool?> VehicleExistsAsync(Guid vehicleId, CancellationToken cancellationToken);

        /// <summary>
        /// Check if an active auction exists for a vehicle
        /// </summary>
        /// <param name="vehicleId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool?> ActiveAuctionForVehicleExistsAsync(Guid vehicleId, CancellationToken cancellationToken);
    }
    public class StartAuctionRepository(AuctioneerDbContext context, ILogger<StartAuctionRepository> logger) : IStartAuctionRepository
    {
        public async Task<SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>> StartAuctionAsync(Auction auction, CancellationToken cancellationToken)
        {
            try
            {
                // Add the new auction and save changes
                await context.Auctions.AddAsync(auction, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);

                logger.LogInformation("Auction successfully added for vehicle ID: {VehicleId}", auction.VehicleId);
                return SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>.Success(new StartAuctionCommandResult(auction.Id));

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while creating auction for vehicle ID: {VehicleId}", auction.VehicleId);
                return SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>.Failure(StartAuctionErrorCode.InternalError);
            }
        }

        public async Task<bool?> VehicleExistsAsync(Guid vehicleId, CancellationToken cancellationToken)
        {
            try
            {
                // Using EF to check if the vehicle exists
                var exists = await context.Vehicles
                    .AnyAsync(v => v.Id == vehicleId, cancellationToken);

                return exists;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while retrieving information for vehicle with ID: {VehicleId}", vehicleId);
                return null;
            }
        }

        public async Task<bool?> ActiveAuctionForVehicleExistsAsync(Guid vehicleId, CancellationToken cancellationToken)
        {
            try
            {
                // Using EF to check for an active auction for the given vehicleId
                var activeAuctionExists = await context.Auctions
                    .AnyAsync(a => a.VehicleId == vehicleId && a.EndDate == null, cancellationToken);

                return activeAuctionExists;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while checking for active auction for vehicle with ID: {VehicleId}", vehicleId);
                return null;
            }
        }
    }
}
