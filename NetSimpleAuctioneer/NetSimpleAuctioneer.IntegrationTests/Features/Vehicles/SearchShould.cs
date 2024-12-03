using AutoFixture;
using FluentAssertions;
using NetSimpleAuctioneer.API.Features.Vehicles.AddHatchback;
using NetSimpleAuctioneer.API.Features.Vehicles.AddSedan;
using NetSimpleAuctioneer.API.Features.Vehicles.AddSUV;
using NetSimpleAuctioneer.API.Features.Vehicles.AddTruck;
using NetSimpleAuctioneer.API.Features.Vehicles.Search;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;
using NetSimpleAuctioneer.IntegrationTests.Helpers;
using RestSharp;
using System.Net;

namespace NetSimpleAuctioneer.IntegrationTests.Features.Vehicles
{
    [Collection("AuctioneerClient")]

    public class SearchShould(AuctioneerFixture auctioneerFixture)
    {
        private readonly string endpointSearch = @"/api/vehicles.search";
        private readonly Fixture fixture = new();
        private readonly AddInformation addVehicleHelper = new(auctioneerFixture);

        [Fact]
        public async Task EnsureVehicleIsFoundWithNoSearchFilters()
        {
            //Arrange
            AddHatchbackRequest addRequest = fixture.Create<AddHatchbackRequest>();
            await addVehicleHelper.AddVehicleAsync<AddHatchbackRequest>(addRequest);

            RestRequest request = new(endpointSearch, Method.Get);
            // Ensure all records are retrieved
            request.AddQueryParameter("pageSize", int.MaxValue);

            //Act
            var response = await auctioneerFixture.RestClient.ExecuteGetAsync<IEnumerable<SearchVehiclesResponse>>(request);

            //Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data!.Select(x => x.Id).Should().Contain(addRequest.Id);
        }

        [Fact]
        public async Task EnsureVehicleIsFoundWithManufacturerFilter()
        {
            //Arrange
            AddHatchbackRequest addRequest = fixture.Create<AddHatchbackRequest>();
            await addVehicleHelper.AddVehicleAsync<AddHatchbackRequest>(addRequest);

            RestRequest request = new(endpointSearch, Method.Get);
            // Ensure all records are retrieved
            request.AddQueryParameter("pageSize", int.MaxValue);
            request.AddQueryParameter("manufacturer", addRequest.Manufacturer);

            //Act
            var response = await auctioneerFixture.RestClient.ExecuteGetAsync<IEnumerable<SearchVehiclesResponse>>(request);

            //Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data!.Select(x => x.Id).Should().Contain(addRequest.Id);
        }

        [Fact]
        public async Task EnsureVehicleIsFoundWithModelFilter()
        {
            //Arrange
            AddSedanRequest addRequest = fixture.Create<AddSedanRequest>();
            await addVehicleHelper.AddVehicleAsync<AddSedanRequest>(addRequest);

            RestRequest request = new(endpointSearch, Method.Get);
            // Ensure all records are retrieved
            request.AddQueryParameter("pageSize", int.MaxValue);
            request.AddQueryParameter("model", addRequest.Model);

            //Act
            var response = await auctioneerFixture.RestClient.ExecuteGetAsync<IEnumerable<SearchVehiclesResponse>>(request);

            //Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data!.Select(x => x.Id).Should().Contain(addRequest.Id);
        }

        [Fact]
        public async Task EnsureVehicleIsFoundWithYearFilter()
        {
            //Arrange
            AddSUVRequest addRequest = fixture.Create<AddSUVRequest>();
            await addVehicleHelper.AddVehicleAsync<AddSUVRequest>(addRequest);

            RestRequest request = new(endpointSearch, Method.Get);
            // Ensure all records are retrieved
            request.AddQueryParameter("pageSize", int.MaxValue);
            request.AddQueryParameter("year", addRequest.Year);

            //Act
            var response = await auctioneerFixture.RestClient.ExecuteGetAsync<IEnumerable<SearchVehiclesResponse>>(request);

            //Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data!.Select(x => x.Id).Should().Contain(addRequest.Id);
        }

        [Fact]
        public async Task EnsureVehicleIsFoundWithVehicleTypeFilter()
        {
            //Arrange
            AddTruckRequest addRequest = fixture.Create<AddTruckRequest>();
            await addVehicleHelper.AddVehicleAsync<AddTruckRequest>(addRequest);

            RestRequest request = new(endpointSearch, Method.Get);
            // Ensure all records are retrieved
            request.AddQueryParameter("pageSize", int.MaxValue);
            request.AddQueryParameter("vehicleType", (int)VehicleType.Truck);

            //Act
            var response = await auctioneerFixture.RestClient.ExecuteGetAsync<IEnumerable<SearchVehiclesResponse>>(request);

            //Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data!.Select(x => x.Id).Should().Contain(addRequest.Id);
        }

        [Fact]
        public async Task EnsureVehicleIsFoundWithAllFilters()
        {
            //Arrange
            AddTruckRequest addRequest = fixture.Create<AddTruckRequest>();
            await addVehicleHelper.AddVehicleAsync<AddTruckRequest>(addRequest);

            RestRequest request = new(endpointSearch, Method.Get);
            // Ensure all records are retrieved
            request.AddQueryParameter("pageSize", int.MaxValue);
            request.AddQueryParameter("manufacturer", addRequest.Manufacturer);
            request.AddQueryParameter("model", addRequest.Model);
            request.AddQueryParameter("year", addRequest.Year);
            request.AddQueryParameter("vehicleType", (int)VehicleType.Truck);

            //Act
            var response = await auctioneerFixture.RestClient.ExecuteGetAsync<IEnumerable<SearchVehiclesResponse>>(request);

            //Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data!.Select(x => x.Id).Should().Contain(addRequest.Id);
        }

        [Fact]
        public async Task EnsurePaginationIsWorkingCorrectly()
        {
            //Arrange
            //Create 5 records
            for (int i = 0; i < 5; i++)
            {
                AddTruckRequest addRequest = fixture.Create<AddTruckRequest>();
                await addVehicleHelper.AddVehicleAsync<AddTruckRequest>(addRequest);
            }

            RestRequest request = new(endpointSearch, Method.Get);
            request.AddQueryParameter("pageNumber", 1);
            request.AddQueryParameter("pageSize", 3);

            //First iteration
            //Act
            var responseFirstIteration = await auctioneerFixture.RestClient.ExecuteGetAsync<IEnumerable<SearchVehiclesResponse>>(request);

            //Assert
            responseFirstIteration.StatusCode.Should().Be(HttpStatusCode.OK);
            responseFirstIteration.Data!.Count().Should().Be(3);

            //Second iteration
            request = new(endpointSearch, Method.Get);
            request.AddQueryParameter("pageNumber", 2);
            request.AddQueryParameter("pageSize", 3);
            var responseSecondIterarion = await auctioneerFixture.RestClient.ExecuteGetAsync<IEnumerable<SearchVehiclesResponse>>(request);

            //Assert
            responseSecondIterarion.StatusCode.Should().Be(HttpStatusCode.OK);
            responseSecondIterarion.Data!.Select(x => x.Id).Should().NotContain(responseFirstIteration.Data!.Select(x => x.Id));
        }

        [Theory]
        [InlineData(1899, HttpStatusCode.BadRequest)]
        [InlineData(2050, HttpStatusCode.UnprocessableEntity)]
        public async Task EnsureNoSearchWhenYearIsInvalid(int year, HttpStatusCode expectedErrorCode)
        {
            //Arrange
            RestRequest request = new(endpointSearch, Method.Get);
            request.AddQueryParameter("year", year);

            //Act
            var response = await auctioneerFixture.RestClient.ExecuteGetAsync(request);

            //Assert
            response.Should().NotBeNull();
            response.StatusCode.Should().Be(expectedErrorCode);
        }
    }
}
