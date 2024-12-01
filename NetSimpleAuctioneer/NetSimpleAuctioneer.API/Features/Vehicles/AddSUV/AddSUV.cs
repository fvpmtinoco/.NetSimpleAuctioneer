using MediatR;
using Microsoft.AspNetCore.Mvc;
using NetSimpleAuctioneer.API.Features.Shared;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;
using System.ComponentModel.DataAnnotations;

namespace NetSimpleAuctioneer.API.Features.Vehicles.AddSUV
{
    #region Controller

    public class AddSUVController(IMediator mediator) : VehiclesControllerBase(mediator)
    {
        [HttpPost, ActionName("addSUV")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(List<ErrorResult<AddVehicleErrorCode>>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AddSUV([FromBody, Required] AddSUVRequest request)
        {
            var response = await mediator.Send(new AddSUVCommand(request.Id, request.Manufacturer, request.Model, request.Year, request.StartingBid, request.NumberOfSeats));
            if (response.HasErrors)
                return Conflict(response.Errors);

            return Created();
        }
    }

    #endregion

    #region Contract

    public class AddSUVRequest : AddVehicleRequest
    {
        [Required]
        [Range(0, 20, ErrorMessage = "Number of seats must be between 1 and 20")]
        public int NumberOfSeats { get; set; }
    }

    #endregion
}
