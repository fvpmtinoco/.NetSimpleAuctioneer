namespace NetSimpleAuctioneer.API.Database
{
    public class Vehicle
    {
        public Guid Id { get; set; }
        public string Manufacturer { get; set; } = default!;
        public string Model { get; set; } = default!;
        public int Year { get; set; }
        public decimal StartingBid { get; set; }
        //Hatchback = 0, Sedan = 1, SUV = 2, Truck = 3
        public int VehicleType { get; set; }
        public int? NumberOfDoors { get; set; }
        public int? NumberOfSeats { get; set; }
        public decimal? LoadCapacity { get; set; }
    }
}
