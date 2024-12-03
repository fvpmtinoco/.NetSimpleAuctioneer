using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NetSimpleAuctioneer.API.Application.Policies;
using NetSimpleAuctioneer.API.Database;
using NetSimpleAuctioneer.API.Features.Auctions.PlaceBid;
using Polly;

namespace NetSimpleAuctioneer.UnitTests
{
    public class PlaceBidRepositoryTests
    {
        private readonly Mock<ILogger<PlaceBidRepository>> _mockLogger;
        private readonly Mock<IPolicyProvider> _mockPolicyProvider;
        private readonly PlaceBidRepository _repository;
        private readonly AuctioneerDbContext _context;
        private readonly AsyncPolicy _mockRetryPolicy;
        private readonly AsyncPolicy _mockCircuitBreakerPolicy;

        public PlaceBidRepositoryTests()
        {
            _mockLogger = new Mock<ILogger<PlaceBidRepository>>();
            _mockPolicyProvider = new Mock<IPolicyProvider>();

            // Set up an in-memory database for testing
            var options = new DbContextOptionsBuilder<AuctioneerDbContext>()
                            .UseInMemoryDatabase(databaseName: "TestDatabase")
                            .Options;

            _context = new AuctioneerDbContext(options);  // Use the in-memory DbContext

            // Directly create and mock concrete AsyncPolicy (e.g., RetryPolicy)
            _mockRetryPolicy = Policy.NoOpAsync();  // This is a simple NoOp policy, you can use others like RetryPolicy.
            _mockCircuitBreakerPolicy = Policy.NoOpAsync();  // Similarly for CircuitBreaker or any policy.

            // Mock IPolicyProvider
            _mockPolicyProvider = new Mock<IPolicyProvider>();

            // Set up the mock to return concrete policies
            _mockPolicyProvider.Setup(x => x.GetRetryPolicy()).Returns(_mockRetryPolicy);
            _mockPolicyProvider.Setup(x => x.GetCircuitBreakerPolicy()).Returns(_mockCircuitBreakerPolicy);

            _repository = new PlaceBidRepository(
                _context,
                _mockLogger.Object,
                _mockPolicyProvider.Object
            );
        }

        [Fact]
        public async Task PlaceBidAsync_ShouldReturnSuccess_WhenBidIsHigherThanCurrentBid()
        {
            // Arrange: Setup an auction and an initial bid
            var auctionId = Guid.NewGuid();
            var bidderEmail = "test@bidder.com";
            var bidAmount = 100m;

            // Add auction and initial bid to the in-memory context
            var auction = new Auction
            {
                Id = auctionId,
                VehicleId = Guid.NewGuid(),
                StartDate = DateTime.UtcNow,
                EndDate = null // Auction is open
            };

            _context.Auctions.Add(auction);
            await _context.SaveChangesAsync();

            var bid = new Bid
            {
                AuctionId = auctionId,
                BidderEmail = "existing@bidder.com",
                BidAmount = 50m,
                Timestamp = DateTime.UtcNow
            };

            _context.Bids.Add(bid);
            await _context.SaveChangesAsync();

            // Act: Place a higher bid
            var result = await _repository.PlaceBidAsync(auctionId, bidderEmail, bidAmount, CancellationToken.None);

            // Assert: Check if the result is success
            Assert.False(result.HasError);
        }

        [Fact]
        public async Task PlaceBidAsync_ShouldReturnFailure_WhenAuctionNotFound()
        {
            // Arrange: Use an invalid auction ID
            var auctionId = Guid.NewGuid();  // Non-existing auction
            var bidderEmail = "test@bidder.com";
            var bidAmount = 100m;

            // Act: Try placing a bid on a non-existing auction
            var result = await _repository.PlaceBidAsync(auctionId, bidderEmail, bidAmount, CancellationToken.None);

            // Assert: The result should indicate failure for auction not found
            Assert.True(result.HasError);
            Assert.Equal(PlaceBidErrorCode.AuctionNotFound, result.Error);
        }

        [Fact]
        public async Task PlaceBidAsync_ShouldReturnFailure_WhenAuctionClosed()
        {
            // Arrange: Create a closed auction
            var auctionId = Guid.NewGuid();
            var bidderEmail = "test@bidder.com";
            var bidAmount = 100m;

            var auction = new Auction
            {
                Id = auctionId,
                VehicleId = Guid.NewGuid(),
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow // Auction is closed
            };

            _context.Auctions.Add(auction);
            await _context.SaveChangesAsync();

            // Act: Try placing a bid on a closed auction
            var result = await _repository.PlaceBidAsync(auctionId, bidderEmail, bidAmount, CancellationToken.None);

            // Assert: The result should indicate failure for auction already closed
            Assert.True(result.HasError);
            Assert.Equal(PlaceBidErrorCode.AuctionAlreadyClosed, result.Error);
        }

        [Fact]
        public async Task PlaceBidAsync_ShouldReturnFailure_WhenBidAmountIsTooLow()
        {
            // Arrange: Setup auction and place a higher bid
            var auctionId = Guid.NewGuid();
            var bidderEmail = "test@bidder.com";
            var bidAmount = 100m;

            // Add auction to the in-memory context
            var auction = new Auction
            {
                Id = auctionId,
                VehicleId = Guid.NewGuid(),
                StartDate = DateTime.UtcNow,
                EndDate = null // Auction is open
            };

            _context.Auctions.Add(auction);
            await _context.SaveChangesAsync();

            var bid = new Bid
            {
                AuctionId = auctionId,
                BidderEmail = "existing@bidder.com",
                BidAmount = 200m,
                Timestamp = DateTime.UtcNow
            };

            _context.Bids.Add(bid);
            await _context.SaveChangesAsync();

            // Act: Try placing a lower bid than the highest bid
            var result = await _repository.PlaceBidAsync(auctionId, bidderEmail, bidAmount, CancellationToken.None);

            // Assert: The result should indicate failure for bid amount being too low
            Assert.True(result.HasError);
            Assert.Equal(PlaceBidErrorCode.BidAmountTooLow, result.Error);
        }
    }
}