using Newtonsoft.Json;

namespace Arcus.BackgroundJobs.Tests.Integration.Fixture.ServiceBus
{
    public class Order
    {
        [JsonProperty]
        public string Id { get; set; }

        [JsonProperty]
        public int Amount { get; set; }

        [JsonProperty]
        public string ArticleNumber { get; set; }

        [JsonProperty]
        public Customer Customer { get; set; }
    }
}
