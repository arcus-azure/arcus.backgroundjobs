using System;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.Tests.Integration.CloudEvents.Fixture;
using Arcus.BackgroundJobs.Tests.Integration.Fixture;
using Arcus.BackgroundJobs.Tests.Integration.Hosting;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using Arcus.Testing.Logging;
using Azure.Messaging;
using Azure.Messaging.ServiceBus;
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
    [Collection(TestCollections.Integration)]
    public class CloudEventBackgroundJobTests : IAsyncLifetime
    {
        private const string TopicConnectionStringSecretKey = "Arcus:CloudEvents:ServiceBus:ConnectionStringWithTopic",
                             NamespaceConnectionStringSecretKey = "Arcus:CloudEvents:ServiceBus:NamespaceConnectionString",
                             TopicSubscriptionPrefix = "Test-";

        private readonly TestConfig _configuration;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEventBackgroundJobTests" /> class.
        /// </summary>
        public CloudEventBackgroundJobTests(ITestOutputHelper outputWriter)
        {
            _configuration = TestConfig.Create();
            _logger = new XunitTestLogger(outputWriter);
        }

        private TestServiceBusEventConsumer Consumer { get; set; }

        /// <summary>
        /// Called immediately after the class has been created, before it is used.
        /// </summary>
        public async Task InitializeAsync()
        {
            Consumer = await TestServiceBusEventConsumer.StartNewAsync(_configuration, _logger);
        }

        [Fact]
        public async Task CloudEventsBackgroundJobOnNamespace_ReceivesCloudEvents_ProcessesCorrectly()
        {
            // Arrange
            CloudEvent expected = CreateCloudEvent();
            await using Worker worker = await CreateJobOnNamespaceAsync<CloudEventToEventGridAzureServiceBusMessageHandler, CloudEvent>();

            // Act
            await ProduceAsync(expected);

            // Assert
            CloudEvent actual = ConsumeFor(expected);
            AssertCloudEvent(expected, actual);
        }

        [Fact]
        public async Task CloudEventsBackgroundJobOnNamespaceUsingManagedIdentity_ReceivesCloudEvents_ProcessesCorrectly()
        {
            // Arrange
            using var connection = TemporaryManagedIdentityConnection.Create(_configuration);
            
            CloudEvent expected = CreateCloudEvent();
            await using Worker worker = await CreateJobOnNamespaceUsingManagedIdentityAsync<CloudEventToEventGridAzureServiceBusMessageHandler, CloudEvent>();

            // Act
            await ProduceAsync(expected);

            // Assert
            CloudEvent actual = ConsumeFor(expected);
            AssertCloudEvent(expected, actual);
        }

        [Fact]
        public async Task CloudEventsBackgroundJobOnTopic_ReceivesCloudEvents_ProcessesCorrectly()
        {
            // Arrange
            CloudEvent expected = CreateCloudEvent();
            await using Worker worker = await CreateJobOnTopicAsync<CloudEventToEventGridAzureServiceBusMessageHandler, CloudEvent>();
            
            // Act
            await ProduceAsync(expected);

            // Assert
            CloudEvent actual = ConsumeFor(expected);
            AssertCloudEvent(expected, actual);
        }

        private async Task ProduceAsync(CloudEvent @event)
        {
            TestServiceBusEventProducer producer = CreateEventProducer();
            await producer.ProduceAsync(@event);
        }

        private CloudEvent ConsumeFor(CloudEvent @event)
        {
            return Consumer.Consume(@event.Id);
        }

        private TestServiceBusEventProducer CreateEventProducer()
        {
            return TestServiceBusEventProducer.Create(TopicConnectionStringSecretKey, _configuration);
        }

        private Task<Worker> CreateJobOnTopicAsync<TMessageHandler, TMessage>(
            Action<IAzureServiceBusTopicMessagePumpOptions> configureTopicOptions = null,
            string subscriptionPrefix = TopicSubscriptionPrefix) 
            where TMessageHandler : class, IAzureServiceBusMessageHandler<TMessage> 
            where TMessage : class
        {
            WorkerOptions options = 
                CreateDefaultsOptions()
                    .ConfigureServices(services =>
                    {
                        services.AddCloudEventBackgroundJob(
                            subscriptionNamePrefix: subscriptionPrefix,
                                serviceBusTopicConnectionStringSecretKey: TopicConnectionStringSecretKey,
                                opt =>
                                {
                                    opt.TopicSubscription = TopicSubscription.Automatic;
                                    configureTopicOptions?.Invoke(opt);
                                })
                                .WithServiceBusMessageHandler<TMessageHandler, TMessage>();
                    });

            return Worker.StartNewAsync(options);
        }

        private Task<Worker> CreateJobOnNamespaceUsingManagedIdentityAsync<TMessageHandler, TMessage>() 
            where TMessageHandler : class, IAzureServiceBusMessageHandler<TMessage> 
            where TMessage : class
        {
            var topicConnectionString = _configuration.GetValue<string>(TopicConnectionStringSecretKey);
            var properties = ServiceBusConnectionStringProperties.Parse(topicConnectionString);
            
            WorkerOptions options =
                CreateDefaultsOptions()
                    .ConfigureServices(services =>
                    {
                        services.AddCloudEventBackgroundJobUsingManagedIdentity(
                            topicName: properties.EntityPath, 
                            subscriptionNamePrefix: TopicSubscriptionPrefix, 
                            serviceBusNamespace: properties.FullyQualifiedNamespace,
                            configureBackgroundJob: opt => opt.TopicSubscription = TopicSubscription.Automatic)
                                .WithServiceBusMessageHandler<TMessageHandler, TMessage>();
                    });

            return Worker.StartNewAsync(options);
        }

        private Task<Worker> CreateJobOnNamespaceAsync<TMessageHandler, TMessage>(
            Action<IAzureServiceBusTopicMessagePumpOptions> configureTopicOptions = null,
            string subscriptionNamePrefix = TopicSubscriptionPrefix) 
            where TMessageHandler : class, IAzureServiceBusMessageHandler<TMessage> 
            where TMessage : class
        {
            var topicConnectionString = _configuration.GetValue<string>(TopicConnectionStringSecretKey);
            var properties = ServiceBusConnectionStringProperties.Parse(topicConnectionString);

            WorkerOptions options = 
                CreateDefaultsOptions()
                    .ConfigureServices(services =>
                    {
                       services.AddCloudEventBackgroundJob(
                           topicName: properties.EntityPath,
                           subscriptionNamePrefix: subscriptionNamePrefix,
                           serviceBusNamespaceConnectionStringSecretKey: NamespaceConnectionStringSecretKey,
                           opt =>
                           {
                               opt.TopicSubscription = TopicSubscription.Automatic;
                               configureTopicOptions?.Invoke(opt);
                           }).WithServiceBusMessageHandler<TMessageHandler, TMessage>();
                   });

            return Worker.StartNewAsync(options);
        }

        private WorkerOptions CreateDefaultsOptions()
        {
            var options = new WorkerOptions();
            options.Configure(host =>
                   {
                       host.ConfigureAppConfiguration(context => context.AddConfiguration(_configuration))
                           .ConfigureSecretStore((config, stores) => stores.AddConfiguration(config));
                   })
                   .ConfigureLogging(_logger)
                   .ConfigureServices(services => services.AddEventGridPublisher(_configuration));

            return options;
        }

        private static CloudEvent CreateCloudEvent()
        {
            var data = new StorageBlobCreatedEventData(
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
                });

            var cloudEvent = new CloudEvent(
                type: "Microsoft.Storage.BlobCreated",
                source: "/subscriptions/{subscription-id}/resourceGroups/{resource-group}/providers/Microsoft.Storage/storageAccounts/{storage-account}#blobServices/default/containers/{storage-container}/blobs/{new-file}",
                jsonSerializableData: data)
            {
                Id ="173d9985-401e-0075-2497-de268c06ff25", 
                Time = DateTimeOffset.UtcNow,
            };

            return cloudEvent;
        }

        private static void AssertCloudEvent(CloudEvent expected, CloudEvent actual)
        {
            Assert.Equal(expected.Id, actual.Id);
            Assert.NotNull(expected.Data);
            Assert.NotNull(actual.Data);

            var expectedData = expected.Data.ToObjectFromJson<StorageBlobCreatedEventData>();
            var actualData = actual.Data.ToObjectFromJson<StorageBlobCreatedEventData>();
            Assert.Equal(expectedData.Api, actualData.Api);
            Assert.Equal(expectedData.ClientRequestId, actualData.ClientRequestId);
        }

        /// <summary>
        /// Called when an object is no longer needed. Called just before <see cref="M:System.IDisposable.Dispose" />
        /// if the class also implements that.
        /// </summary>
        public async Task DisposeAsync()
        {
            if (Consumer != null)
            {
                await Consumer.DisposeAsync();
            }
        }
    }
}
