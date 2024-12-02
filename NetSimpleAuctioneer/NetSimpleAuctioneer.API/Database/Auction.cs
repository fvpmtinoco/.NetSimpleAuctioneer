namespace NetSimpleAuctioneer.API.Database
{
    public class Auction
    {
        public Guid Id { get; set; }
        public Guid VehicleId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = default!;

        // Navigational property
        public required Vehicle Vehicle { get; set; }
    }
}
