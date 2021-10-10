using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.AzureAD.ClientSecretExpiration;
using Bogus;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Graph;
using Xunit;

namespace Arcus.BackgroundJobs.Tests.Unit.AzureAD.ClientSecretExpiration
{
    [Trait(name: "Category", value: "Unit")]
    public class ClientSecretExpirationInfoProviderTests
    {
        private static readonly Faker BogusGenerator = new Faker();

        [Fact]
        public void CreateClientSecretExpirationInfoProvider_WithNullGraphServiceClient_Throws()
        {
            // Assert
            Assert.ThrowsAny<ArgumentException>(() => new ClientSecretExpirationInfoProvider(null, NullLogger.Instance));
        }

        [Fact]
        public void CreateClientSecretExpirationInfoProvider_WithNullLogger_Throws()
        {
            // Arrange
            GraphServiceClient client = new GraphServiceClient(
                new DelegateAuthenticationProvider((requestMessage) =>
                {
                    requestMessage
                    .Headers
                    .Authorization = new AuthenticationHeaderValue("bearer", BogusGenerator.Random.ToString());

                    return Task.CompletedTask;
                })
            );

            // Assert
            Assert.ThrowsAny<ArgumentException>(() => new ClientSecretExpirationInfoProvider(client, null));
        }

        [Fact]
        public async Task GetApplicationsWithExpiredAndAboutToExpireSecrets_WithExpirationThresholdLessThanZero_Throws()
        {
            // Arrange
            int expirationThreshold = -1;
            GraphServiceClient client = new GraphServiceClient(
                new DelegateAuthenticationProvider((requestMessage) =>
                {
                    requestMessage
                    .Headers
                    .Authorization = new AuthenticationHeaderValue("bearer", BogusGenerator.Random.ToString());

                    return Task.CompletedTask;
                })
            );
            var provider = new ClientSecretExpirationInfoProvider(client, NullLogger.Instance);

            // Act / Assert
            await Assert.ThrowsAnyAsync<ArgumentException>(() => provider.GetApplicationsWithExpiredAndAboutToExpireSecrets(expirationThreshold));
        }
    }
}
