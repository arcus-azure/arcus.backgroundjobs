using System;
using System.Collections.Generic;
using Arcus.Messaging.Pumps.Abstractions.MessageHandling;
using Arcus.Messaging.Pumps.ServiceBus;
using CloudNative.CloudEvents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Arcus.BackgroundJobs.Tests.Unit.KeyVault
{
    [Trait("Category", "Unit")]
    // ReSharper disable once InconsistentNaming
    public class IServiceCollectionExtensionsTests
    {
        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoRestart_WithoutJobId_Fails(string jobId)
        {
            // Arrange
            var collection = new ServiceCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob(
                    jobId,
                    "subscription-prefix",
                    "service bus topic connection string secret key",
                    "message pump connection string key"));
        }
        
        [Theory]
        [ClassData(typeof(Blanks))]
        public void WithAutoRestart_WithoutSubscriptionPrefix_Fails(string subscriptionPrefix)
        {
            // Arrange
            var collection = new ServiceCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob(
                    "job ID",
                    subscriptionPrefix,
                    "service bus topic connection string secret key",
                    "message pump connection string key"));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void WithAutoRestart_WithoutSubscriptionTopicConnectionStringSecretKey_Fails(
            string serviceBusTopicConnectionStringSecretKey)
        {
            // Arrange
            var collection = new ServiceCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob(
                    "job ID",
                    "subscription-prefix",
                    serviceBusTopicConnectionStringSecretKey,
                    "message pump connection string key"));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void WithAutoRestart_WithoutMessagePumpConnectionStringKey_Fails(string messagePumpConnectionStringKey)
        {
            // Arrange
            var collection = new ServiceCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob(
                    "job ID",
                    "subscription-prefix",
                    "service bus topic connection string secret key",
                    messagePumpConnectionStringKey));
        }

        [Fact]
        public void WithAutoRestart_WithoutRelatedMessagePump_Fails()
        {
            // Arrange
            var collection = new ServiceCollection();
            collection.AddSingleton<IConfiguration>(new ConfigurationRoot(new List<IConfigurationProvider>()));
            collection.AddLogging();
            
            // Act
            collection.AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob(
                "job ID",
                "subscription-prefix",
                "service bus topic connection string secret key",
                "message pump connection string key");
            
            // Assert
            IServiceProvider provider = collection.BuildServiceProvider();
            var exception = Assert.ThrowsAny<InvalidOperationException>(() =>
                provider.GetRequiredService<IMessageHandler<CloudEvent, AzureServiceBusMessageContext>>());
            Assert.Contains("Cannot register re-authentication", exception.Message);
        }
    }
}
