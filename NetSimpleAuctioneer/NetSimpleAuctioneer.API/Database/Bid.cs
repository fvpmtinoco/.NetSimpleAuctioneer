namespace NetSimpleAuctioneer.API.Database
{
    public class Bid
    {
        public Guid Id { get; set; }
        public Guid AuctionId { get; set; }
        public decimal BidAmount { get; set; }
        public string BidderEmail { get; set; } = default!;
        public DateTime Timestamp { get; set; }
    }
}
