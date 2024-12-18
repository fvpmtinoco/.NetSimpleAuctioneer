﻿namespace NetSimpleAuctioneer.API.Domain
{
    public class Auction
    {
        public Guid Id { get; set; }
        public Guid VehicleId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Vehicle? Vehicle { get; set; }
    }
}
