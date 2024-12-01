using MediatR;
using Microsoft.AspNetCore.Mvc;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;
using System.ComponentModel.DataAnnotations;

namespace NetSimpleAuctioneer.API.Features.Vehicles.Search
{
    public class SearchVehicleController(IMediator mediator) : VehiclesControllerBase(mediator)
    {
        [HttpGet, ActionName("search")]
        public async Task<IActionResult> SearchVehicles([FromQuery, Required] SearchVehiclesRequest request)
        {
            var response = await mediator.Send(new SearchVehicleQuery(request.VehicleType, request.Manufacturer, request.Model, request.Year));
            return Ok(response);
        }
    }

    public class SearchVehiclesRequest : IRequest<List<SearchVehiclesResponse>>
    {
        public VehicleType? VehicleType { get; set; }
        public string? Manufacturer { get; set; }
        public string? Model { get; set; }
        public int? Year { get; set; }
    }

    public class SearchVehiclesResponse
    {
        public Guid Id { get; }
        public string Type { get; }
        public string Manufacturer { get; }
        public string Model { get; }
        public int Year { get; }
        public Dictionary<string, object> Attributes { get; }

        public SearchVehiclesResponse(Guid id, string type, string manufacturer, string model, int year, Dictionary<string, object> attributes)
        {
            Id = id;
            Type = type;
            Manufacturer = manufacturer;
            Model = model;
            Year = year;
            Attributes = attributes;
        }
    }
}
