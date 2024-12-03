using AutoFixture;
using FluentAssertions;
using NetSimpleAuctioneer.API.Features.Auctions.StartAuction;
using NetSimpleAuctioneer.API.Features.Vehicles.AddHatchback;
using NetSimpleAuctioneer.IntegrationTests.Helpers;
using Newtonsoft.Json;
using RestSharp;
using System.Net;

namespace NetSimpleAuctioneer.IntegrationTests.Features.Auctions
{
    [Collection("AuctioneerClient")]

    public class StartAuctionShould(AuctioneerFixture auctioneerFixture)
    {
        private readonly string endpointStart = @"/api/auctions.startAuction";
        private readonly Fixture fixture = new();
        private readonly AddInformation addVehicleHelper = new(auctioneerFixture);

        [Fact]
        public async Task EnsureDataContractIsRespected()
        {
            //Arrange
            RestRequest request = new(endpointStart, Method.Post);
            var defaultRequest = fixture.Create<StartAuctionRequest>();

            var json = JsonConvert.SerializeObject(
                    defaultRequest,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new ExcludePropertiesResolver(nameof(StartAuctionRequest.VehicleId))
                    });

            request.AddJsonBody(json);

            //Act
            var response = await auctioneerFixture.RestClient.ExecutePostAsync(request);

            //Assert
            response.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task EnsureAuctionStarts()
        {
            //Arrange
            AddHatchbackRequest addRequest = fixture.Create<AddHatchbackRequest>();
            await addVehicleHelper.AddVehicleAsync<AddHatchbackRequest>(addRequest);

            RestRequest request = new(endpointStart, Method.Post);
            var defaultRequest = new StartAuctionRequest { VehicleId = addRequest.Id };
            request.AddJsonBody(defaultRequest);

            //Act
            var response = await auctioneerFixture.RestClient.ExecutePostAsync<StartAuctionResponse>(request);

            //Assert
            response.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data!.AuctionId.Should().NotBeEmpty();
        }

        [Fact]
        public async Task EnsureAuctionDoesNotStartForVehicleAlreadyOnOtherAuction()
        {
            //Arrange
            AddHatchbackRequest addRequest = fixture.Create<AddHatchbackRequest>();
            await addVehicleHelper.AddVehicleAsync<AddHatchbackRequest>(addRequest);

            RestRequest request = new(endpointStart, Method.Post);
            var defaultRequest = new StartAuctionRequest { VehicleId = addRequest.Id };
            request.AddJsonBody(defaultRequest);
            //Start auction for created vehicle
            await auctioneerFixture.RestClient.ExecutePostAsync<StartAuctionResponse>(request);

            //Act
            var response = await auctioneerFixture.RestClient.ExecutePostAsync<StartAuctionErrorCode>(request);

            //Assert
            response.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            response.Data.Should().Be(StartAuctionErrorCode.AuctionForVehicleAlreadyActive);
        }

        [Fact]
        public async Task EnsureAuctionDoesNotStartForInvalidVehicle()
        {
            //Arrange
            RestRequest request = new(endpointStart, Method.Post);
            var defaultRequest = new StartAuctionRequest { VehicleId = fixture.Create<Guid>() };
            request.AddJsonBody(defaultRequest);

            //Act
            var response = await auctioneerFixture.RestClient.ExecutePostAsync<StartAuctionErrorCode>(request);

            //Assert
            response.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            response.Data.Should().Be(StartAuctionErrorCode.InvalidVehicle);
        }
    }
}
