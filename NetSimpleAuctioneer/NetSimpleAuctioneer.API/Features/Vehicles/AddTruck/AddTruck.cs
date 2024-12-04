using MediatR;
using Microsoft.AspNetCore.Mvc;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;
using System.ComponentModel.DataAnnotations;

namespace NetSimpleAuctioneer.API.Features.Vehicles.AddTruck
{
    #region Controller

    public class AddTruckController(IMediator mediator) : VehiclesControllerBase(mediator)
    {
        /// <summary>
        /// Add a truck vehicle
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost, ActionName("addTruck")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(AddVehicleErrorCode), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(AddVehicleErrorCode), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(AddVehicleErrorCode), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddTruck([FromBody, Required] AddTruckRequest request)
        {
            var response = await mediator.Send(new AddTruckCommand(request.Id, request.Manufacturer, request.Model, request.Year, request.StartingBid, request.LoadCapacity));
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

            return Created(Url.Action("addTruck", new { id = request.Id }), request.Id);
        }
    }

    #endregion

    #region Contract

    /// <summary>
    /// Request to add a truck vehicle
    /// </summary>
    public class AddTruckRequest : AddVehicleRequest
    {
        /// <summary>
        /// Load capacity
        /// </summary>
        [Required]
        [Range(1, 50000, ErrorMessage = "Load capacity must be between 1 and 50000")]
        public int LoadCapacity { get; set; }
    }

    #endregion
}
