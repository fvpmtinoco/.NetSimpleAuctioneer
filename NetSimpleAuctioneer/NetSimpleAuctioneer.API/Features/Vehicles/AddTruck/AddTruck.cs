using MediatR;
using Microsoft.AspNetCore.Mvc;
using NetSimpleAuctioneer.API.Features.Shared;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;
using System.ComponentModel.DataAnnotations;

namespace NetSimpleAuctioneer.API.Features.Vehicles.AddTruck
{
    #region Controller

    public class AddTruckController(IMediator mediator) : VehiclesControllerBase(mediator)
    {
        [HttpPost, ActionName("addTruck")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResult<AddVehicleErrorCode>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AddTruck([FromBody, Required] AddTruckRequest request)
        {
            var response = await mediator.Send(new AddTruckCommand(request.Id, request.Manufacturer, request.Model, request.Year, request.StartingBid, request.LoadCapacity));
            if (response.HasError)
                return Conflict(response.Error);

            return Created();
        }
    }

    #endregion

    public class AddTruckRequest : AddVehicleRequest
    {
        [Required]
        public int LoadCapacity { get; set; }
    }
}
