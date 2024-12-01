using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace NetSimpleAuctioneer.API.Features.Vehicles.Shared
{
    public class AddVehicleRequest
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        [MinLength(2)]
        public string Manufacturer { get; set; } = default!;

        [Required]
        [MinLength(2)]
        public string Model { get; set; } = default!;

        [Required]
        [Range(1900, int.MaxValue)]
        public int Year { get; set; }

        [Required]
        [Range(1, double.MaxValue)]
        public decimal StartingBid { get; set; }
    }
}