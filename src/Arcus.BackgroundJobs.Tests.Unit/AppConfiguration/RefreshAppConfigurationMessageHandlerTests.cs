using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.AzureAppConfiguration;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Azure.Messaging;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Arcus.BackgroundJobs.Tests.Unit.AppConfiguration
{
    [Trait("Category", "Unit")]
    public class RefreshAppConfigurationMessageHandlerTests
    {
        [Fact]
        public void Create_WithoutConfigurationRefresherProvider_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => new RefreshAppConfigurationMessageHandler(
                    appConfigurationRefresherProvider: null,
                    logger: NullLogger<RefreshAppConfigurationMessageHandler>.Instance));
        }

        [Fact]
        public void Create_WithoutLogger_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => new RefreshAppConfigurationMessageHandler(
                    appConfigurationRefresherProvider: Mock.Of<IConfigurationRefresherProvider>(),
                    logger: null));
        }

        [Fact]
        public async Task Process_WithoutCloudEvent_Fails()
        {
            // Arrange
            var refresher = new RefreshAppConfigurationMessageHandler(
                Mock.Of<IConfigurationRefresherProvider>(),
                NullLogger<RefreshAppConfigurationMessageHandler>.Instance);

            var context = new AzureServiceBusMessageContext(
                "message-id",
                "job-id",
                AzureServiceBusSystemProperties.CreateFrom(new object().AsServiceBusReceivedMessage()),
                new Dictionary<string, object>());
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");
            
            // Act / Assert
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => refresher.ProcessMessageAsync(
                    message: null,
                    messageContext: context,
                    correlationInfo: correlationInfo,
                    cancellationToken: CancellationToken.None));
        }

        [Theory]
        [InlineData("Microsoft.AppConfiguration.KeyValueModified")]
        [InlineData("Microsoft.AppConfiguration.KeyValueDeleted")]
        public async Task Process_WithAppConfigurationEvents_Succeeds(string eventType)
        {
            // Arrange
            var refreshProviderStub = new Mock<IConfigurationRefresherProvider>();
            refreshProviderStub.Setup(provider => provider.Refreshers)
                           .Returns(Enumerable.Empty<IConfigurationRefresher>());
            
            var refresher = new RefreshAppConfigurationMessageHandler(
                refreshProviderStub.Object,
                NullLogger<RefreshAppConfigurationMessageHandler>.Instance);

            var context = new AzureServiceBusMessageContext(
                "message-id",
                "job-id",
                AzureServiceBusSystemProperties.CreateFrom(new object().AsServiceBusReceivedMessage()),
                new Dictionary<string, object>());
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");

            string appConfigurationEndpoint = "https://some.appconfig.io";
            var cloudEvent = new CloudEvent("http://source", eventType, jsonSerializableData: null)
            {
                Subject = appConfigurationEndpoint,
            };

            // Act / Assert
            await refresher.ProcessMessageAsync(cloudEvent, context, correlationInfo, CancellationToken.None);
        }
        
        [Theory]
        [InlineData("Microsoft.Storage.BlobStorageChanged")]
        [InlineData("Microsoft.KeyVault.SecretVersionChanged")]
        public async Task Process_WithoutAppConfigurationEvents_Fails(string eventType)
        {
            // Arrange
            var refreshProviderStub = new Mock<IConfigurationRefresherProvider>();
            refreshProviderStub.Setup(provider => provider.Refreshers)
                               .Returns(Enumerable.Empty<IConfigurationRefresher>());
            
            var refresher = new RefreshAppConfigurationMessageHandler(
                refreshProviderStub.Object,
                NullLogger<RefreshAppConfigurationMessageHandler>.Instance);

            var context = new AzureServiceBusMessageContext(
                "message-id",
                "job-id",
                AzureServiceBusSystemProperties.CreateFrom(new object().AsServiceBusReceivedMessage()),
                new Dictionary<string, object>());
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");

            string appConfigurationEndpoint = "https://some.appconfig.io";
            var cloudEvent = new CloudEvent("http://source", eventType, jsonSerializableData: null)
            {
                Subject = appConfigurationEndpoint,
            };

            // Act / Assert
            await Assert.ThrowsAnyAsync<InvalidOperationException>(
                () => refresher.ProcessMessageAsync(cloudEvent, context, correlationInfo, CancellationToken.None));
        }

        [Fact]
        public async Task Process_WithoutAppConfigurationEndpointSubjectUri_Fails()
        {
            // Arrange
            var refreshProviderStub = new Mock<IConfigurationRefresherProvider>();
            refreshProviderStub.Setup(provider => provider.Refreshers)
                               .Returns(Enumerable.Empty<IConfigurationRefresher>());
            
            var refresher = new RefreshAppConfigurationMessageHandler(
                refreshProviderStub.Object,
                NullLogger<RefreshAppConfigurationMessageHandler>.Instance);

            var context = new AzureServiceBusMessageContext(
                "message-id",
                "job-id",
                AzureServiceBusSystemProperties.CreateFrom(new object().AsServiceBusReceivedMessage()),
                new Dictionary<string, object>());
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");

            var cloudEvent = new CloudEvent("http://source", "Microsoft.AppConfiguration.KeyValueModified", jsonSerializableData: null)
            {
                Subject = "some-other-value-thats-not-any-uri"
            };

            // Act / Assert
            await Assert.ThrowsAnyAsync<InvalidOperationException>(
                () => refresher.ProcessMessageAsync(cloudEvent, context, correlationInfo, CancellationToken.None));
        }
    }
}
