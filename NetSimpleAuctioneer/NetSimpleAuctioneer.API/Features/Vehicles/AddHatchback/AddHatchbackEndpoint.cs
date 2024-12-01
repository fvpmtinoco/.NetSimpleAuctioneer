using MediatR;
using Microsoft.AspNetCore.Mvc;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace NetSimpleAuctioneer.API.Features.Vehicles.AddHatchback
{
    [ApiController]
    [Route("api/vehicles/hatchbacks")]
    public class AddHatchbackController(IMediator mediator) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> AddHatchback([FromBody, Required] AddHatchbackRequest command)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await mediator.Send(command);
            return CreatedAtAction(nameof(AddHatchback), new { id = command.Id }, null);
        }
    }

    public class AddHatchbackRequest : AddVehicleRequest
    {
        [Required]
        [Range(0, 10, ErrorMessage = "Number of doors must be between 1 and 10.")]
        public int NumberOfDoors { get; set; }
    }
}
