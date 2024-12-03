using AutoFixture;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Application.Policies;
using NetSimpleAuctioneer.API.Database;
using NetSimpleAuctioneer.API.Features.Auctions.PlaceBid;
using Polly;

namespace NetSimpleAuctioneer.UnitTests.Features.Auctions
{
    public class PlaceBidShould
    {
        private readonly Mock<ILogger<PlaceBidRepository>> mockLogger;
        private readonly Mock<IPolicyProvider> mockPolicyProvider;
        private readonly PlaceBidRepository repository;
        private readonly AuctioneerDbContext context;
        private readonly AsyncPolicy mockRetryPolicy;
        private readonly AsyncPolicy mockCircuitBreakerPolicy;
        private readonly Mock<IPlaceBidRepository> mockRepository;
        private readonly PlaceBidHandler handler;
        private readonly Fixture fixture;

        public PlaceBidShould()
        {
            mockLogger = new Mock<ILogger<PlaceBidRepository>>();
            mockPolicyProvider = new Mock<IPolicyProvider>();
            mockRepository = new Mock<IPlaceBidRepository>();
            handler = new PlaceBidHandler(mockRepository.Object);

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
            mockPolicyProvider.Setup(x => x.GetRetryPolicy()).Returns(mockRetryPolicy);
            mockPolicyProvider.Setup(x => x.GetCircuitBreakerPolicy()).Returns(mockCircuitBreakerPolicy);

            repository = new PlaceBidRepository(
                context,
                mockLogger.Object,
                mockPolicyProvider.Object
            );

            fixture = new();
        }

        [Fact]
        public async Task PlaceBidShouldReturnSuccessWhenBidIsHigherThanCurrentBid()
        {
            // Arrange: Setup an auction and an initial bid
            var auctionId = fixture.Create<Guid>();
            var bidderEmail = "test@bidder.com";
            var bidAmount = 100m;

            // Add auction and initial bid to the in-memory context
            var auction = new Auction
            {
                Id = auctionId,
                VehicleId = fixture.Create<Guid>(),
                StartDate = DateTime.UtcNow,
            };

            context.Auctions.Add(auction);
            await context.SaveChangesAsync();

            var bid = new Bid
            {
                AuctionId = auctionId,
                BidderEmail = "existing@bidder.com",
                BidAmount = 50m,
                Timestamp = DateTime.UtcNow
            };

            context.Bids.Add(bid);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.PlaceBidAsync(auctionId, bidderEmail, bidAmount, CancellationToken.None);

            // Assert
            result.HasError.Should().BeFalse();

        }

        [Fact]
        public async Task PlaceBidShouldReturnFailureWhenAuctionNotFound()
        {
            // Arrange
            var auctionId = fixture.Create<Guid>();
            var bidderEmail = "test@bidder.com";
            var bidAmount = 100m;

            // Act
            var result = await repository.PlaceBidAsync(auctionId, bidderEmail, bidAmount, CancellationToken.None);

            // Assert
            result.HasError.Should().BeTrue();
            result.Error.Should().Be(PlaceBidErrorCode.AuctionNotFound);
        }

        [Fact]
        public async Task PlaceBidShouldReturnFailureWhenAuctionClosed()
        {
            // Arrange
            var auctionId = fixture.Create<Guid>();
            var bidderEmail = "test@bidder.com";
            var bidAmount = 100m;

            var auction = new Auction
            {
                Id = auctionId,
                VehicleId = fixture.Create<Guid>(),
                StartDate = DateTime.UtcNow.AddDays(-1),
                // Auction is closed
                EndDate = DateTime.UtcNow
            };

            context.Auctions.Add(auction);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.PlaceBidAsync(auctionId, bidderEmail, bidAmount, CancellationToken.None);

            // Assert
            result.HasError.Should().BeTrue();
            result.Error.Should().Be(PlaceBidErrorCode.AuctionAlreadyClosed);
        }

        [Fact]
        public async Task PlaceBidShouldReturnFailureWhenBidAmountIsTooLow()
        {
            // Arrange
            var auctionId = fixture.Create<Guid>();
            var bidderEmail = "test@bidder.com";
            var bidAmount = 100m;

            var auction = new Auction
            {
                Id = auctionId,
                VehicleId = fixture.Create<Guid>(),
                StartDate = DateTime.UtcNow
            };

            context.Auctions.Add(auction);
            await context.SaveChangesAsync();

            var bid = new Bid
            {
                AuctionId = auctionId,
                BidderEmail = "existing@bidder.com",
                BidAmount = 200m,
                Timestamp = DateTime.UtcNow
            };

            context.Bids.Add(bid);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.PlaceBidAsync(auctionId, bidderEmail, bidAmount, CancellationToken.None);

            // Assert
            result.HasError.Should().BeTrue();
            result.Error.Should().Be(PlaceBidErrorCode.BidAmountTooLow);
        }

        [Fact]
        public async Task PlaceBidShouldReturnFailureWhenBidAmountTooLow()
        {
            // Arrange
            var auctionId = fixture.Create<Guid>();
            var bidderEmail = "test@bidder.com";
            var bidAmount = 100m;

            var auction = new Auction
            {
                Id = auctionId,
                VehicleId = fixture.Create<Guid>(),
                StartDate = DateTime.UtcNow
            };

            context.Auctions.Add(auction);
            await context.SaveChangesAsync();

            var initialBid = new Bid
            {
                AuctionId = auctionId,
                BidderEmail = "existing@bidder.com",
                BidAmount = 200m,
                Timestamp = DateTime.UtcNow
            };

            context.Bids.Add(initialBid);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.PlaceBidAsync(auctionId, bidderEmail, bidAmount, CancellationToken.None);

            // Assert
            result.HasError.Should().BeTrue();
            result.Error.Should().Be(PlaceBidErrorCode.BidAmountTooLow);
        }

        [Fact]
        public async Task PlaceBidShouldReturnFailureWhenBidderIsHighestBidder()
        {
            // Arrange
            var auctionId = fixture.Create<Guid>();
            var bidderEmail = "test@bidder.com";
            var bidAmount = 300m;

            var auction = new Auction
            {
                Id = auctionId,
                VehicleId = fixture.Create<Guid>(),
                StartDate = DateTime.UtcNow
            };

            context.Auctions.Add(auction);
            await context.SaveChangesAsync();

            var initialBid = new Bid
            {
                AuctionId = auctionId,
                // Same bidder as placing new bid
                BidderEmail = bidderEmail,
                BidAmount = 200m,
                Timestamp = DateTime.UtcNow
            };

            context.Bids.Add(initialBid);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.PlaceBidAsync(auctionId, bidderEmail, bidAmount, CancellationToken.None);

            // Assert
            result.HasError.Should().BeTrue();
            result.Error.Should().Be(PlaceBidErrorCode.BidderHasHigherBid);
        }

        [Fact]
        public async Task PlaceBidShouldReturnInternalErrorOnDbUpdateException()
        {
            // Arrange
            var auctionId = fixture.Create<Guid>();
            var bidderEmail = "test@bidder.com";
            var bidAmount = 100m;

            var logger = new Mock<ILogger<PlaceBidRepository>>().Object;
            var policyProvider = new Mock<IPolicyProvider>().Object;

            var auction = new Auction
            {
                Id = auctionId,
                VehicleId = fixture.Create<Guid>(),
                StartDate = DateTime.UtcNow
            };

            context.Auctions.Add(auction);
            await context.SaveChangesAsync();

            // Create the repository with the in-memory DbContext
            var repository = new PlaceBidRepository(context, logger, policyProvider);

            // Act: Simulate a DB update exception by passing an invalid auction ID
            var result = await repository.PlaceBidAsync(Guid.Empty, bidderEmail, bidAmount, CancellationToken.None);

            // Assert: Ensure the result indicates an internal error
            result.HasError.Should().BeTrue();
            result.Error.Should().Be(PlaceBidErrorCode.InternalError);
        }

        [Fact]
        public async Task HandleShouldReturnSuccessWhenBidIsPlacedSuccessfully()
        {
            // Arrange
            var bidId = fixture.Create<Guid>();
            var auctionId = fixture.Create<Guid>();
            var bidderEmail = "test@bidder.com";
            var bidAmount = 100m;
            var command = new PlaceBidCommand(auctionId, bidderEmail, bidAmount);

            var expectedResult = SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>.Success(new PlaceBidCommandResult(bidId));

            mockRepository.Setup(repo => repo.PlaceBidAsync(auctionId, bidderEmail, bidAmount, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Error.Should().BeNull();
            result.Result.BidId.Should().Be(expectedResult.Result.BidId);
        }

        [Fact]
        public async Task HandleShouldReturnErrorWhenBidPlacementFails()
        {
            // Arrange
            var auctionId = fixture.Create<Guid>();
            var bidderEmail = "test@bidder.com";
            var bidAmount = 100m;
            var command = new PlaceBidCommand(auctionId, bidderEmail, bidAmount);

            var expectedError = PlaceBidErrorCode.AuctionNotFound;
            var resultError = SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>.Failure(expectedError);

            mockRepository.Setup(repo => repo.PlaceBidAsync(auctionId, bidderEmail, bidAmount, It.IsAny<CancellationToken>()))
                .ReturnsAsync(resultError);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Result.Should().BeNull();
            result.Error.Should().Be(expectedError);
        }
    }
}