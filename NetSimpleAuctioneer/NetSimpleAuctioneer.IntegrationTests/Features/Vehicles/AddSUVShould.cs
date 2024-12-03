using AutoFixture;
using FluentAssertions;
using NetSimpleAuctioneer.API.Features.Vehicles.AddSUV;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;
using NetSimpleAuctioneer.IntegrationTests.Helpers;
using Newtonsoft.Json;
using RestSharp;
using System.Net;

namespace NetSimpleAuctioneer.IntegrationTests.Features.Vehicles
{
    [Collection("AuctioneerClient")]

    public class AddSUVShould(AuctioneerFixture auctioneerFixture)
    {
        private readonly string endpointCreate = @"/api/vehicles.addSUV";
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
            var defaultRequest = fixture.Create<AddSUVRequest>();

            // No Id in request body
            yield return new object[]
            {
                JsonConvert.SerializeObject(
                    defaultRequest,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new ExcludePropertiesResolver(nameof(AddSUVRequest.Id))
                    })
            };

            // No Manufacturer in request body
            yield return new object[]
            {
                JsonConvert.SerializeObject(
                    defaultRequest,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new ExcludePropertiesResolver(nameof(AddSUVRequest.Manufacturer))
                    })
            };

            // No NumberOfSeats in request body
            yield return new object[]
            {
                JsonConvert.SerializeObject(
                    defaultRequest,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new ExcludePropertiesResolver(nameof(AddSUVRequest.NumberOfSeats))
                    })
            };

            // No Model in request body
            yield return new object[]
            {
                JsonConvert.SerializeObject(
                    defaultRequest,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new ExcludePropertiesResolver(nameof(AddSUVRequest.Model))
                    })
            };

            // No StartingBid in request body
            yield return new object[]
            {
                JsonConvert.SerializeObject(
                    defaultRequest,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new ExcludePropertiesResolver(nameof(AddSUVRequest.StartingBid))
                    })
            };

            // No Year in request body
            yield return new object[]
            {
                JsonConvert.SerializeObject(
                    defaultRequest,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new ExcludePropertiesResolver(nameof(AddSUVRequest.Year))
                    })
            };

            defaultRequest = fixture.Create<AddSUVRequest>();
            defaultRequest.NumberOfSeats = 0;
            // Invalid number of seats
            yield return new object[]
            {
                JsonConvert.SerializeObject(defaultRequest)
            };

            defaultRequest = fixture.Create<AddSUVRequest>();
            defaultRequest.Manufacturer = "a";
            // Invalid Manufacturer value
            yield return new object[]
            {
                JsonConvert.SerializeObject(defaultRequest)
            };

            defaultRequest = fixture.Create<AddSUVRequest>();
            defaultRequest.Model = "a";
            // Invalid Model value
            yield return new object[]
            {
                JsonConvert.SerializeObject(defaultRequest)
            };

            defaultRequest = fixture.Create<AddSUVRequest>();
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
            var AddSUVRequest = new AddSUVRequest
            {
                Manufacturer = "Toyota",
                Model = "Yaris",
                Year = 2021,
                StartingBid = 10000,
                NumberOfSeats = 5,
                Id = fixture.Create<Guid>()
            };
            request.AddJsonBody(AddSUVRequest);
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
            var AddSUVRequest = new AddSUVRequest
            {
                Manufacturer = "Toyota",
                Model = "Yaris",
                Year = 2050,
                StartingBid = 10000,
                NumberOfSeats = 5,
                Id = fixture.Create<Guid>()
            };
            request.AddJsonBody(AddSUVRequest);
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
            var AddSUVRequest = new AddSUVRequest
            {
                Manufacturer = "Toyota",
                Model = "Yaris",
                Year = 2024,
                StartingBid = 10000,
                NumberOfSeats = 5,
                Id = fixture.Create<Guid>()
            };
            request.AddJsonBody(AddSUVRequest);
            await auctioneerFixture.RestClient.ExecutePostAsync<AddVehicleErrorCode>(request);

            request = new(endpointCreate, Method.Post);
            AddSUVRequest.Manufacturer = "VW";
            AddSUVRequest.Model = "Polo";
            request.AddJsonBody(AddSUVRequest);

            //Act
            var response = await auctioneerFixture.RestClient.ExecutePostAsync<AddVehicleErrorCode>(request);

            //Assert
            response.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            response.Data.Should().Be(AddVehicleErrorCode.DuplicatedVehicle);
        }
    }
}
