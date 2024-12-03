using AutoFixture;
using FluentAssertions;
using NetSimpleAuctioneer.API.Features.Auctions.PlaceBid;
using NetSimpleAuctioneer.IntegrationTests.Helpers;
using Newtonsoft.Json;
using RestSharp;
using System.Net;

namespace NetSimpleAuctioneer.IntegrationTests.Features.Auctions
{
    [Collection("AuctioneerClient")]
    public class PlaceBidShould(AuctioneerFixture auctioneerFixture)
    {
        private readonly string endpointBid = @"/api/auctions.placeBid";
        private readonly Fixture fixture = new();

        [Fact]
        public async Task EnsureDataContractIsRespected()
        {
            //Arrange
            RestRequest request = new(endpointBid, Method.Post);
            var defaultRequest = fixture.Create<PlaceBidRequest>();

            var json = JsonConvert.SerializeObject(
                    defaultRequest,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new ExcludePropertiesResolver(nameof(PlaceBidRequest.AuctionId))
                    });

            request.AddJsonBody(json);

            //Act
            var response = await auctioneerFixture.RestClient.ExecutePostAsync(request);

            //Assert
            response.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

    }
}
