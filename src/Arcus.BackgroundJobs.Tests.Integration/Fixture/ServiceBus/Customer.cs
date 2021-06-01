using Newtonsoft.Json;

namespace Arcus.BackgroundJobs.Tests.Integration.Fixture.ServiceBus
{
    public class Customer
    {
        [JsonProperty]
        public string FirstName { get; private set; }

        [JsonProperty]
        public string LastName { get; private set; }
    }
}
