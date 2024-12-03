using NetSimpleAuctioneer.API.Application;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace NetSimpleAuctioneer.API.Features.Vehicles.Shared
{
    public class AddVehicleRequest
    {
        /// <summary>
        /// The unique identifier of the vehicle.
        /// </summary>
        [Required]
        [NotEmptyGuid(ErrorMessage = "Id cannot be empty.")]
        public Guid Id { get; set; }

        /// <summary>
        /// The manufacturer of the vehicle.
        /// </summary>
        [Required]
        [MinLength(2)]
        public string Manufacturer { get; set; } = default!;

        /// <summary>
        /// The model of the vehicle.
        /// </summary>
        [Required]
        [MinLength(2)]
        public string Model { get; set; } = default!;

        /// <summary>
        /// The year the vehicle was manufactured.
        /// </summary>
        [Required]
        [Range(1900, 2100)]
        public int Year { get; set; }

        /// <summary>
        /// The starting bid for the vehicle.
        /// </summary>
        [Required]
        [Range(1, 1000000000)]
        public decimal StartingBid { get; set; }
    }
}