using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Application.Policies;
using NetSimpleAuctioneer.API.Database;
using NetSimpleAuctioneer.API.Features.Auctions.PlaceBid;
using Polly;

namespace NetSimpleAuctioneer.UnitTests.Features.Auctions
{
    public class PlaceBidHandlerTests
    {
        private readonly Mock<IPlaceBidService> _placeBidServiceMock;
        private readonly PlaceBidHandler _handler;

        public PlaceBidHandlerTests()
        {
            _placeBidServiceMock = new Mock<IPlaceBidService>();
            _handler = new PlaceBidHandler(_placeBidServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenAuctionIsInvalid()
        {
            // Arrange
            var command = new PlaceBidCommand(Guid.NewGuid(), "bidder@example.com", 100.0m);
            _placeBidServiceMock
                .Setup(s => s.ValidateAuctionAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(PlaceBidErrorCode.InvalidAuction);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.HasError);
            Assert.Equal(PlaceBidErrorCode.InvalidAuction, result.Error);
        }

        [Fact]
        public async Task Handle_ShouldPlaceBidSuccessfully_WhenAuctionIsValid()
        {
            // Arrange
            var command = new PlaceBidCommand(Guid.NewGuid(), "bidder@example.com", 100.0m);
            _placeBidServiceMock
                .Setup(s => s.ValidateAuctionAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync((PlaceBidErrorCode?)null); // No validation error

            var placeBidResult = SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>.Success(new PlaceBidCommandResult(Guid.NewGuid()));

            _placeBidServiceMock
                .Setup(s => s.PlaceBidAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(placeBidResult);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.HasError);
            Assert.NotNull(result.Result);
        }
    }

    public class PlaceBidServiceTests
    {
        private readonly Mock<IPlaceBidRepository> repositoryMock;
        private readonly Mock<IPolicyProvider> policyProviderMock;
        private readonly Mock<ILogger<PlaceBidService>> loggerMock;
        private readonly PlaceBidService service;
        private readonly AsyncPolicy mockRetryPolicy;
        private readonly AsyncPolicy mockCircuitBreakerPolicy;

        public PlaceBidServiceTests()
        {
            repositoryMock = new Mock<IPlaceBidRepository>();
            policyProviderMock = new Mock<IPolicyProvider>();
            loggerMock = new Mock<ILogger<PlaceBidService>>();

            // Directly create and mock concrete AsyncPolicy (e.g., RetryPolicy)
            mockRetryPolicy = Policy.NoOpAsync();  // This is a simple NoOp policy
            mockCircuitBreakerPolicy = Policy.NoOpAsync();

            // Set up the mock to return concrete policies
            policyProviderMock.Setup(x => x.GetRetryPolicy()).Returns(mockRetryPolicy);
            policyProviderMock.Setup(x => x.GetCircuitBreakerPolicy()).Returns(mockCircuitBreakerPolicy);

            service = new PlaceBidService(repositoryMock.Object, policyProviderMock.Object, loggerMock.Object);
        }

        [Fact]
        public async Task PlaceBidShouldReturnSuccessWhenAuctionIsOpenAndBidIsPlacedSuccessfully()
        {
            // Arrange
            var command = new PlaceBidCommand(Guid.NewGuid(), "bidder@example.com", 100.0m);
            var bid = new Bid
            {
                AuctionId = command.AuctionId,
                BidderEmail = command.BidderEmail,
                BidAmount = command.BidAmount,
                Timestamp = DateTime.UtcNow
            };

            // Mock repository to return a valid auction and place the bid successfully
            repositoryMock
                .Setup(r => r.PlaceBidAsync(It.IsAny<Bid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>.Success(new PlaceBidCommandResult(Guid.NewGuid())));

            repositoryMock
                .Setup(r => r.GetAuctionByIdAsync(command.AuctionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid.NewGuid(), (DateTime?)null, 50.0m)); // Simulate an open auction

            // Act
            var result = await service.PlaceBidAsync(command, CancellationToken.None);

            // Assert
            result.HasError.Should().BeFalse();
            result.Result.Should().NotBeNull();
            result.Result.BidId.Should().NotBeEmpty();
        }

        [Fact]
        public async Task ValidateAuctionShouldReturnFailureWhenAuctionIsClosed()
        {
            // Arrange
            var command = new PlaceBidCommand(Guid.NewGuid(), "bidder@example.com", 100.0m);

            // Mock repository to return a closed auction
            repositoryMock
                .Setup(r => r.GetAuctionByIdAsync(command.AuctionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid.NewGuid(), DateTime.UtcNow.AddDays(-1), 50.0m)); // Simulate a closed auction

            // Act
            var result = await service.ValidateAuctionAsync(command, CancellationToken.None);

            // Assert
            result.Should().Be(PlaceBidErrorCode.AuctionAlreadyClosed);
        }


        [Fact]
        public async Task ValidateAuctionShouldReturnFailureWhenBidAmountIsTooLow()
        {
            // Arrange
            var command = new PlaceBidCommand(Guid.NewGuid(), "bidder@example.com", 25.0m); // Too low bid

            repositoryMock
                .Setup(r => r.GetAuctionByIdAsync(command.AuctionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid.NewGuid(), (DateTime?)null, 50.0m)); // Minimum bid amount is 50

            // Act
            var result = await service.ValidateAuctionAsync(command, CancellationToken.None);

            // Assert
            result.Should().Be(PlaceBidErrorCode.BidAmountTooLow);
        }

        [Fact]
        public async Task ValidateAuctionShouldReturnFailureWhenHigherBidExists()
        {
            // Arrange
            var command = new PlaceBidCommand(Guid.NewGuid(), "bidder@example.com", 25.0m);

            // Mock repository to return a closed auction
            repositoryMock
                .Setup(r => r.GetAuctionByIdAsync(command.AuctionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid.NewGuid(), (DateTime?)null, 5m)); // Simulate a closed auction

            repositoryMock
                .Setup(r => r.GetHighestBidForAuctionAsync(command.AuctionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((100m, It.IsAny<string>())); // Minimum bid amount is 50

            // Act
            var result = await service.ValidateAuctionAsync(command, CancellationToken.None);

            // Assert
            result.Should().Be(PlaceBidErrorCode.ExistingHigherBid);
        }

        [Fact]
        public async Task ValidateAuctionShouldReturnFailureWhenBidderHasAlreadyTheHigherBid()
        {
            // Arrange
            var command = new PlaceBidCommand(Guid.NewGuid(), "bidder@example.com", 25.0m);

            // Mock repository to return a closed auction
            repositoryMock
                .Setup(r => r.GetAuctionByIdAsync(command.AuctionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid.NewGuid(), (DateTime?)null, 5m)); // Simulate a closed auction

            repositoryMock
                .Setup(r => r.GetHighestBidForAuctionAsync(command.AuctionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((10, "bidder@example.com")); // Minimum bid amount is 50

            // Act
            var result = await service.ValidateAuctionAsync(command, CancellationToken.None);

            // Assert
            result.Should().Be(PlaceBidErrorCode.BidderHasHigherBid);
        }
    }
}