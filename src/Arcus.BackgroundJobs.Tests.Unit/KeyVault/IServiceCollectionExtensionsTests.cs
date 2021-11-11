using System;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using Azure.Identity;
using CloudNative.CloudEvents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Arcus.BackgroundJobs.Tests.Unit.KeyVault
{
    [Trait("Category", "Unit")]
    // ReSharper disable once InconsistentNaming
    public class IServiceCollectionExtensionsTests
    {
        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoInvalidateKeyVaultSecretBackgroundJob_WithoutTopisSubscriptionPrefix_Fails(string subscriptionPrefix)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                services.AddAutoInvalidateKeyVaultSecretBackgroundJob(
                    subscriptionPrefix,
                    "<service-bus-topic-connection-string-secret-key>"));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoInvalidateKeyVaultSecretBackgroundJob_WithoutServiceBusTopicConnectionStringSecretKey_Fails(
            string serviceBusTopicConnectionStringSecretKey)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoInvalidateKeyVaultSecretBackgroundJob(
                    "<topic-subscription-prefix>",
                    serviceBusTopicConnectionStringSecretKey));
        }

        [Fact]
        public void AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob_WithoutReferencingMessagePump_Fails()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<IConfiguration>());
            services.AddLogging();
            services.AddSingleton<IHostedService>(serviceProvider =>
            {
                var settings = new AzureServiceBusMessagePumpSettings(
                    "<entity-name>",
                    "<subscription-name>",
                    ServiceBusEntityType.Queue,
                    "<service-bus-namespace>",
                    new DefaultAzureCredential(),
                    new AzureServiceBusMessagePumpOptions {JobId = "No-the-same-job-id"},
                    serviceProvider);
                
                return new AzureServiceBusMessagePump(
                    settings,
                    Mock.Of<IConfiguration>(),
                    serviceProvider,
                    Mock.Of<IAzureServiceBusMessageRouter>(),
                    NullLogger<AzureServiceBusMessagePump>.Instance);
            });
            
            // Act
            services.AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob(
                "<non-existing-job-id>",
                "<topic-subscription-prefix>",
                "<service-bus-topic-connection-string-secret-key>",
                "<message-pump-connection-string-key>");
            
            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.ThrowsAny<InvalidOperationException>(
                () => provider.GetRequiredService<IMessageHandler<CloudEvent, AzureServiceBusMessageContext>>());
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob_WithoutJobId_Fails(string jobId)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                services.AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob(
                    jobId,
                    "<topic-subscription-prefix>",
                    "<service-bus-connection-string-secret-key>",
                    "<message-pump-connection-string-key>"));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob_WithoutTopicSubscriptionPrefix_Fails(string topicSubscriptionPrefix)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                services.AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob(
                    "<job-id>",
                    topicSubscriptionPrefix,
                    "<service-bus-connection-string-secret-key>",
                    "<message-pump-connection-string-key>"));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob_WithoutServiceBusConnectionStringSecretKey_Fails(
            string serviceBusConnectionStringSecretKey)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                services.AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob(
                    "<job-id>",
                    "<topic-subscription-prefix>",
                    serviceBusConnectionStringSecretKey,
                    "<message-pump-connection-string-key>"));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob_WithoutMessagePumpConnectionStringKey_Fails(
            string messagePumpConnectionStringKey)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                services.AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob(
                    "<job-id>",
                    "<topic-subscription-prefix>",
                    "<service-bus-connection-string-secret-key>",
                    messagePumpConnectionStringKey));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJobOptions_WithoutJobId_Fails(string jobId)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                services.AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob(
                    jobId,
                    "<topic-subscription-prefix>",
                    "<service-bus-connection-string-secret-key>",
                    "<message-pump-connection-string-key>",
                    configureBackgroundJob: null));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJobOptions_WithoutTopicSubscriptionPrefix_Fails(string topicSubscriptionPrefix)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                services.AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob(
                    "<job-id>",
                    topicSubscriptionPrefix,
                    "<service-bus-connection-string-secret-key>",
                    "<message-pump-connection-string-key>",
                    configureBackgroundJob: null));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJobOptions_WithoutServiceBusConnectionStringSecretKey_Fails(
            string serviceBusConnectionStringSecretKey)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                services.AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob(
                    "<job-id>",
                    "<topic-subscription-prefix>",
                    serviceBusConnectionStringSecretKey,
                    "<message-pump-connection-string-key>",
                    configureBackgroundJob: null));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJobOptions_WithoutMessagePumpConnectionStringKey_Fails(
            string messagePumpConnectionStringKey)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                services.AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob(
                    "<job-id>",
                    "<topic-subscription-prefix>",
                    "<service-bus-connection-string-secret-key>",
                    messagePumpConnectionStringKey,
                    configureBackgroundJob: null));
        }
    }
}
