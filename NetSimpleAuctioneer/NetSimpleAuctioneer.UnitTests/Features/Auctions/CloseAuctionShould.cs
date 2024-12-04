using AutoFixture;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Application.Policies;
using NetSimpleAuctioneer.API.Database;
using NetSimpleAuctioneer.API.Features.Auctions.CloseAuction;
using Polly;

namespace NetSimpleAuctioneer.UnitTests.Features.Auctions
{
    public class CloseAuctionShould
    {
        private readonly Mock<ILogger<CloseAuctionRepository>> mockLoggerRepository;
        private readonly Mock<ILogger<CloseAuctionService>> mockLoggerService;
        private readonly Mock<ICloseAuctionRepository> mockRepository;
        private readonly Mock<ICloseAuctionService> mockService;
        private readonly Mock<IDatabaseConnection> mockDbConnection;
        private readonly Mock<IPolicyProvider> mockPolicyProvider;
        private readonly Mock<IOptions<ConnectionStrings>> mockConnectionStrings;
        private readonly CloseAuctionRepository repository;
        private readonly AuctioneerDbContext context;
        private readonly AsyncPolicy mockRetryPolicy;
        private readonly AsyncPolicy mockCircuitBreakerPolicy;

        private readonly CloseAuctionHandler handler;
        private readonly Fixture fixture;

        public CloseAuctionShould()
        {
            mockLoggerRepository = new Mock<ILogger<CloseAuctionRepository>>();
            mockLoggerService = new Mock<ILogger<CloseAuctionService>>();
            mockPolicyProvider = new Mock<IPolicyProvider>();
            mockRepository = new Mock<ICloseAuctionRepository>();
            mockService = new Mock<ICloseAuctionService>();
            handler = new CloseAuctionHandler(mockService.Object);

            // Set up an in-memory database for testing
            var options = new DbContextOptionsBuilder<AuctioneerDbContext>()
                            .UseInMemoryDatabase(databaseName: "TestDatabase")
                            .Options;

            // Use the in-memory DbContext
            context = new AuctioneerDbContext(options);

            // Mock the database connection
            mockDbConnection = new Mock<IDatabaseConnection>();

            // Mock the IOptions<ConnectionStrings>
            mockConnectionStrings = new Mock<IOptions<ConnectionStrings>>();
            mockConnectionStrings.Setup(x => x.Value).Returns(new ConnectionStrings
            {
                AuctioneerDBConnectionString = "Host=localhost;Port=5432;Database=AuctioneerDB;Username=postgres;Password=postgres"
            });

            // Directly create and mock concrete AsyncPolicy (e.g., RetryPolicy)
            mockRetryPolicy = Policy.NoOpAsync();  // This is a simple NoOp policy
            mockCircuitBreakerPolicy = Policy.NoOpAsync();

            // Mock IPolicyProvider
            mockPolicyProvider = new Mock<IPolicyProvider>();

            // Set up the mock to return concrete policies
            mockPolicyProvider.Setup(x => x.GetRetryPolicyWithoutConcurrencyException()).Returns(mockRetryPolicy);
            mockPolicyProvider.Setup(x => x.GetCircuitBreakerPolicy()).Returns(mockCircuitBreakerPolicy);

            repository = new CloseAuctionRepository(
                context,
                mockLoggerRepository.Object,
                mockConnectionStrings.Object,
                mockDbConnection.Object
            );

            fixture = new();
        }

        [Fact]
        public async Task CloseAuctionAsyncShouldCloseAuctionSuccessfully()
        {
            // Arrange
            var auctionId = fixture.Create<Guid>();
            var auction = new Auction
            {
                Id = auctionId,
                VehicleId = fixture.Create<Guid>(),
                StartDate = DateTime.UtcNow.AddHours(-1),
            };

            // Add auction to the in-memory DbContext
            context.Auctions.Add(auction);
            await context.SaveChangesAsync();

            var cancellationToken = CancellationToken.None;

            // Act
            var result = await repository.CloseAuctionAsync(auctionId, cancellationToken);

            // Assert
            result.HasError.Should().BeFalse();
            result.Result.AuctionId.Should().Be(auctionId);

            var closedAuction = await context.Auctions.SingleAsync(a => a.Id == auctionId);
            closedAuction.EndDate.Should().NotBeNull(); // Ensure the auction was closed
        }

        [Fact]
        public async Task CloseAuctionAsyncShouldReturnErrorWhenAuctionNotFound()
        {
            // Arrange
            var auctionId = fixture.Create<Guid>();
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await repository.CloseAuctionAsync(auctionId, cancellationToken);

            // Assert
            result.HasError.Should().BeTrue();
            result.Error.Should().Be(CloseAuctionErrorCode.InvalidAuction);
        }

        [Fact]
        public async Task CloseAuctionAsyncShouldReturnErrorWhenAuctionAlreadyClosed()
        {
            // Arrange
            var auctionId = fixture.Create<Guid>();
            var auction = new Auction
            {
                Id = auctionId,
                VehicleId = fixture.Create<Guid>(),
                StartDate = DateTime.UtcNow.AddHours(-1),
                // Auction already closed
                EndDate = DateTime.UtcNow.AddMinutes(-30)
            };

            context.Auctions.Add(auction);
            await context.SaveChangesAsync();

            var cancellationToken = CancellationToken.None;

            // Act
            var result = await repository.CloseAuctionAsync(auctionId, cancellationToken);

            // Assert
            result.HasError.Should().BeTrue();
            result.Error.Should().Be(CloseAuctionErrorCode.AuctionAlreadyClosed);
        }

        [Fact]
        public async Task CloseAuctionAsyncShouldReturnErrorOnDatabaseUpdateException()
        {
            // Arrange
            // Mock IPolicyProvider - No policies so it will throw reference not set
            var mockPolicyProvider = new Mock<IPolicyProvider>();

            var service = new CloseAuctionService(mockRepository.Object, mockPolicyProvider.Object, mockLoggerService.Object);

            // Act
            var result = await service.CloseAuctionAsync(fixture.Create<Guid>(), It.IsAny<CancellationToken>());

            // Assert
            result.HasError.Should().BeTrue();
            result.Error.Should().Be(CloseAuctionErrorCode.InternalError);
        }

        [Fact]
        public async Task HandleShouldCloseAuctionSuccessfully()
        {
            // Arrange
            var auctionId = fixture.Create<Guid>();
            var command = new CloseAuctionCommand(auctionId);
            var cancellationToken = CancellationToken.None;

            mockService.Setup(repo => repo.ValidateAuctionAsync(auctionId, cancellationToken))
                        .ReturnsAsync((CloseAuctionErrorCode?)null);

            // Mock the service to return success
            mockService.Setup(repo => repo.CloseAuctionAsync(auctionId, cancellationToken))
                          .ReturnsAsync(SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>.Success(new CloseAuctionCommandResult(auctionId)));

            // Act
            var result = await handler.Handle(command, cancellationToken);

            // Assert
            result.HasError.Should().BeFalse();
            result.Result.Should().NotBeNull();
            result.Result.AuctionId.Should().Be(auctionId);
        }

        [Fact]
        public async Task HandleShouldReturnErrorWhenAuctionNotFound()
        {
            // Arrange
            var auctionId = fixture.Create<Guid>();
            var command = new CloseAuctionCommand(auctionId);
            var cancellationToken = CancellationToken.None;

            mockService.Setup(repo => repo.ValidateAuctionAsync(auctionId, cancellationToken))
                          .ReturnsAsync((CloseAuctionErrorCode?)null);

            // Mock the service to return failure when the auction is not found
            mockService.Setup(repo => repo.CloseAuctionAsync(auctionId, cancellationToken))
                          .ReturnsAsync(SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>.Failure(CloseAuctionErrorCode.InvalidAuction));

            // Act
            var result = await handler.Handle(command, cancellationToken);

            // Assert
            result.HasError.Should().BeTrue();
            result.Error.Should().Be(CloseAuctionErrorCode.InvalidAuction);
        }

        [Fact]
        public async Task HandleShouldReturnErrorWhenAuctionAlreadyClosed()
        {
            // Arrange
            var auctionId = fixture.Create<Guid>();
            var command = new CloseAuctionCommand(auctionId);
            var cancellationToken = CancellationToken.None;

            var service = new CloseAuctionService(mockRepository.Object, mockPolicyProvider.Object, mockLoggerService.Object);

            mockService.Setup(serv => serv.ValidateAuctionAsync(auctionId, cancellationToken))
                          .ReturnsAsync((CloseAuctionErrorCode?)null);

            // Mock the service to return failure when the auction is not found
            mockService.Setup(serv => serv.CloseAuctionAsync(auctionId, cancellationToken))
                          .ReturnsAsync(SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>.Failure(CloseAuctionErrorCode.AuctionAlreadyClosed));

            // Act
            var result = await handler.Handle(command, cancellationToken);

            // Assert
            result.HasError.Should().BeTrue();
            result.Error.Should().Be(CloseAuctionErrorCode.AuctionAlreadyClosed);
        }

        [Fact]
        public async Task HandleShouldReturnErrorWhenClosingAuctionFails()
        {
            // Arrange
            var auctionId = fixture.Create<Guid>();
            var command = new CloseAuctionCommand(auctionId);
            var cancellationToken = CancellationToken.None;

            // Mock IPolicyProvider - No policies so it will throw reference not set
            var mockPolicyProvider = new Mock<IPolicyProvider>();

            mockRepository.Setup(repo => repo.GetAuctionByIdAsync(auctionId, cancellationToken))
                          .ReturnsAsync((Guid.NewGuid(), (DateTime?)null));

            var service = new CloseAuctionService(mockRepository.Object, mockPolicyProvider.Object, mockLoggerService.Object);

            var handler = new CloseAuctionHandler(service);

            // Act
            var result = await handler.Handle(command, cancellationToken);

            // Assert
            result.HasError.Should().BeTrue();
            result.Error.Should().Be(CloseAuctionErrorCode.InternalError);
        }
    }
}