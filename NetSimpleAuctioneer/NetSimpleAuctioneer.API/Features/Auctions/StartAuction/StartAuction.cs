using MediatR;
using Microsoft.AspNetCore.Mvc;
using NetSimpleAuctioneer.API.Features.Auctions.Shared;
using NetSimpleAuctioneer.API.Features.Shared;
using System.ComponentModel.DataAnnotations;

namespace NetSimpleAuctioneer.API.Features.Auctions.StartAuction
{
    public class StartAuctionController(IMediator mediator) : AuctionsControllerBase(mediator)
    {
        [HttpPost, ActionName("startAuction")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(List<ErrorResult<StartAuctionErrorCode>>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> StartAuction([FromBody, Required] StartAuctionRequest request)
        {
            var response = await mediator.Send(new StartAuctionCommand(request.VehicleId));
            if (response.HasErrors)
                return Conflict(response.Errors);

            return Ok();
        }
    }

    public class StartAuctionRequest
    {
        [Required]
        public Guid VehicleId { get; set; }
    }

    public enum StartAuctionErrorCode
    {
        VehicleNotFound,
        AuctionAlreadyActive
    }
}
