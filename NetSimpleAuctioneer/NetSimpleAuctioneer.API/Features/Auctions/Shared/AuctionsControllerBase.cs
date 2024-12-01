using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace NetSimpleAuctioneer.API.Features.Auctions.Shared
{
    [ApiController]
    [Route("api/auctions.[action]")]
    public class AuctionsControllerBase(IMediator mediator) : ControllerBase
    {
        protected readonly IMediator mediator = mediator;
    }
}
