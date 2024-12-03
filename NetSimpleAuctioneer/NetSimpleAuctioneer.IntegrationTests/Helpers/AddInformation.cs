using AutoFixture;
using NetSimpleAuctioneer.API.Features.Auctions.CloseAuction;
using NetSimpleAuctioneer.API.Features.Auctions.StartAuction;
using NetSimpleAuctioneer.API.Features.Vehicles.AddHatchback;
using NetSimpleAuctioneer.API.Features.Vehicles.AddSedan;
using NetSimpleAuctioneer.API.Features.Vehicles.AddSUV;
using NetSimpleAuctioneer.API.Features.Vehicles.AddTruck;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;
using RestSharp;

namespace NetSimpleAuctioneer.IntegrationTests.Helpers
{
    internal class AddInformation(AuctioneerFixture auctioneerFixture)
    {
        private readonly string endpointCreateHatchback = @"/api/vehicles.addHatchback";
        private readonly string endpointCreateSedan = @"/api/vehicles.addSedan";
        private readonly string endpointCreateSUV = @"/api/vehicles.addSUV";
        private readonly string endpointCreateTruck = @"/api/vehicles.addTruck";

        private readonly string endpointStart = @"/api/auctions.startAuction";
        private readonly string endpointClose = @"/api/auctions.closeAuction";

        private readonly Fixture fixture = new();

        internal async Task AddVehicleAsync<T>(T vehicleRequest) where T : AddVehicleRequest
        {
            vehicleRequest.Year = 1980; // Ensure valid year
            RestRequest request = new(GetEndpointForVehicleType<T>(), Method.Post);
            request.AddJsonBody(vehicleRequest);

            await auctioneerFixture.RestClient.ExecutePostAsync(request);
        }

        internal string GetEndpointForVehicleType<T>()
        {
            return typeof(T) switch
            {
                Type _ when typeof(T) == typeof(AddHatchbackRequest) => endpointCreateHatchback,
                Type _ when typeof(T) == typeof(AddSedanRequest) => endpointCreateSedan,
                Type _ when typeof(T) == typeof(AddSUVRequest) => endpointCreateSUV,
                Type _ when typeof(T) == typeof(AddTruckRequest) => endpointCreateTruck,
                _ => null!,
            };
        }

        internal async Task<Guid> StartAuctionAsync()
        {
            var vehicle = fixture.Create<AddSUVRequest>();
            await AddVehicleAsync<AddSUVRequest>(vehicle);

            RestRequest request = new(endpointStart, Method.Post);
            request.AddJsonBody(new StartAuctionRequest { VehicleId = vehicle.Id });

            var response = await auctioneerFixture.RestClient.ExecutePostAsync<StartAuctionResponse>(request);
            return response.Data!.AuctionId;
        }

        internal async Task CloseAuctionAsync(Guid auctionId)
        {
            RestRequest request = new(endpointClose, Method.Post);
            var defaultRequest = new CloseAuctionRequest { AuctionId = auctionId };
            request.AddJsonBody(defaultRequest);

            await auctioneerFixture.RestClient.ExecutePostAsync(request);
        }
    }
}
