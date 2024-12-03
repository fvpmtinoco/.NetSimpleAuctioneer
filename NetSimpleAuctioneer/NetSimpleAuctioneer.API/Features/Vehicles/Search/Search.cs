using MediatR;
using Microsoft.AspNetCore.Mvc;
using NetSimpleAuctioneer.API.Features.Shared;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace NetSimpleAuctioneer.API.Features.Vehicles.Search
{
    #region Controllers

    public class SearchVehicleController(IMediator mediator) : VehiclesControllerBase(mediator)
    {
        /// <summary>
        /// Search for vehicles that match the provided criteria and that are in a active auction or not in a closed auction
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet, ActionName("search")]
        [ProducesResponseType(typeof(SearchVehiclesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResult<SearchVehicleErrorCode>), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ErrorResult<SearchVehicleErrorCode>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SearchVehicles([FromQuery, Required] SearchVehiclesRequest request)
        {
            var response = await mediator.Send(new SearchVehicleQuery(request.VehicleType, request.Manufacturer, request.Model, request.Year));

            if (response.HasError)
            {
                var result = response.Error switch
                {
                    SearchVehicleErrorCode.InvalidYear => StatusCode(StatusCodes.Status422UnprocessableEntity, response.Error.Value),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, response.Error!.Value)
                };

                return result;
            }
            return StatusCode(StatusCodes.Status200OK, response.Result);
        }
    }

    #endregion

    #region Contracts

    /// <summary>
    /// Search vehicles request
    /// </summary>
    public class SearchVehiclesRequest
    {
        public VehicleType? VehicleType { get; set; }
        public string? Manufacturer { get; set; }
        public string? Model { get; set; }
        [Range(1900, Int32.MaxValue)]
        public int? Year { get; set; }
    }

    /// <summary>
    /// Search vehicles response
    /// </summary>
    /// <param name="Id"></param>
    /// <param name="VehicleType"></param>
    /// <param name="Manufacturer"></param>
    /// <param name="Model"></param>
    /// <param name="Year"></param>
    /// <param name="StartingBid"></param>
    /// <param name="AuctionId"></param>
    public record SearchVehiclesResponse(Guid Id, VehicleType VehicleType, string Manufacturer, string Model, int Year, decimal StartingBid, Guid? AuctionId);

    /// <summary>
    /// Search vehicle error codes
    /// </summary>
    public enum SearchVehicleErrorCode
    {
        [Description("The vehicle's year cannot be above the current year")]
        InvalidYear,
        [Description("An internal error occurred")]
        InternalError
    }

    #endregion

}
