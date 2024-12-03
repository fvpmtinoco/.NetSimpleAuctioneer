using MediatR;
using Microsoft.AspNetCore.Mvc;
using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Features.Auctions.Shared;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace NetSimpleAuctioneer.API.Features.Auctions.StartAuction
{
    #region Controller

    public class StartAuctionController(IMediator mediator) : AuctionsControllerBase(mediator)
    {
        /// <summary>
        /// Start an auction for a vehicle
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost, ActionName("startAuction")]
        [ProducesResponseType(typeof(StartAuctionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(StartAuctionErrorCode), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(StartAuctionErrorCode), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(StartAuctionErrorCode), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> StartAuction([FromBody, Required] StartAuctionRequest request)
        {
            var response = await mediator.Send(new StartAuctionCommand(request.VehicleId));
            if (response.HasError)
            {
                var action = response.Error switch
                {
                    StartAuctionErrorCode.InvalidVehicle => StatusCode(StatusCodes.Status422UnprocessableEntity, StartAuctionErrorCode.InvalidVehicle),
                    StartAuctionErrorCode.AuctionForVehicleAlreadyActive => StatusCode(StatusCodes.Status409Conflict, StartAuctionErrorCode.AuctionForVehicleAlreadyActive),
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
        [NotEmptyGuid(ErrorMessage = "Vehicle Id cannot be empty.")]
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
        [Description("Provided vehicle identification is invalid")]
        InvalidVehicle,

        [Description("An auction for the provided vehicle identification is already active")]
        AuctionForVehicleAlreadyActive,

        [Description("Internal error creating the auction")]
        InternalError
    }

    #endregion
}
