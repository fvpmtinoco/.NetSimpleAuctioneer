using MediatR;
using Microsoft.AspNetCore.Mvc;
using NetSimpleAuctioneer.API.Features.Auctions.Shared;
using NetSimpleAuctioneer.API.Features.Shared;
using System.ComponentModel.DataAnnotations;

namespace NetSimpleAuctioneer.API.Features.Auctions.StartAuction
{
    #region Controller

    /// <summary>
    /// Controller to start an auction for a vehicle
    /// </summary>
    /// <param name="mediator"></param>
    public class StartAuctionController(IMediator mediator) : AuctionsControllerBase(mediator)
    {
        /// <summary>
        /// Start an auction for a vehicle
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost, ActionName("startAuction")]
        [ProducesResponseType(typeof(StartAuctionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResult<StartAuctionErrorCode>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResult<StartAuctionErrorCode>), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ErrorResult<StartAuctionErrorCode>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> StartAuction([FromBody, Required] StartAuctionRequest request)
        {
            var response = await mediator.Send(new StartAuctionCommand(request.VehicleId));
            if (response.HasError)
            {
                var action = response.Error switch
                {
                    StartAuctionErrorCode.VehicleNotFound => StatusCode(StatusCodes.Status422UnprocessableEntity, StartAuctionErrorCode.VehicleNotFound),
                    StartAuctionErrorCode.AuctionAlreadyActive => StatusCode(StatusCodes.Status409Conflict, StartAuctionErrorCode.AuctionAlreadyActive),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, StartAuctionErrorCode.InternalError),
                };

                return action;
            }

            return Ok(new StartAuctionResponse { AuctionId = response.Result.AuctionId });
        }
    }

    #endregion

    #region Contract

    /// <summary>
    /// Request to start an auction for a vehicle
    /// </summary>
    public class StartAuctionRequest
    {
        /// <summary>
        /// Vehicle identification
        /// </summary>
        [Required]
        public Guid VehicleId { get; set; }
    }

    /// <summary>
    /// Response to starting an auction request
    /// </summary>
    public class StartAuctionResponse
    {
        /// <summary>
        /// Auction identification
        /// </summary>
        [Required]
        public Guid AuctionId { get; set; }
    }

    /// <summary>
    /// Error codes for starting an auction request
    /// </summary>
    public enum StartAuctionErrorCode
    {
        /// <summary>
        /// Provided vehicle identification not found
        /// </summary>
        VehicleNotFound,
        /// <summary>
        /// An auction for the provided vehicle identification is already active
        /// </summary>
        AuctionAlreadyActive,
        /// <summary>
        /// Internal error creating the auction
        /// </summary>
        InternalError
    }

    #endregion
}
