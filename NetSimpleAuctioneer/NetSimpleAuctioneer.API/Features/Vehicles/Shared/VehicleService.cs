using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Application.Policies;
using NetSimpleAuctioneer.API.Database;
using NetSimpleAuctioneer.API.Features.Vehicles.AddHatchback;
using NetSimpleAuctioneer.API.Features.Vehicles.AddSedan;
using NetSimpleAuctioneer.API.Features.Vehicles.AddSUV;
using NetSimpleAuctioneer.API.Features.Vehicles.AddTruck;
using Polly;

namespace NetSimpleAuctioneer.API.Features.Vehicles.Shared
{
    public interface IVehicleService
    {
        /// <summary>
        /// Add a vehicle to the system
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<VoidOrError<AddVehicleErrorCode>> AddVehicleAsync(IVehicle vehicle, CancellationToken cancellationToken);

        /// <summary>
        /// Check if the year of the vehicle is valid and if doesn't already exists
        /// </summary>
        /// <param name="vehicleId"></param>
        /// <param name="year"></param>
        /// <returns></returns>
        Task<AddVehicleErrorCode?> ValidateVehicleAsync(Guid vehicleId, int year);
    }
    public class VehicleService(IVehicleRepository vehicleRepository, ILogger<VehicleService> logger, IPolicyProvider policyProvider) : IVehicleService
    {
        public async Task<VoidOrError<AddVehicleErrorCode>> AddVehicleAsync(IVehicle vehicle, CancellationToken cancellationToken)
        {
            try
            {
                // Retrieve policies from the PolicyProvider
                var retryPolicy = policyProvider.GetRetryPolicyWithoutConcurrencyException();
                var circuitBreakerPolicy = policyProvider.GetCircuitBreakerPolicy();

                // Map the IVehicle to the entity (Vehicle)
                var vehicleEntity = VehicleMapper.MapToEntity(vehicle);

                // Wrap policies
                var result = await Policy.WrapAsync(retryPolicy, circuitBreakerPolicy).ExecuteAsync(async ct =>
                {
                    var result = await vehicleRepository.AddVehicleAsync(vehicleEntity, cancellationToken);
                    return result;

                }, cancellationToken);

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while creating vehicle with ID: {VehicleId}", vehicle.Id);
                return VoidOrError<AddVehicleErrorCode>.Failure(AddVehicleErrorCode.InternalError);
            }
        }

        public async Task<AddVehicleErrorCode?> ValidateVehicleAsync(Guid vehicleId, int year)
        {
            if (year < 1900 || year > DateTime.UtcNow.Year)
                return AddVehicleErrorCode.InvalidYear;

            var vehicleExists = await vehicleRepository.VehicleExistsAsync(vehicleId, CancellationToken.None);

            if (!vehicleExists.HasValue)
                return AddVehicleErrorCode.InternalError;

            if (vehicleExists.Value)
            {
                logger.LogWarning("A vehicle already exists with ID {VehicleId}.", vehicleId);
                return AddVehicleErrorCode.DuplicatedVehicle;
            }

            //All ok
            return null;
        }
    }

    public class VehicleMapper
    {
        public static Vehicle MapToEntity(IVehicle vehicle)
        {
            // Map shared properties from IVehicle to Vehicle
            var vehicleEntity = new Vehicle
            {
                Id = vehicle.Id,
                Manufacturer = vehicle.Manufacturer,
                Model = vehicle.Model,
                Year = vehicle.Year,
                StartingBid = vehicle.StartingBid,
                VehicleType = (int)vehicle.VehicleType
            };

            // Specific logic for different types of vehicles
            switch (vehicle)
            {
                case Truck truck:
                    vehicleEntity.LoadCapacity = truck.LoadCapacity;
                    break;
                case Sedan sedan:
                    vehicleEntity.NumberOfDoors = sedan.NumberOfDoors;
                    break;
                case Hatchback hatchback:
                    vehicleEntity.NumberOfDoors = hatchback.NumberOfDoors;
                    break;
                case SUV suv:
                    vehicleEntity.NumberOfSeats = suv.NumberOfSeats;
                    break;
            }

            return vehicleEntity;
        }
    }
}
