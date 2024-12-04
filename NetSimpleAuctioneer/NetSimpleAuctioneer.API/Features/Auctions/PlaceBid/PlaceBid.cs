using MediatR;
using Microsoft.AspNetCore.Mvc;
using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Features.Auctions.Shared;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace NetSimpleAuctioneer.API.Features.Auctions.PlaceBid
{
    #region Controller

    public class PlaceBidController(IMediator mediator) : AuctionsControllerBase(mediator)
    {
        /// <summary>
        /// Place a bid in an auction
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost, ActionName("placeBid")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(PlaceBidErrorCode), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(PlaceBidErrorCode), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(PlaceBidErrorCode), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PlaceBid([FromBody, Required] PlaceBidRequest request)
        {
            var response = await mediator.Send(new PlaceBidCommand(request.AuctionId, request.BidderEmail, request.BidAmount));
            if (response.HasError)
            {
                var action = response.Error switch
                {
                    PlaceBidErrorCode.InvalidAuction => StatusCode(StatusCodes.Status422UnprocessableEntity, PlaceBidErrorCode.InvalidAuction),
                    PlaceBidErrorCode.BidAmountTooLow => StatusCode(StatusCodes.Status422UnprocessableEntity, PlaceBidErrorCode.BidAmountTooLow),
                    PlaceBidErrorCode.ExistingHigherBid => StatusCode(StatusCodes.Status409Conflict, PlaceBidErrorCode.ExistingHigherBid),
                    PlaceBidErrorCode.BidderHasHigherBid => StatusCode(StatusCodes.Status409Conflict, PlaceBidErrorCode.BidderHasHigherBid),
                    PlaceBidErrorCode.AuctionAlreadyClosed => StatusCode(StatusCodes.Status409Conflict, PlaceBidErrorCode.AuctionAlreadyClosed),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, PlaceBidErrorCode.InternalError),
                };

                return action;
            }

            return Ok(new PlaceBidResponse { BidId = response.Result.BidId });
        }
    }

    #endregion

    #region Contract

    /// <summary>
    /// Request to place a bid in an auction
    /// </summary>
    public class PlaceBidRequest
    {
        /// <summary>
        /// Auction identification
        /// </summary>
        [Required]
        [NotEmptyGuid(ErrorMessage = "Auction Id cannot be empty.")]
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
        [Range(0.01, 1000000000, ErrorMessage = "Bid amount must be greater than 0.")]
        public decimal BidAmount { get; set; }
    }

    /// <summary>
    /// Response to placing a bid request
    /// </summary>
    public class PlaceBidResponse
    {
        /// <summary>
        /// Bid identification
        /// </summary>
        [Required]
        public Guid BidId { get; set; }
    }

    /// <summary>
    /// Error codes for placing a bid in an auction
    /// </summary>
    public enum PlaceBidErrorCode
    {
        [Description("Provided auction identification not found")]
        InvalidAuction,

        [Description("The auction for the provided identification has already closed")]
        AuctionAlreadyClosed,

        [Description("The bid amount is lower than the vehicle's minimum bid")]
        BidAmountTooLow,

        [Description("Existing higher bid in the current auction")]
        ExistingHigherBid,

        [Description("Bidder has already the higher bid")]
        BidderHasHigherBid,

        [Description("Internal error placing the bid")]
        InternalError
    }

    #endregion
}
