using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Arcus.BackgroundJobs.Tests.Unit.AppConfiguration
{
    [Trait("Category", "Unit")]
    // ReSharper disable once InconsistentNaming
    public class IServiceCollectionExtensionsTests
    {
        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJobWithoutOptions_WithoutSubscriptionPrefix_Fails(string subscriptionNamePrefix)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoRefreshAppConfigurationBackgroundJob(
                    subscriptionNamePrefix,
                    "<service-bus-topic-connection-string-secret-key>"));
        }
        
        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJobWithoutOptions_WithoutServiceBusTopicConnectionStringSecretKey_Fails(string serviceBusTopicConnectionStringSecretKey)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoRefreshAppConfigurationBackgroundJob(
                    "<subscription-name-prefix>",
                    serviceBusTopicConnectionStringSecretKey));
        }
        
        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJobWithOptions_WithoutSubscriptionPrefix_Fails(string subscriptionNamePrefix)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoRefreshAppConfigurationBackgroundJob(
                    subscriptionNamePrefix,
                    "<service-bus-topic-connection-string-secret-key>",
                    options => { }));
        }
        
        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJobWithOptions_WithoutServiceBusTopicConnectionStringSecretKey_Fails(string serviceBusTopicConnectionStringSecretKey)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoRefreshAppConfigurationBackgroundJob(
                    "<subscription-name-prefix>",
                    serviceBusTopicConnectionStringSecretKey,
                    options => { }));
        }
    }
}
