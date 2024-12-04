using AutoFixture;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Application.Policies;
using NetSimpleAuctioneer.API.Database;
using NetSimpleAuctioneer.API.Features.Auctions.StartAuction;
using Polly;

namespace NetSimpleAuctioneer.UnitTests.Features.Auctions
{
    public class StartAuctionShould
    {
        private readonly Mock<ILogger<StartAuctionRepository>> mockRepositoryLogger;
        private readonly Mock<ILogger<StartAuctionService>> mockServiceLogger;
        private readonly Mock<IPolicyProvider> mockPolicyProvider;
        private readonly StartAuctionRepository repository;
        private readonly AuctioneerDbContext context;
        private readonly AsyncPolicy mockRetryPolicy;
        private readonly AsyncPolicy mockCircuitBreakerPolicy;
        private readonly Mock<IStartAuctionRepository> mockRepository;
        private readonly Mock<IStartAuctionService> mockService;
        private readonly StartAuctionHandler handler;
        private readonly Fixture fixture;
        private readonly Mock<IOptions<ConnectionStrings>> mockConnectionStrings;
        private readonly Mock<IDatabaseConnection> mockDbConnection;

        public StartAuctionShould()
        {
            mockRepositoryLogger = new Mock<ILogger<StartAuctionRepository>>();
            mockServiceLogger = new Mock<ILogger<StartAuctionService>>();
            mockPolicyProvider = new Mock<IPolicyProvider>();
            mockRepository = new Mock<IStartAuctionRepository>();
            mockService = new Mock<IStartAuctionService>();

            // Mock the database connection
            mockDbConnection = new Mock<IDatabaseConnection>();

            handler = new StartAuctionHandler(mockService.Object);
            mockConnectionStrings = new Mock<IOptions<ConnectionStrings>>();

            // Set up mock connection string
            mockConnectionStrings.Setup(options => options.Value).Returns(new ConnectionStrings
            {
                AuctioneerDBConnectionString = "Host=localhost;Port=5432;Database=AuctioneerDB;Username=postgres;Password=postgres"
            });

            // Set up an in-memory database for testing
            var options = new DbContextOptionsBuilder<AuctioneerDbContext>()
                            .UseInMemoryDatabase(databaseName: "TestDatabase")
                            .Options;

            // Use the in-memory DbContext
            context = new AuctioneerDbContext(options);

            // Directly create and mock concrete AsyncPolicy (e.g., RetryPolicy)
            mockRetryPolicy = Policy.NoOpAsync();  // This is a simple NoOp policy
            mockCircuitBreakerPolicy = Policy.NoOpAsync();

            // Mock IPolicyProvider
            mockPolicyProvider = new Mock<IPolicyProvider>();

            // Set up the mock to return concrete policies
            mockPolicyProvider.Setup(x => x.GetRetryPolicyWithoutConcurrencyException()).Returns(mockRetryPolicy);
            mockPolicyProvider.Setup(x => x.GetCircuitBreakerPolicy()).Returns(mockCircuitBreakerPolicy);

            repository = new StartAuctionRepository(
                context,
                mockRepositoryLogger.Object,
                mockConnectionStrings.Object,
                mockDbConnection.Object
            );

            fixture = new();
        }

        [Fact]
        public async Task StartAuctionAsyncShouldCreateAuctionForVehicle()
        {
            // Arrange
            var vehicle = fixture.Create<Auction>();
            var cancellationToken = CancellationToken.None;
            var commandResult = SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>.Success(new StartAuctionCommandResult(fixture.Create<Guid>()));

            mockRepository.Setup(repo => repo.StartAuctionAsync(vehicle, cancellationToken))
                .ReturnsAsync(commandResult);

            // Act
            var result = await repository.StartAuctionAsync(vehicle, cancellationToken);

            // Assert
            result.HasError.Should().BeFalse();
            result.Result.Should().NotBeNull();
            result.Result.AuctionId.Should().NotBeEmpty();
        }

        [Fact]
        public async Task StartAuctionShouldReturnInternalErrorOnException()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;

            // Mock IPolicyProvider - No policies so it will throw reference not set
            var mockPolicyProvider = new Mock<IPolicyProvider>();

            var service = new StartAuctionService(mockRepository.Object, mockServiceLogger.Object, mockPolicyProvider.Object);

            // Act
            var result = await service.StartAuctionAsync(It.IsAny<Guid>(), cancellationToken);

            // Assert
            result.HasError.Should().BeTrue();
            result.Error.Should().Be(StartAuctionErrorCode.InternalError);
        }

        [Fact]
        public async Task HandleShouldReturnErrorWhenVehicleDoesNotExist()
        {
            // Arrange
            var vehicleId = fixture.Create<Guid>();
            var command = new StartAuctionCommand(vehicleId);
            var cancellationToken = CancellationToken.None;

            mockService.Setup(repo => repo.ValidateAuctionAsync(command, cancellationToken))
                .ReturnsAsync(StartAuctionErrorCode.InvalidVehicle);

            // Act
            var result = await handler.Handle(command, cancellationToken);

            // Assert
            result.HasError.Should().BeTrue();
            result.Error.Should().Be(StartAuctionErrorCode.InvalidVehicle);
        }

        [Fact]
        public async Task HandleShouldReturnErrorWhenVehicleAlreadyHasActiveAuction()
        {
            // Arrange
            var vehicleId = fixture.Create<Guid>();
            var command = new StartAuctionCommand(vehicleId);
            var cancellationToken = CancellationToken.None;

            mockService.Setup(repo => repo.ValidateAuctionAsync(command, cancellationToken))
                .ReturnsAsync(StartAuctionErrorCode.AuctionForVehicleAlreadyActive);

            // Act
            var result = await handler.Handle(command, cancellationToken);

            // Assert
            result.HasError.Should().BeTrue();
            result.Error.Should().Be(StartAuctionErrorCode.AuctionForVehicleAlreadyActive);
        }

        [Fact]
        public async Task HandleShouldReturnSuccessWhenAuctionIsStartedSuccessfully()
        {
            // Arrange
            var vehicleId = fixture.Create<Guid>();
            var command = new StartAuctionCommand(vehicleId);
            var cancellationToken = CancellationToken.None;
            var expectedAuctionId = Guid.NewGuid();

            mockService.Setup(repo => repo.ValidateAuctionAsync(command, cancellationToken))
                .ReturnsAsync((StartAuctionErrorCode?)null);

            mockService.Setup(repo => repo.StartAuctionAsync(It.IsAny<Guid>(), cancellationToken))
                .ReturnsAsync(SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>.Success(new StartAuctionCommandResult(expectedAuctionId)));

            // Act
            var result = await handler.Handle(command, cancellationToken);

            // Assert
            result.HasError.Should().BeFalse();
            result.Result.AuctionId.Should().Be(expectedAuctionId);
        }
    }
}