using AutoFixture;
using FluentAssertions;
using NetSimpleAuctioneer.API.Features.Vehicles.AddSedan;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;
using NetSimpleAuctioneer.IntegrationTests.Helpers;
using Newtonsoft.Json;
using RestSharp;
using System.Net;

namespace NetSimpleAuctioneer.IntegrationTests.Features.Vehicles
{
    [Collection("AuctioneerClient")]

    public class AddSedanShould(AuctioneerFixture auctioneerFixture)
    {
        private readonly string endpointCreate = @"/api/vehicles.addSedan";
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
            var defaultRequest = fixture.Create<AddSedanRequest>();

            // No Id in request body
            yield return new object[]
            {
                JsonConvert.SerializeObject(
                    defaultRequest,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new ExcludePropertiesResolver(nameof(AddSedanRequest.Id))
                    })
            };

            // No Manufacturer in request body
            yield return new object[]
            {
                JsonConvert.SerializeObject(
                    defaultRequest,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new ExcludePropertiesResolver(nameof(AddSedanRequest.Manufacturer))
                    })
            };

            // No NumberOfDoors in request body
            yield return new object[]
            {
                JsonConvert.SerializeObject(
                    defaultRequest,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new ExcludePropertiesResolver(nameof(AddSedanRequest.NumberOfDoors))
                    })
            };

            // No Model in request body
            yield return new object[]
            {
                JsonConvert.SerializeObject(
                    defaultRequest,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new ExcludePropertiesResolver(nameof(AddSedanRequest.Model))
                    })
            };

            // No StartingBid in request body
            yield return new object[]
            {
                JsonConvert.SerializeObject(
                    defaultRequest,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new ExcludePropertiesResolver(nameof(AddSedanRequest.StartingBid))
                    })
            };

            // No Year in request body
            yield return new object[]
            {
                JsonConvert.SerializeObject(
                    defaultRequest,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new ExcludePropertiesResolver(nameof(AddSedanRequest.Year))
                    })
            };

            defaultRequest = fixture.Create<AddSedanRequest>();
            defaultRequest.NumberOfDoors = 0;
            // Invalid number of doors
            yield return new object[]
            {
                JsonConvert.SerializeObject(defaultRequest)
            };

            defaultRequest = fixture.Create<AddSedanRequest>();
            defaultRequest.Manufacturer = "a";
            // Invalid Manufacturer value
            yield return new object[]
            {
                JsonConvert.SerializeObject(defaultRequest)
            };

            defaultRequest = fixture.Create<AddSedanRequest>();
            defaultRequest.Model = "a";
            // Invalid Model value
            yield return new object[]
            {
                JsonConvert.SerializeObject(defaultRequest)
            };

            defaultRequest = fixture.Create<AddSedanRequest>();
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
            var AddSedanRequest = new AddSedanRequest
            {
                Manufacturer = "Toyota",
                Model = "Yaris",
                Year = 2021,
                StartingBid = 10000,
                NumberOfDoors = 5,
                Id = fixture.Create<Guid>()
            };
            request.AddJsonBody(AddSedanRequest);
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
            var AddSedanRequest = new AddSedanRequest
            {
                Manufacturer = "Toyota",
                Model = "Yaris",
                Year = 2050,
                StartingBid = 10000,
                NumberOfDoors = 5,
                Id = fixture.Create<Guid>()
            };
            request.AddJsonBody(AddSedanRequest);
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
            var AddSedanRequest = new AddSedanRequest
            {
                Manufacturer = "Toyota",
                Model = "Yaris",
                Year = 2024,
                StartingBid = 10000,
                NumberOfDoors = 5,
                Id = fixture.Create<Guid>()
            };
            request.AddJsonBody(AddSedanRequest);
            await auctioneerFixture.RestClient.ExecutePostAsync<AddVehicleErrorCode>(request);

            request = new(endpointCreate, Method.Post);
            AddSedanRequest.Manufacturer = "VW";
            AddSedanRequest.Model = "Polo";
            request.AddJsonBody(AddSedanRequest);

            //Act
            var response = await auctioneerFixture.RestClient.ExecutePostAsync<AddVehicleErrorCode>(request);

            //Assert
            response.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            response.Data.Should().Be(AddVehicleErrorCode.DuplicatedVehicle);
        }
    }
}
