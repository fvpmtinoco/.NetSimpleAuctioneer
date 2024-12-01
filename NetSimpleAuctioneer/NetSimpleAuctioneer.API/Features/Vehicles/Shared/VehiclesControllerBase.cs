using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace NetSimpleAuctioneer.API.Features.Vehicles.Shared
{
    [ApiController]
    [Route("api/vehicles.[action]")]
    public class VehiclesControllerBase(IMediator mediator) : ControllerBase
    {
        protected readonly IMediator mediator = mediator;
    }
}
