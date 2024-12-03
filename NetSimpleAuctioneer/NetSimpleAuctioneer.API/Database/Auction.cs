namespace NetSimpleAuctioneer.API.Database
{
    public class Auction
    {
        public Guid Id { get; set; }
        public Guid VehicleId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
