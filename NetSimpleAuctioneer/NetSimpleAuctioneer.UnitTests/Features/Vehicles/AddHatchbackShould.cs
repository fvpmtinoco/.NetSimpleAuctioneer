using AutoFixture;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Application.Policies;
using NetSimpleAuctioneer.API.Domain;
using NetSimpleAuctioneer.API.Features.Vehicles.AddHatchback;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;
using NetSimpleAuctioneer.API.Infrastructure.Data;
using Polly;

namespace NetSimpleAuctioneer.UnitTests.Features.Vehicles
{
    public class AddHatchbackShould
    {
        private readonly Mock<ILogger<VehicleRepository>> mockLoggerRepository;
        private readonly Mock<ILogger<VehicleService>> mockLoggerService;
        private readonly Mock<IPolicyProvider> mockPolicyProvider;
        private readonly VehicleRepository repository;
        private readonly AuctioneerDbContext context;
        private readonly AsyncPolicy mockRetryPolicy;
        private readonly AsyncPolicy mockCircuitBreakerPolicy;
        private readonly Fixture fixture;
        private readonly Mock<IVehicleService> mockVehicleService;
        private readonly Mock<IVehicleRepository> mockRepository;

        public AddHatchbackShould()
        {
            // Mock dependencies
            mockLoggerRepository = new Mock<ILogger<VehicleRepository>>();
            mockLoggerService = new Mock<ILogger<VehicleService>>();
            mockPolicyProvider = new Mock<IPolicyProvider>();
            mockVehicleService = new Mock<IVehicleService>();
            mockRepository = new Mock<IVehicleRepository>();

            // Set up an in-memory database for testing
            var options = new DbContextOptionsBuilder<AuctioneerDbContext>()
                            .UseInMemoryDatabase(databaseName: "TestDatabase")
                            .Options;

            // Use the in-memory DbContext
            context = new AuctioneerDbContext(options);

            // Directly create and mock concrete AsyncPolicy
            mockRetryPolicy = Policy.NoOpAsync();  // This is a simple NoOp policy
            mockCircuitBreakerPolicy = Policy.NoOpAsync();

            // Set up the mock to return concrete policies
            mockPolicyProvider.Setup(x => x.GetRetryPolicyWithoutConcurrencyException()).Returns(mockRetryPolicy);
            mockPolicyProvider.Setup(x => x.GetCircuitBreakerPolicy()).Returns(mockCircuitBreakerPolicy);

            repository = new VehicleRepository(
                context,
                mockLoggerRepository.Object
            );

            fixture = new Fixture();
            fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        }

        [Fact]
        public async Task RepositoryAddVehicleShouldAddVehicleSuccessfully()
        {
            // Arrange
            var vehicle = fixture.Create<Vehicle>();
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await repository.AddVehicleAsync(vehicle, cancellationToken);

            // Assert
            result.HasError.Should().BeFalse();
            var addedVehicle = await context.Vehicles.FindAsync(vehicle.Id);
            addedVehicle.Should().NotBeNull();
            addedVehicle.Should().BeEquivalentTo(vehicle);
        }

        [Fact]
        public async Task RepositoryAddVehicleShouldReturnInternalForDuplicateId()
        {
            // Arrange
            var vehicle = fixture.Create<Vehicle>();
            var cancellationToken = CancellationToken.None;

            // Add the vehicle once to simulate it already being in the database
            await repository.AddVehicleAsync(vehicle, cancellationToken);

            // Act
            var result = await repository.AddVehicleAsync(vehicle, cancellationToken);

            // Assert
            result.HasError.Should().BeTrue();  // Ensure an error occurred
            result.Error.Should().Be(AddVehicleErrorCode.InternalError);
        }

        [Fact]
        public async Task ServiceAddVehicleShouldReturnInternalErrorOnException()
        {
            // Arrange
            var vehicle = fixture.Create<Hatchback>();
            var cancellationToken = CancellationToken.None;

            // Mock IPolicyProvider - No policies so it will throw reference not set
            var mockPolicyProvider = new Mock<IPolicyProvider>();
            var service = new VehicleService(mockRepository.Object, mockLoggerService.Object, mockPolicyProvider.Object);

            // Act
            var result = await service.AddVehicleAsync(vehicle, cancellationToken);

            // Assert
            result.HasError.Should().BeTrue();  // Ensure an error occurred
            result.Error.Should().Be(AddVehicleErrorCode.InternalError);  // Ensure the error code is InternalError
        }

        [Fact]
        public async Task HandleShouldReturnSuccessWhenValidationsPass()
        {
            // Arrange
            var command = fixture.Create<AddHatchbackCommand>();
            var cancellationToken = CancellationToken.None;

            mockVehicleService.Setup(x => x.ValidateVehicleAsync(command.Id, command.Year)).ReturnsAsync((AddVehicleErrorCode?)null);

            mockVehicleService.Setup(repo => repo.AddVehicleAsync(It.IsAny<Hatchback>(), cancellationToken))
                .ReturnsAsync(VoidOrError<AddVehicleErrorCode>.Success());

            var handler = new AddHatchbackHandler(mockVehicleService.Object);

            // Act
            var result = await handler.Handle(command, cancellationToken);

            // Assert
            result.HasError.Should().BeFalse();
        }

        [Fact]
        public async Task HandleShouldReturnInvalidYearErrorWhenVehicleYearIsInvalid()
        {
            // Arrange
            var command = fixture.Create<AddHatchbackCommand>();
            var cancellationToken = CancellationToken.None;

            mockVehicleService.Setup(x => x.ValidateVehicleAsync(command.Id, command.Year)).ReturnsAsync(AddVehicleErrorCode.InvalidYear);

            var handler = new AddHatchbackHandler(mockVehicleService.Object);

            // Act
            var result = await handler.Handle(command, cancellationToken);

            // Assert
            result.HasError.Should().BeTrue();
            result.Error.Should().Be(AddVehicleErrorCode.InvalidYear);
        }

        [Fact]
        public async Task HandleShouldReturnDuplicatedVehicleErrorWhenVehicleAlreadyExists()
        {
            // Arrange
            var command = fixture.Create<AddHatchbackCommand>();
            var cancellationToken = CancellationToken.None;

            // Mock the IVehicleService to return true for a valid year
            mockVehicleService.Setup(x => x.ValidateVehicleAsync(command.Id, command.Year)).ReturnsAsync(AddVehicleErrorCode.DuplicatedVehicle);

            var handler = new AddHatchbackHandler(mockVehicleService.Object);

            // Act
            var result = await handler.Handle(command, cancellationToken);

            // Assert
            result.HasError.Should().BeTrue();
            result.Error.Should().Be(AddVehicleErrorCode.DuplicatedVehicle);
        }

        [Fact]
        public void MapToEntityShouldMapHatchbackToVehicle()
        {
            // Arrange
            var hatchback = new Hatchback
            {
                Id = Guid.NewGuid(),
                Manufacturer = "HatchbackManufacturer",
                Model = "HatchbackModel",
                Year = 2022,
                StartingBid = 10000m,
                VehicleType = VehicleType.Hatchback,
                NumberOfDoors = 2
            };

            // Act
            var vehicleEntity = VehicleMapper.MapToEntity(hatchback);

            // Assert
            vehicleEntity.Id.Should().Be(hatchback.Id);
            vehicleEntity.Manufacturer.Should().Be(hatchback.Manufacturer);
            vehicleEntity.Model.Should().Be(hatchback.Model);
            vehicleEntity.Year.Should().Be(hatchback.Year);
            vehicleEntity.StartingBid.Should().Be(hatchback.StartingBid);
            vehicleEntity.VehicleType.Should().Be((int)hatchback.VehicleType);
            vehicleEntity.NumberOfDoors.Should().Be(hatchback.NumberOfDoors);
        }
    }
}
