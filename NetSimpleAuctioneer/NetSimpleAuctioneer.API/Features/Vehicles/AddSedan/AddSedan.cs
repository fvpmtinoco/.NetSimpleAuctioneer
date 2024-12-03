using MediatR;
using Microsoft.AspNetCore.Mvc;
using NetSimpleAuctioneer.API.Features.Shared;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace NetSimpleAuctioneer.API.Features.Vehicles.AddSedan
{
    #region Controller

    public class AddSedanController(IMediator mediator) : VehiclesControllerBase(mediator)
    {
        /// <summary>
        /// Add a sedan vehicle
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost, ActionName("addSedan")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResult<AddVehicleErrorCode>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResult<AddVehicleErrorCode>), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ErrorResult<AddVehicleErrorCode>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddSedan([FromBody, Required] AddSedanRequest request)
        {
            var response = await mediator.Send(new AddSedanCommand(request.Id, request.Manufacturer, request.Model, request.Year, request.StartingBid, request.NumberOfDoors));
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

            return Created(Url.Action("addSedan", new { id = request.Id }), request.Id);
        }
    }

    #endregion

    #region Contract

    /// <summary>
    /// Request to add a sedan vehicle
    /// </summary>
    public class AddSedanRequest : AddVehicleRequest
    {
        /// <summary>
        /// Number of doors
        /// </summary>
        [Required]
        [Range(0, 10, ErrorMessage = "Number of doors must be between 1 and 10")]
        public int NumberOfDoors { get; set; }
    }

    #endregion
}
