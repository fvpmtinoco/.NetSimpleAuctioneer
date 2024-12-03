using AutoFixture;
using FluentAssertions;
using NetSimpleAuctioneer.API.Features.Auctions.CloseAuction;
using NetSimpleAuctioneer.IntegrationTests.Helpers;
using RestSharp;
using System.Net;

namespace NetSimpleAuctioneer.IntegrationTests.Features.Auctions
{
    [Collection("AuctioneerClient")]
    public class CloseAuctionShould(AuctioneerFixture auctioneerFixture)
    {
        private readonly string endpointClose = @"/api/auctions.closeAuction";
        private readonly Fixture fixture = new();
        private readonly AddInformation addInformationHelper = new(auctioneerFixture);

        [Fact]
        public async Task EnsureAuctionCloses()
        {
            //Arrange
            Guid newAuctionId = await addInformationHelper.StartAuctionAsync();

            RestRequest request = new(endpointClose, Method.Post);
            var defaultRequest = new CloseAuctionRequest { AuctionId = newAuctionId };
            request.AddJsonBody(defaultRequest);

            //Act
            var response = await auctioneerFixture.RestClient.ExecutePostAsync(request);

            //Assert
            response.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task EnsureAuctionDoesNotCloseMultipleTimes()
        {
            //Arrange
            Guid newAuctionId = await addInformationHelper.StartAuctionAsync();
            await addInformationHelper.CloseAuctionAsync(newAuctionId);

            RestRequest request = new(endpointClose, Method.Post);
            var defaultRequest = new CloseAuctionRequest { AuctionId = newAuctionId };
            request.AddJsonBody(defaultRequest);

            //Act
            var response = await auctioneerFixture.RestClient.ExecutePostAsync<CloseAuctionErrorCode>(request);

            //Assert
            response.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            response.Data.Should().Be(CloseAuctionErrorCode.AuctionAlreadyClosed);
        }

        [Fact]
        public async Task EnsureInvalidAuctionIsHandled()
        {
            //Arrange
            RestRequest request = new(endpointClose, Method.Post);
            var defaultRequest = new CloseAuctionRequest { AuctionId = fixture.Create<Guid>() };
            request.AddJsonBody(defaultRequest);

            //Act
            var response = await auctioneerFixture.RestClient.ExecutePostAsync<CloseAuctionErrorCode>(request);

            //Assert
            response.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            response.Data.Should().Be(CloseAuctionErrorCode.InvalidAuction);
        }
    }
}
