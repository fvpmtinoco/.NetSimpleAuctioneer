using MediatR;
using Microsoft.AspNetCore.Mvc;
using NetSimpleAuctioneer.API.Features.Shared;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace NetSimpleAuctioneer.API.Features.Vehicles.AddHatchback
{
    #region Controller

    /// <summary>
    /// Controller to add a hatchback vehicle
    /// </summary>
    /// <param name="mediator"></param>
    public class AddHatchbackController(IMediator mediator) : VehiclesControllerBase(mediator)
    {
        /// <summary>
        /// Add a hatchback vehicle
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost, ActionName("addHatchback")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResult<AddVehicleErrorCode>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AddHatchback([FromBody, Required] AddHatchbackRequest request)
        {
            var response = await mediator.Send(new AddHatchbackCommand(request.Id, request.Manufacturer, request.Model, request.Year, request.StartingBid, request.NumberOfDoors));

            if (response.HasError)
                return Conflict(response.Error);

            return Created();
        }
    }

    #endregion

    #region Contract

    /// <summary>
    /// Request to add a hatchback vehicle
    /// </summary>
    public class AddHatchbackRequest : AddVehicleRequest
    {
        /// <summary>
        /// Number of doors
        /// </summary>
        [Required]
        [Range(1, 10, ErrorMessage = "Number of doors must be between 1 and 10")]
        public int NumberOfDoors { get; set; }
    }

    #endregion
}
