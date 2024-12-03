using MediatR;
using Microsoft.AspNetCore.Mvc;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace NetSimpleAuctioneer.API.Features.Vehicles.AddHatchback
{
    #region Controller

    public class AddHatchbackController(IMediator mediator) : VehiclesControllerBase(mediator)
    {
        /// <summary>
        /// Add a hatchback vehicle
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost, ActionName("addHatchback")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(AddVehicleErrorCode), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(AddVehicleErrorCode), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(AddVehicleErrorCode), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddHatchback([FromBody, Required] AddHatchbackRequest request)
        {
            var response = await mediator.Send(new AddHatchbackCommand(request.Id, request.Manufacturer, request.Model, request.Year, request.StartingBid, request.NumberOfDoors));

            if (response.HasError)
            {
                var action = response.Error switch
                {
                    AddVehicleErrorCode.DuplicatedVehicle => StatusCode(StatusCodes.Status409Conflict, response.Error.Value),
                    AddVehicleErrorCode.InvalidYear => StatusCode(StatusCodes.Status422UnprocessableEntity, response.Error.Value),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, response.Error!.Value)
                };

                return action;
            }

            return Created(Url.Action("addHatchback", new { id = request.Id }), request.Id);
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
