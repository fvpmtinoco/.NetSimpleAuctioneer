using AutoFixture;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private readonly Mock<ILogger<CloseAuctionRepository>> mockLogger;
        private readonly Mock<IPolicyProvider> mockPolicyProvider;
        private readonly CloseAuctionRepository repository;
        private readonly AuctioneerDbContext context;
        private readonly AsyncPolicy mockRetryPolicy;
        private readonly AsyncPolicy mockCircuitBreakerPolicy;
        private readonly Mock<ICloseAuctionRepository> mockRepository;
        private readonly CloseAuctionHandler handler;
        private readonly Fixture fixture;

        public CloseAuctionShould()
        {
            mockLogger = new Mock<ILogger<CloseAuctionRepository>>();
            mockPolicyProvider = new Mock<IPolicyProvider>();
            mockRepository = new Mock<ICloseAuctionRepository>();
            handler = new CloseAuctionHandler(mockRepository.Object);

            // Set up an in-memory database for testing
            var options = new DbContextOptionsBuilder<AuctioneerDbContext>()
                            .UseInMemoryDatabase(databaseName: "TestDatabase")
                            .Options;

            handler = new CloseAuctionHandler(mockRepository.Object);

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

            repository = new CloseAuctionRepository(
                context,
                mockLogger.Object,
                mockPolicyProvider.Object
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

            var repository = new CloseAuctionRepository(context, mockLogger.Object, mockPolicyProvider.Object);

            // Act
            var result = await repository.CloseAuctionAsync(fixture.Create<Guid>(), It.IsAny<CancellationToken>());

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

            // Mock the repository to return success
            mockRepository.Setup(repo => repo.CloseAuctionAsync(auctionId, cancellationToken))
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

            // Mock the repository to return failure when the auction is not found
            mockRepository.Setup(repo => repo.CloseAuctionAsync(auctionId, cancellationToken))
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

            // Mock the repository to return failure when the auction is already closed
            mockRepository.Setup(repo => repo.CloseAuctionAsync(auctionId, cancellationToken))
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

            // Mock the repository to simulate an error while closing the auction
            mockRepository.Setup(repo => repo.CloseAuctionAsync(auctionId, cancellationToken))
                          .ReturnsAsync(SuccessOrError<CloseAuctionCommandResult, CloseAuctionErrorCode>.Failure(CloseAuctionErrorCode.InternalError));

            // Act
            var result = await handler.Handle(command, cancellationToken);

            // Assert
            result.HasError.Should().BeTrue();
            result.Error.Should().Be(CloseAuctionErrorCode.InternalError);
        }
    }
}