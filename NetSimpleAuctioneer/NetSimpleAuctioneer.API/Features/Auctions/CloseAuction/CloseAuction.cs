using MediatR;
using Microsoft.AspNetCore.Mvc;
using NetSimpleAuctioneer.API.Features.Auctions.Shared;
using NetSimpleAuctioneer.API.Features.Shared;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace NetSimpleAuctioneer.API.Features.Auctions.CloseAuction
{
    #region Controller

    public class CloseAuctionController(IMediator mediator) : AuctionsControllerBase(mediator)
    {
        /// <summary>
        /// Close an auction
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost, ActionName("closeAuction")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResult<CloseAuctionErrorCode>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResult<CloseAuctionErrorCode>), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ErrorResult<CloseAuctionErrorCode>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CloseAuction([FromBody, Required] CloseAuctionRequest request)
        {
            var response = await mediator.Send(new CloseAuctionCommand(request.AuctionId));
            if (response.HasError)
            {
                var action = response.Error switch
                {
                    CloseAuctionErrorCode.AuctionNotFound => StatusCode(StatusCodes.Status422UnprocessableEntity, CloseAuctionErrorCode.AuctionNotFound),
                    CloseAuctionErrorCode.AuctionAlreadyClosed => StatusCode(StatusCodes.Status409Conflict, CloseAuctionErrorCode.AuctionAlreadyClosed),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, CloseAuctionErrorCode.InternalError),
                };

                return action;
            }

            return Ok();
        }
    }

    #endregion

    #region Contract

    /// <summary>
    /// Request to close an auction
    /// </summary>
    public class CloseAuctionRequest
    {
        /// <summary>
        /// Auction identification to close
        /// </summary>
        [Required]
        public Guid AuctionId { get; set; }
    }

    /// <summary>
    /// Error codes for closing an auction
    /// </summary>
    public enum CloseAuctionErrorCode
    {
        [Description("Provided auction identification not found")]
        AuctionNotFound,

        [Description("The auction for the provided identification has already closed")]
        AuctionAlreadyClosed,

        [Description("Internal error ending the auction")]
        InternalError
    }

    #endregion
}
