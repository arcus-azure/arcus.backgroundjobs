using System;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.Tests.Integration.CloudEvents.Fixture;
using Arcus.BackgroundJobs.Tests.Integration.Hosting;
using Arcus.EventGrid;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Publishing;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.Testing.Logging;
using Azure.Messaging.ServiceBus;
using CloudNative.CloudEvents;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.BackgroundJobs.Tests.Integration.CloudEvents
{
   [Trait("Category", "Integration")]
    public class CloudEventBackgroundJobTests
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEventBackgroundJobTests" /> class.
        /// </summary>
        public CloudEventBackgroundJobTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }

        [Fact]
        public async Task CloudEventsBackgroundJobOnNamespace_ReceivesCloudEvents_ProcessesCorrectly()
        {
            // Arrange
            var configuration = TestConfig.Create();
            var topicConnectionStringSecretKey = "Arcus:ServiceBus:ConnectionStringWithTopic";
            var namespaceConnectionStringSecretKey = "Arcus:ServiceBus:NamespaceConnectionString";
            var topicConnectionString = configuration.GetValue<string>(topicConnectionStringSecretKey);
            var connectionStringBuilder = ServiceBusConnectionStringProperties.Parse(topicConnectionString);
            IEventGridPublisher eventGridPublisher = CreateEventGridPublisher(configuration);
            
            var options = new WorkerOptions();
            options.Configure(host =>
            {
                host.ConfigureAppConfiguration(context => context.AddConfiguration(configuration))
                    .ConfigureSecretStore((config, stores) => stores.AddConfiguration(config)); 
            });
            options.ConfigureLogging(_logger)
                   .AddSingleton(eventGridPublisher)
                   .AddCloudEventBackgroundJob(
                       topicName: connectionStringBuilder.EntityPath,
                       subscriptionNamePrefix: "Test-", 
                       serviceBusNamespaceConnectionStringSecretKey: namespaceConnectionStringSecretKey)
                   .WithServiceBusMessageHandler<CloudEventToEventGridAzureServiceBusMessageHandler, CloudEvent>();

            CloudEvent expected = CreateCloudEvent();
            ServiceBusMessage message = CreateServiceBusMessageFor(expected);

            await using (var worker = await Worker.StartNewAsync(options))
            {
                var producer = TestServiceBusEventProducer.Create(topicConnectionStringSecretKey, configuration);
                await using (var consumer = await TestServiceBusEventConsumer.StartNewAsync(configuration, _logger))
                {
                    // Act
                    await producer.ProduceAsync(message);
                    
                    // Assert
                    EventBatch<Event> eventBatch = consumer.Consume(expected.Id);
                    Event @event = Assert.Single(eventBatch.Events);
                    CloudEvent actual = @event.AsCloudEvent();
                    Assert.Equal(expected.Id, actual.Id);

                    var expectedData = expected.GetPayload<StorageBlobCreatedEventData>();
                    var actualData = actual.GetPayload<StorageBlobCreatedEventData>();
                    Assert.Equal(expectedData.Api, actualData.Api);
                    Assert.Equal(expectedData.ClientRequestId, actualData.ClientRequestId);
                }
            }
        }

        [Fact]
        public async Task CloudEventsBackgroundJobOnTopic_ReceivesCloudEvents_ProcessesCorrectly()
        {
            // Arrange
            var configuration = TestConfig.Create();
            const string topicConnectionStringSecretKey = "Arcus:ServiceBus:ConnectionStringWithTopic";
            IEventGridPublisher eventGridPublisher = CreateEventGridPublisher(configuration);
            
            var options = new WorkerOptions();
            options.Configure(host =>
                   {
                       host.ConfigureAppConfiguration(context => context.AddConfiguration(configuration))
                           .ConfigureSecretStore((config, stores) => stores.AddConfiguration(config));
                   }).ConfigureLogging(_logger)
                   .AddSingleton(eventGridPublisher)
                   .AddCloudEventBackgroundJob(
                       subscriptionNamePrefix: "Test-",
                       serviceBusTopicConnectionStringSecretKey: topicConnectionStringSecretKey)
                   .WithServiceBusMessageHandler<CloudEventToEventGridAzureServiceBusMessageHandler, CloudEvent>();

            CloudEvent expected = CreateCloudEvent();
            ServiceBusMessage message = CreateServiceBusMessageFor(expected);

            await using (var worker = await Worker.StartNewAsync(options))
            {
                var producer = TestServiceBusEventProducer.Create(topicConnectionStringSecretKey, configuration);
                await using (var consumer = await TestServiceBusEventConsumer.StartNewAsync(configuration, _logger))
                {
                    // Act
                    await producer.ProduceAsync(message);
                    
                    // Assert
                    EventBatch<Event> eventBatch = consumer.Consume(expected.Id);
                    Event @event = Assert.Single(eventBatch.Events);
                    CloudEvent actual = @event.AsCloudEvent();
                    Assert.Equal(expected.Id, actual.Id);

                    var expectedData = expected.GetPayload<StorageBlobCreatedEventData>();
                    var actualData = actual.GetPayload<StorageBlobCreatedEventData>();
                    Assert.Equal(expectedData.Api, actualData.Api);
                    Assert.Equal(expectedData.ClientRequestId, actualData.ClientRequestId);
                }
            }
        }

        private static IEventGridPublisher CreateEventGridPublisher(TestConfig configuration)
        {
            IEventGridPublisher eventGridPublisher = EventGridPublisherBuilder
                .ForTopic(configuration.GetTestInfraEventGridTopicUri())
                .UsingAuthenticationKey(configuration.GetTestInfraEventGridAuthKey())
                .Build();
            
            return eventGridPublisher;
        }

        private static CloudEvent CreateCloudEvent()
        {
            var cloudEvent = new CloudEvent(
                specVersion: CloudEventsSpecVersion.V1_0,
                type: "Microsoft.Storage.BlobCreated",
                source: new Uri(
                    "/subscriptions/{subscription-id}/resourceGroups/{resource-group}/providers/Microsoft.Storage/storageAccounts/{storage-account}#blobServices/default/containers/{storage-container}/blobs/{new-file}",
                    UriKind.Relative),
                id: "173d9985-401e-0075-2497-de268c06ff25",
                time: DateTime.UtcNow)
            {
                Data = new StorageBlobCreatedEventData(
                    api: "PutBlockList",
                    clientRequestId: "6d79dbfb-0e37-4fc4-981f-442c9ca65760",
                    requestId: "831e1650-001e-001b-66ab-eeb76e000000",
                    eTag: "0x8D4BCC2E4835CD0",
                    contentType: "application/octet-stream",
                    contentLength: 524288,
                    blobType: "BlockBlob",
                    url: "https://oc2d2817345i60006.blob.core.windows.net/oc2d2817345i200097container/oc2d2817345i20002296blob",
                    sequencer: "00000000000004420000000000028963",
                    storageDiagnostics: new
                    {
                        batchId = "b68529f3-68cd-4744-baa4-3c0498ec19f0"
                    })
            };
            
            return cloudEvent;
        }

        private static ServiceBusMessage CreateServiceBusMessageFor(CloudEvent cloudEvent)
        {
            var formatter = new JsonEventFormatter();
            byte[] bytes = formatter.EncodeStructuredEvent(cloudEvent, out ContentType contentType);
            var message = new ServiceBusMessage(bytes)
            {
                ApplicationProperties =
                {
                    {"Content-Type", "application/json"},
                    {"Message-Encoding", Encoding.UTF8.WebName}
                }
            };
            
            return message;
        }
    }
}
