using MediatR;
using Microsoft.AspNetCore.Mvc;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace NetSimpleAuctioneer.API.Features.Vehicles.Search
{
    #region Controllers

    public class SearchVehicleController(IMediator mediator) : VehiclesControllerBase(mediator)
    {
        /// <summary>
        /// Searches for vehicles based on the specified criteria, ensuring that the vehicles are either currently in an active auction 
        /// or are not part of a closed auction.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet, ActionName("search")]
        [ProducesResponseType(typeof(IEnumerable<SearchVehiclesResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(SearchVehicleErrorCode), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(SearchVehicleErrorCode), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SearchVehicles([FromQuery, Required] SearchVehiclesRequest request)
        {
            var response = await mediator.Send(new SearchVehicleQuery(request.VehicleType, request.Manufacturer, request.Model, request.Year, request.PageNumber!.Value, request.PageSize!.Value));

            if (response.HasError)
            {
                var action = response.Error switch
                {
                    SearchVehicleErrorCode.InvalidYear => StatusCode(StatusCodes.Status422UnprocessableEntity, response.Error.Value),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, response.Error!.Value)
                };

                return action;
            }
            return StatusCode(StatusCodes.Status200OK, response.Result.Select(r => new SearchVehiclesResponse(r.Id, r.VehicleType, r.Manufacturer, r.Model, r.Year, r.StartingBid, r.AuctionId)));
        }
    }

    #endregion

    #region Contracts

    /// <summary>
    /// Search vehicles request
    /// </summary>
    public class SearchVehiclesRequest
    {
        /// <summary>
        /// Vehicle type
        /// </summary>
        public VehicleType? VehicleType { get; set; }

        /// <summary>
        /// Manufacturer
        /// </summary>
        public string? Manufacturer { get; set; }

        /// <summary>
        /// Model
        /// </summary>
        public string? Model { get; set; }

        /// <summary>
        /// Year
        /// </summary>
        [Range(1900, Int32.MaxValue)]
        public int? Year { get; set; }

        /// <summary>
        /// Pagination property - Page number 
        /// </summary>
        [Range(1, int.MaxValue)]
        public int? PageNumber { get; set; } = 1;

        /// <summary>
        /// Pagination property - Page size
        /// </summary>
        [Range(1, int.MaxValue)]
        public int? PageSize { get; set; } = 10;
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
