using MediatR;
using Microsoft.AspNetCore.Mvc;
using NetSimpleAuctioneer.API.Features.Auctions.Shared;
using NetSimpleAuctioneer.API.Features.Shared;
using System.ComponentModel.DataAnnotations;

namespace NetSimpleAuctioneer.API.Features.Auctions.PlaceBid
{
    public class PlaceBidController(IMediator mediator) : AuctionsControllerBase(mediator)
    {
        [HttpPost, ActionName("placeBid")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(List<ErrorResult<PlaceBidErrorCode>>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> PlaceBid([FromBody, Required] PlaceBidRequest request)
        {
            var response = await mediator.Send(new PlaceBidCommand(request.VehicleId, request.BidderName, request.BidAmount));
            if (response.HasErrors)
                return Conflict(response.Errors);

            return Ok();
        }
    }

    public class PlaceBidRequest
    {
        [Required]
        public Guid VehicleId { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Bidder name must be between 1 and 100 characters.")]
        public string BidderName { get; set; } = default!;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Bid amount must be greater than 0.")]
        public decimal BidAmount { get; set; }
    }

    public enum PlaceBidErrorCode
    {
        AuctionNotFound,
        AuctionNotActive,
        BidAmountTooLow
    }
}
