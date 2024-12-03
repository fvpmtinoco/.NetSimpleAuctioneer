using AutoFixture;
using FluentAssertions;
using NetSimpleAuctioneer.API.Features.Vehicles.AddTruck;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;
using NetSimpleAuctioneer.IntegrationTests.Helpers;
using Newtonsoft.Json;
using RestSharp;
using System.Net;

namespace NetSimpleAuctioneer.IntegrationTests.Features.Vehicles
{
    [Collection("AuctioneerClient")]

    public class AddTruckShould(AuctioneerFixture auctioneerFixture)
    {
        private readonly string endpointCreate = @"/api/vehicles.addTruck";
        private readonly Fixture fixture = new();

        [Theory]
        [MemberData(nameof(EnsureDataContractIsRespectedData))]
        public async Task EnsureDataContractIsRespected(string jsonBody)
        {
            //Arrange
            RestRequest request = new(endpointCreate, Method.Post);
            request.AddJsonBody(jsonBody);

            //Act
            var response = await auctioneerFixture.RestClient.ExecutePostAsync(request);

            //Assert
            response.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        public static IEnumerable<object[]> EnsureDataContractIsRespectedData()
        {
            Fixture fixture = new();
            var defaultRequest = fixture.Create<AddTruckRequest>();

            // No Id in request body
            yield return new object[]
            {
                JsonConvert.SerializeObject(
                    defaultRequest,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new ExcludePropertiesResolver(nameof(AddTruckRequest.Id))
                    })
            };

            // No Manufacturer in request body
            yield return new object[]
            {
                JsonConvert.SerializeObject(
                    defaultRequest,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new ExcludePropertiesResolver(nameof(AddTruckRequest.Manufacturer))
                    })
            };

            // No LoadCapacity in request body
            yield return new object[]
            {
                JsonConvert.SerializeObject(
                    defaultRequest,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new ExcludePropertiesResolver(nameof(AddTruckRequest.LoadCapacity))
                    })
            };

            // No Model in request body
            yield return new object[]
            {
                JsonConvert.SerializeObject(
                    defaultRequest,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new ExcludePropertiesResolver(nameof(AddTruckRequest.Model))
                    })
            };

            // No StartingBid in request body
            yield return new object[]
            {
                JsonConvert.SerializeObject(
                    defaultRequest,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new ExcludePropertiesResolver(nameof(AddTruckRequest.StartingBid))
                    })
            };

            // No Year in request body
            yield return new object[]
            {
                JsonConvert.SerializeObject(
                    defaultRequest,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new ExcludePropertiesResolver(nameof(AddTruckRequest.Year))
                    })
            };

            defaultRequest = fixture.Create<AddTruckRequest>();
            defaultRequest.LoadCapacity = 0;
            // Invalid load capacity
            yield return new object[]
            {
                JsonConvert.SerializeObject(defaultRequest)
            };

            defaultRequest = fixture.Create<AddTruckRequest>();
            defaultRequest.Manufacturer = "a";
            // Invalid Manufacturer value
            yield return new object[]
            {
                JsonConvert.SerializeObject(defaultRequest)
            };

            defaultRequest = fixture.Create<AddTruckRequest>();
            defaultRequest.Model = "a";
            // Invalid Model value
            yield return new object[]
            {
                JsonConvert.SerializeObject(defaultRequest)
            };

            defaultRequest = fixture.Create<AddTruckRequest>();
            defaultRequest.StartingBid = -1;
            // Invalid StartingBid value
            yield return new object[]
            {
                JsonConvert.SerializeObject(defaultRequest)
            };
        }

        [Fact]
        public async Task EnsureVehicleIsAdded()
        {
            //Arrange
            RestRequest request = new(endpointCreate, Method.Post);
            var AddTruckRequest = new AddTruckRequest
            {
                Manufacturer = "Scania",
                Model = "Big",
                Year = 2021,
                StartingBid = 10000,
                LoadCapacity = 5000,
                Id = fixture.Create<Guid>()
            };
            request.AddJsonBody(AddTruckRequest);
            //Act
            var response = await auctioneerFixture.RestClient.ExecutePostAsync(request);
            //Assert
            response.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task EnsureVehicleIsNotAddedWhenYearIsInvalid()
        {
            //Arrange
            RestRequest request = new(endpointCreate, Method.Post);
            var AddTruckRequest = new AddTruckRequest
            {
                Manufacturer = "Volvo",
                Model = "Huge",
                Year = 2050,
                StartingBid = 10000,
                LoadCapacity = 6000,
                Id = fixture.Create<Guid>()
            };
            request.AddJsonBody(AddTruckRequest);
            //Act
            var response = await auctioneerFixture.RestClient.ExecutePostAsync<AddVehicleErrorCode>(request);
            //Assert
            response.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            response.Data.Should().Be(AddVehicleErrorCode.InvalidYear);
        }

        [Fact]
        public async Task EnsureVehicleWithSameIdIsNotAdded()
        {
            //Arrange
            RestRequest request = new(endpointCreate, Method.Post);
            var AddTruckRequest = new AddTruckRequest
            {
                Manufacturer = "Volvo",
                Model = "Huge",
                Year = 2024,
                StartingBid = 10000,
                LoadCapacity = 4000,
                Id = fixture.Create<Guid>()
            };
            request.AddJsonBody(AddTruckRequest);
            await auctioneerFixture.RestClient.ExecutePostAsync<AddVehicleErrorCode>(request);

            request = new(endpointCreate, Method.Post);
            AddTruckRequest.Manufacturer = "Mercedes";
            AddTruckRequest.Model = "Big";
            request.AddJsonBody(AddTruckRequest);

            //Act
            var response = await auctioneerFixture.RestClient.ExecutePostAsync<AddVehicleErrorCode>(request);

            //Assert
            response.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            response.Data.Should().Be(AddVehicleErrorCode.DuplicatedVehicle);
        }
    }
}