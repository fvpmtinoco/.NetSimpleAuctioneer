using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;

namespace NetSimpleAuctioneer.IntegrationTests
{
    [CollectionDefinition("AuctioneerClient")]
    public class AuctioneerFixtureCollection : ICollectionFixture<AuctioneerFixture> { }

    public class AuctioneerFixture
    {
        public RestClient RestClient { get; private set; }
        private readonly HttpClient client;

        public AuctioneerFixture()
        {
            WebApplicationFactory<Program> factory = new();
            client = factory.CreateClient();

            RestClient = new RestClient(client, configureSerialization: s => s.UseNewtonsoftJson(new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Include,
                Converters = { new StringEnumConverter() },
                Error = delegate (object? sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
                {
                    if (args.ErrorContext.Error.StackTrace?.Contains("SerializeObject") ?? false)
                    {
                        args.ErrorContext.Handled = true;
                    }
                }
            }));
        }
    }
}
