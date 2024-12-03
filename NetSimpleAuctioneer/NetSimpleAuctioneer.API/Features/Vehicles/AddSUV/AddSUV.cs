using MediatR;
using Microsoft.AspNetCore.Mvc;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;
using System.ComponentModel.DataAnnotations;

namespace NetSimpleAuctioneer.API.Features.Vehicles.AddSUV
{
    #region Controller

    public class AddSUVController(IMediator mediator) : VehiclesControllerBase(mediator)
    {
        /// <summary>
        /// Add an SUV vehicle
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost, ActionName("addSUV")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(AddVehicleErrorCode), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(AddVehicleErrorCode), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(AddVehicleErrorCode), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddSUV([FromBody, Required] AddSUVRequest request)
        {
            var response = await mediator.Send(new AddSUVCommand(request.Id, request.Manufacturer, request.Model, request.Year, request.StartingBid, request.NumberOfSeats));
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

            return Created(Url.Action("addSUV", new { id = request.Id }), request.Id);
        }
    }

    #endregion

    #region Contract

    /// <summary>
    /// Request to add an SUV vehicle
    /// </summary>
    public class AddSUVRequest : AddVehicleRequest
    {
        /// <summary>
        /// Number of seats
        /// </summary>
        [Required]
        [Range(1, 20, ErrorMessage = "Number of seats must be between 1 and 20")]
        public int NumberOfSeats { get; set; }
    }

    #endregion
}
