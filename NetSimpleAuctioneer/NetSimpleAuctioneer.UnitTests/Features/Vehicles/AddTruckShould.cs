using AutoFixture;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Application.Policies;
using NetSimpleAuctioneer.API.Database;
using NetSimpleAuctioneer.API.Features.Vehicles.AddTruck;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;
using Polly;

namespace NetSimpleAuctioneer.UnitTests.Features.Vehicles
{
    public class AddTruckShould
    {
        private readonly Mock<ILogger<VehicleRepository>> mockLogger;
        private readonly Mock<IPolicyProvider> mockPolicyProvider;
        private readonly VehicleRepository repository;
        private readonly AuctioneerDbContext context;
        private readonly AsyncPolicy mockRetryPolicy;
        private readonly AsyncPolicy mockCircuitBreakerPolicy;
        private readonly Fixture fixture;
        private readonly Mock<IVehicleService> mockVehicleService;
        private readonly Mock<IVehicleRepository> mockRepository;

        public AddTruckShould()
        {
            // Mock dependencies
            mockLogger = new Mock<ILogger<VehicleRepository>>();
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
                mockLogger.Object,
                mockPolicyProvider.Object
            );

            fixture = new Fixture();
        }

        [Fact]
        public async Task AddVehicleAsyncShouldAddVehicleSuccessfully()
        {
            // Arrange
            var vehicle = fixture.Create<Vehicle>();
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await repository.AddVehicleAsync(vehicle, cancellationToken);

            // Assert
            result.HasError.Should().BeFalse();  // Ensure no error occurred
            var addedVehicle = await context.Vehicles.FindAsync(vehicle.Id);
            addedVehicle.Should().NotBeNull();  // Ensure the vehicle was added
            addedVehicle.Should().BeEquivalentTo(vehicle);  // Ensure the vehicle data matches
        }

        [Fact]
        public async Task AddVehicleAsyncShouldReturnDuplicatedVehicleErrorForDuplicateId()
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
            result.Error.Should().Be(AddVehicleErrorCode.DuplicatedVehicle);  // Ensure the error code is DuplicatedVehicle
        }

        [Fact]
        public async Task AddVehicleAsyncShouldReturnInternalErrorOnException()
        {
            // Arrange
            var vehicle = fixture.Create<Vehicle>();
            var cancellationToken = CancellationToken.None;

            // Mock IPolicyProvider - No policies so it will throw reference not set
            var mockPolicyProvider = new Mock<IPolicyProvider>();
            var repository = new VehicleRepository(context, mockLogger.Object, mockPolicyProvider.Object);

            // Act
            var result = await repository.AddVehicleAsync(vehicle, cancellationToken);

            // Assert
            result.HasError.Should().BeTrue();  // Ensure an error occurred
            result.Error.Should().Be(AddVehicleErrorCode.InternalError);  // Ensure the error code is InternalError
        }

        [Fact]
        public async Task HandleShouldReturnSuccessWhenVehicleYearIsValid()
        {
            // Arrange
            var command = fixture.Create<AddTruckCommand>();
            var cancellationToken = CancellationToken.None;

            mockVehicleService.Setup(x => x.IsVehicleYearValid(command.Year)).Returns(true);

            mockRepository.Setup(repo => repo.AddVehicleAsync(It.IsAny<Vehicle>(), cancellationToken))
                .ReturnsAsync(VoidOrError<AddVehicleErrorCode>.Success());

            var handler = new AddTruckHandler(mockRepository.Object, mockVehicleService.Object);

            // Act
            var result = await handler.Handle(command, cancellationToken);

            // Assert
            result.HasError.Should().BeFalse();
        }

        [Fact]
        public async Task HandleShouldReturnInvalidYearErrorWhenVehicleYearIsInvalid()
        {
            // Arrange
            var command = fixture.Create<AddTruckCommand>();
            var cancellationToken = CancellationToken.None;

            mockVehicleService.Setup(x => x.IsVehicleYearValid(command.Year)).Returns(false);

            var handler = new AddTruckHandler(mockRepository.Object, mockVehicleService.Object);

            // Act
            var result = await handler.Handle(command, cancellationToken);

            // Assert
            result.HasError.Should().BeTrue();
            result.Error.Should().Be(AddVehicleErrorCode.InvalidYear);
        }

        [Fact]
        public async Task Handle_ShouldReturnDuplicatedVehicleError_WhenVehicleAlreadyExists()
        {
            // Arrange
            var command = fixture.Create<AddTruckCommand>();
            var cancellationToken = CancellationToken.None;

            // Mock the IVehicleService to return true for a valid year
            mockVehicleService.Setup(x => x.IsVehicleYearValid(command.Year)).Returns(true);

            mockRepository.Setup(repo => repo.AddVehicleAsync(It.IsAny<Vehicle>(), cancellationToken))
                .ReturnsAsync(VoidOrError<AddVehicleErrorCode>.Failure(AddVehicleErrorCode.DuplicatedVehicle));

            var handler = new AddTruckHandler(mockRepository.Object, mockVehicleService.Object);

            // Act
            var result = await handler.Handle(command, cancellationToken);

            // Assert
            result.HasError.Should().BeTrue();
            result.Error.Should().Be(AddVehicleErrorCode.DuplicatedVehicle);
        }
    }
}
