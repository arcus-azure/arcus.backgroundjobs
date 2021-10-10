using System;
using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Arcus.BackgroundJobs.Tests.Unit.AzureAD.ClientSecretExpiration
{
    // ReSharper disable once InconsistentNaming
    [Trait("Category", "Unit")]
    public class IServiceCollectionExtensionsTests
    {
        private static readonly Faker BogusGenerator = new Faker();

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJob_WithoutTopicEndpoint_Fails(string topicEndpoint)
        {
            // Arrange
            var services = new ServiceCollection();
            var topicEndpointSecretKey = "Infra.EventGrid.AuthKey";

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddClientSecretExpirationJob(topicEndpoint, topicEndpointSecretKey));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJob_WithoutTopicEndpointSecretKey_Fails(string topicEndpointSecretKey)
        {
            // Arrange
            var services = new ServiceCollection();
            string topicEndpoint = BogusGenerator.Internet.Url().ToString();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddClientSecretExpirationJob(topicEndpoint, topicEndpointSecretKey));
        }
    }
}
