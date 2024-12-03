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
using Npgsql;
using Polly;

namespace NetSimpleAuctioneer.UnitTests.Features.Auctions
{
    public class StartAuctionShould
    {
        private readonly Mock<ILogger<StartAuctionRepository>> mockLogger;
        private readonly Mock<IPolicyProvider> mockPolicyProvider;
        private readonly StartAuctionRepository repository;
        private readonly AuctioneerDbContext context;
        private readonly AsyncPolicy mockRetryPolicy;
        private readonly AsyncPolicy mockCircuitBreakerPolicy;
        private readonly Mock<IStartAuctionRepository> mockRepository;
        private readonly StartAuctionHandler handler;
        private readonly Fixture fixture;
        private readonly Mock<IOptions<ConnectionStrings>> mockConnectionStrings;
        private readonly Mock<IDatabaseConnection> mockDbConnection;

        public StartAuctionShould()
        {
            mockLogger = new Mock<ILogger<StartAuctionRepository>>();
            mockPolicyProvider = new Mock<IPolicyProvider>();
            mockRepository = new Mock<IStartAuctionRepository>();
            mockDbConnection = new Mock<IDatabaseConnection>();

            handler = new StartAuctionHandler(mockRepository.Object, It.IsAny<IOptions<ConnectionStrings>>());
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

            handler = new StartAuctionHandler(mockRepository.Object, mockConnectionStrings.Object);

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
                mockLogger.Object,
                mockPolicyProvider.Object,
                mockDbConnection.Object
            );

            fixture = new();
        }

        [Fact]
        public async Task StartAuctionAsyncShouldCreateAuctionForVehicle()
        {
            // Arrange
            var vehicleId = fixture.Create<Guid>();
            var cancellationToken = CancellationToken.None;
            var commandResult = SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>.Success(new StartAuctionCommandResult(fixture.Create<Guid>()));

            // Mock the policy provider and logger
            mockRepository.Setup(repo => repo.StartAuctionAsync(vehicleId, cancellationToken))
                .ReturnsAsync(commandResult);

            // Act
            var result = await repository.StartAuctionAsync(vehicleId, cancellationToken);

            // Assert
            result.HasError.Should().BeFalse();
            result.Result.Should().NotBeNull();
            result.Result.AuctionId.Should().NotBeEmpty();
        }

        [Fact]
        public async Task StartAuctionShouldReturnErrorWhenAuctionAlreadyActive()
        {
            // Arrange
            var vehicleId = fixture.Create<Guid>();
            var cancellationToken = CancellationToken.None;

            // Add auction and initial bid to the in-memory context
            var auction = new Auction
            {
                Id = fixture.Create<Guid>(),
                VehicleId = vehicleId,
                StartDate = DateTime.UtcNow,
            };

            context.Auctions.Add(auction);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.StartAuctionAsync(vehicleId, cancellationToken);

            // Assert
            result.HasError.Should().BeTrue();
            result.Error.Should().Be(StartAuctionErrorCode.AuctionForVehicleAlreadyActive);
        }

        [Fact]
        public async Task StartAuctionAsyncShouldReturnInternalErrorOnException()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;

            // Mock IPolicyProvider - No policies so it will throw reference not set
            var mockPolicyProvider = new Mock<IPolicyProvider>();

            var repository = new StartAuctionRepository(context, mockLogger.Object, mockPolicyProvider.Object, mockDbConnection.Object);

            // Act
            var result = await repository.StartAuctionAsync(fixture.Create<Guid>(), cancellationToken);

            // Assert
            result.HasError.Should().BeTrue();
            result.Error.Should().Be(StartAuctionErrorCode.InternalError);
        }

        [Fact]
        public async Task VehicleExistsAsyncShouldReturnInternalErrorOnException()
        {
            // Arrange
            var vehicleId = fixture.Create<Guid>();
            var cancellationToken = CancellationToken.None;

            // Mock IPolicyProvider - No policies so it will throw reference not set
            var mockPolicyProvider = new Mock<IPolicyProvider>();

            var repository = new StartAuctionRepository(context, mockLogger.Object, mockPolicyProvider.Object, mockDbConnection.Object);

            // Act
            var result = await repository.VehicleExistsAsync(It.IsAny<NpgsqlConnection>(), vehicleId, cancellationToken);

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

            // Mock VehicleExistsAsync to return false (vehicle does not exist)
            mockRepository.Setup(repo => repo.VehicleExistsAsync(It.IsAny<NpgsqlConnection>(), vehicleId, cancellationToken))
                .ReturnsAsync(SuccessOrError<bool, StartAuctionErrorCode>.Success(false));

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

            // Mock VehicleExistsAsync to return true (vehicle exists)
            mockRepository.Setup(repo => repo.VehicleExistsAsync(It.IsAny<NpgsqlConnection>(), vehicleId, cancellationToken))
                .ReturnsAsync(SuccessOrError<bool, StartAuctionErrorCode>.Success(true));

            // Mock StartAuctionAsync to throw AuctionAlreadyActiveException
            mockRepository.Setup(repo => repo.StartAuctionAsync(It.IsAny<Guid>(), cancellationToken))
                .ReturnsAsync(SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>.Failure(StartAuctionErrorCode.AuctionForVehicleAlreadyActive));

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

            // Mock VehicleExistsAsync to return true (vehicle exists)
            mockRepository.Setup(repo => repo.VehicleExistsAsync(It.IsAny<NpgsqlConnection>(), vehicleId, cancellationToken))
                .ReturnsAsync(SuccessOrError<bool, StartAuctionErrorCode>.Success(true));

            // Mock StartAuctionAsync to return success
            mockRepository.Setup(repo => repo.StartAuctionAsync(It.IsAny<Guid>(), cancellationToken))
                .ReturnsAsync(SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>.Success(new StartAuctionCommandResult(expectedAuctionId)));

            // Act
            var result = await handler.Handle(command, cancellationToken);

            // Assert
            result.HasError.Should().BeFalse();
            result.Result.AuctionId.Should().Be(expectedAuctionId);
        }
    }
}