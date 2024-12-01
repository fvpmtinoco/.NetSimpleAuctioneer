using MediatR;
using Microsoft.AspNetCore.Mvc;
using NetSimpleAuctioneer.API.Features.Auctions.Shared;
using NetSimpleAuctioneer.API.Features.Shared;
using System.ComponentModel.DataAnnotations;

namespace NetSimpleAuctioneer.API.Features.Auctions.CloseAuction
{
    public class CloseAuctionController(IMediator mediator) : AuctionsControllerBase(mediator)
    {
        [HttpPost, ActionName("closeAuction")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(List<ErrorResult<CloseAuctionErrorCode>>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CloseAuction([FromBody, Required] CloseAuctionRequest request)
        {
            var response = await mediator.Send(new CloseAuctionCommand(request.VehicleId));
            if (response.HasErrors)
                return Conflict(response.Errors);

            return Ok();
        }
    }

    public class CloseAuctionRequest
    {
        [Required]
        public Guid VehicleId { get; set; }
    }

    public enum CloseAuctionErrorCode
    {
        AuctionNotFound,
        AuctionAlreadyClosed
    }
}
