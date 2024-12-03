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
        [HttpPost, ActionName("addSedan")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResult<AddVehicleErrorCode>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AddSedan([FromBody, Required] AddSedanRequest request)
        {
            var response = await mediator.Send(new AddSedanCommand(request.Id, request.Manufacturer, request.Model, request.Year, request.StartingBid, request.NumberOfDoors));
            if (response.HasError)
                return Conflict(response.Error);

            return Created();
        }
    }

    #endregion

    #region Contract

    public class AddSedanRequest : AddVehicleRequest
    {
        [Required]
        [Range(0, 10, ErrorMessage = "Number of doors must be between 1 and 10")]
        public int NumberOfDoors { get; set; }
    }

    #endregion
}
