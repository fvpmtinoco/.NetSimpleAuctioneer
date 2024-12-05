using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Domain;
using NetSimpleAuctioneer.API.Infrastructure.Configuration;
using NetSimpleAuctioneer.API.Infrastructure.Data;
using Npgsql;

namespace NetSimpleAuctioneer.API.Features.Vehicles.Shared
{
    public interface IVehicleRepository
    {
        /// <summary>
        /// Adds a vehicle to the database
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<VoidOrError<AddVehicleErrorCode>> AddVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken);

        /// <summary>
        /// Check if a vehicle exists in the database
        /// </summary>
        /// <param name="vehicleId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool?> VehicleExistsAsync(Guid vehicleId, CancellationToken cancellationToken);
    }

    public class VehicleRepository(AuctioneerDbContext context, ILogger<VehicleRepository> logger, IOptions<ConnectionStrings> connectionStrings, IDatabaseConnection dbConnection) : IVehicleRepository
    {
        public async Task<VoidOrError<AddVehicleErrorCode>> AddVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken)
        {
            try
            {
                context.Vehicles.Add(vehicle);
                await context.SaveChangesAsync(cancellationToken);

                return VoidOrError<AddVehicleErrorCode>.Success();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
            {
                // Specific constraint error in PostGresSQL, just to prevent racing conditions
                logger.LogWarning("Attempted to add a vehicle with a duplicate ID {VehicleId}", vehicle.Id);
                return VoidOrError<AddVehicleErrorCode>.Failure(AddVehicleErrorCode.DuplicatedVehicle);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unknown error occurred while adding a vehicle with ID {VehicleId}", vehicle.Id);
                return VoidOrError<AddVehicleErrorCode>.Failure(AddVehicleErrorCode.InternalError);
            }
        }

        public async Task<bool?> VehicleExistsAsync(Guid vehicleId, CancellationToken cancellationToken)
        {
            try
            {
                string query = "SELECT EXISTS (SELECT 1 FROM auction WHERE vehicleid = @vehicleId and enddate is null)";
                await using var connection = new NpgsqlConnection(connectionStrings.Value.AuctioneerDBConnectionString);
                var command = new CommandDefinition(query, new { vehicleId }, cancellationToken: cancellationToken);

                var result = await dbConnection.QuerySingleOrDefaultAsync<bool>(command);

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while checking for active auction for vehicle with ID: {VehicleId}", vehicleId);
                return null;
            }
        }
    }
}
