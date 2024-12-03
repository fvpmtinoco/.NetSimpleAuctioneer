using MediatR;
using Microsoft.AspNetCore.Mvc;
using NetSimpleAuctioneer.API.Features.Auctions.Shared;
using NetSimpleAuctioneer.API.Features.Shared;
using System.ComponentModel.DataAnnotations;

namespace NetSimpleAuctioneer.API.Features.Auctions.PlaceBid
{
    /// <summary>
    /// Controller to place a bid in an auction
    /// </summary>
    /// <param name="mediator"></param>
    public class PlaceBidController(IMediator mediator) : AuctionsControllerBase(mediator)
    {
        /// <summary>
        /// Place a bid in an auction
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost, ActionName("placeBid")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResult<PlaceBidErrorCode>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResult<PlaceBidErrorCode>), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ErrorResult<PlaceBidErrorCode>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PlaceBid([FromBody, Required] PlaceBidRequest request)
        {
            var response = await mediator.Send(new PlaceBidCommand(request.AuctionId, request.BidderEmail, request.BidAmount));
            if (response.HasError)
            {
                var action = response.Error switch
                {
                    PlaceBidErrorCode.AuctionNotFound => StatusCode(StatusCodes.Status422UnprocessableEntity, PlaceBidErrorCode.AuctionNotFound),
                    PlaceBidErrorCode.BidAmountTooLow => StatusCode(StatusCodes.Status422UnprocessableEntity, PlaceBidErrorCode.BidAmountTooLow),
                    PlaceBidErrorCode.ExistingHigherBid => StatusCode(StatusCodes.Status409Conflict, PlaceBidErrorCode.ExistingHigherBid),
                    PlaceBidErrorCode.BidderHasHigherBid => StatusCode(StatusCodes.Status409Conflict, PlaceBidErrorCode.BidderHasHigherBid),
                    PlaceBidErrorCode.AuctionAlreadyClosed => StatusCode(StatusCodes.Status409Conflict, PlaceBidErrorCode.AuctionAlreadyClosed),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, PlaceBidErrorCode.InternalError),
                };

                return action;
            }

            return Ok();
        }
    }

    /// <summary>
    /// Request to place a bid in an auction
    /// </summary>
    public class PlaceBidRequest
    {
        /// <summary>
        /// Auction identification
        /// </summary>
        [Required]
        public Guid AuctionId { get; set; }

        /// <summary>
        /// Bidder email
        /// </summary>
        [Required]
        [EmailAddress]
        public string BidderEmail { get; set; } = default!;

        /// <summary>
        /// Bid amount
        /// </summary>
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Bid amount must be greater than 0.")]
        public decimal BidAmount { get; set; }
    }

    /// <summary>
    /// Error codes for placing a bid in an auction
    /// </summary>
    public enum PlaceBidErrorCode
    {
        /// <summary>
        /// Provided auction identification not found
        /// </summary>
        AuctionNotFound,

        /// <summary>
        /// The auction for the provided identification has already closed
        /// </summary>
        AuctionAlreadyClosed,

        /// <summary>
        /// The bid amount is lower than the vehicle's minimum bid
        /// </summary>
        BidAmountTooLow,

        /// <summary>
        /// Existing higher bid in the current auction
        /// </summary>
        ExistingHigherBid,

        /// <summary>
        /// Bidder has already the higher bid
        /// </summary>
        BidderHasHigherBid,

        /// <summary>
        /// Internal error placing the bid
        /// </summary>
        InternalError
    }
}
