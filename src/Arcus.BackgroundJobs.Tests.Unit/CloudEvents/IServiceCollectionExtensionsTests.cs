using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Arcus.BackgroundJobs.Tests.Unit.CloudEvents
{
    // ReSharper disable once InconsistentNaming
    [Trait("Category", "Unit")]
    public class IServiceCollectionExtensionsTests
    {
        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJob_WithoutTopicName_Fails(string topicName)
        {
            // Arrange
            var services = new ServiceCollection();
            var subscriptionPrefix = "Test-";
            var namespaceConnectionStringSecretKey = "Arcus:ServiceBus:NamespaceConnectionString";
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddCloudEventBackgroundJob(topicName, subscriptionPrefix, namespaceConnectionStringSecretKey));
        }
        
        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJob_WithoutSubscriptionPrefixBasedOnNamespace_Fails(string subscriptionPrefix)
        {
            // Arrange
            var services = new ServiceCollection();
            var topicName = "orders";
            var namespaceConnectionStringSecretKey = "Arcus:ServiceBus:NamespaceConnectionString";
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddCloudEventBackgroundJob(topicName, subscriptionPrefix, namespaceConnectionStringSecretKey));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJob_WithoutNamespaceConnectionString_Fails(string namespaceConnectionStringSecretKey)
        {
            // Arrange
            var services = new ServiceCollection();
            var topicName = "orders";
            var subscriptionPrefix = "Test-";
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddCloudEventBackgroundJob(topicName, subscriptionPrefix, namespaceConnectionStringSecretKey));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJob_WithoutSubscriptionPrefixBasedOnTopic_Fails(string subscriptionPrefix)
        {
            // Arrange
            var services = new ServiceCollection();
            var topicConnectionStringSecretKey = "Arcus:ServiceBus:TopicConnectionString";
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddCloudEventBackgroundJob(subscriptionPrefix, topicConnectionStringSecretKey));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJob_WithoutTopicConnectionStringSecretKey_Fails(string topicConnectionStringSecretKey)
        {
            // Arrange
            var services = new ServiceCollection();
            var subscriptionPrefix = "Test-";
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddCloudEventBackgroundJob(subscriptionPrefix, topicConnectionStringSecretKey));
        }
    }
}
