using System;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.Tests.Integration.CloudEvents.Fixture;
using Arcus.BackgroundJobs.Tests.Integration.Fixture.ServiceBus;
using Arcus.BackgroundJobs.Tests.Integration.Hosting;
using Arcus.EventGrid;
using Arcus.EventGrid.Contracts;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Testing.Logging;
using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Bogus;
using CloudNative.CloudEvents;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.ServiceBus;
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
        private const string TopicConnectionStringSecretKey = "Arcus:CloudEvents:ServiceBus:ConnectionStringWithTopic",
                             NamespaceConnectionStringSecretKey = "Arcus:CloudEvents:ServiceBus:NamespaceConnectionString";

        private readonly TestConfig _configuration;
        private readonly ILogger _logger;

        private static readonly Faker BogusGenerator = new Faker();
        
        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEventBackgroundJobTests" /> class.
        /// </summary>
        public CloudEventBackgroundJobTests(ITestOutputHelper outputWriter)
        {
            _configuration = TestConfig.Create();
            _logger = new XunitTestLogger(outputWriter);
        }

        [Fact]
        public async Task CloudEventsBackgroundJobOnNamespace_ReceivesCloudEvents_ProcessesCorrectly()
        {
            // Arrange
            var topicConnectionString = _configuration.GetValue<string>(TopicConnectionStringSecretKey);
            var properties = ServiceBusConnectionStringProperties.Parse(topicConnectionString);
            
            var options = new WorkerOptions();
            options.Configure(host =>
            {
                host.ConfigureAppConfiguration(context => context.AddConfiguration(_configuration))
                    .ConfigureSecretStore((config, stores) => stores.AddConfiguration(config)); 
            });
            options.ConfigureLogging(_logger)
                   .ConfigureServices(services =>
                   {
                       services.AddEventGridPublisher(_configuration);
                       services.AddCloudEventBackgroundJob(
                                   topicName: properties.EntityPath,
                                   subscriptionNamePrefix: "Test-",
                                   serviceBusNamespaceConnectionStringSecretKey: NamespaceConnectionStringSecretKey)
                               .WithServiceBusMessageHandler<CloudEventToEventGridAzureServiceBusMessageHandler, CloudEvent>();
                   });

            CloudEvent expected = CreateCloudEvent();
            ServiceBusMessage message = CreateServiceBusMessageFor(expected);

            await using (var worker = await Worker.StartNewAsync(options))
            {
                var producer = TestServiceBusEventProducer.Create(TopicConnectionStringSecretKey, _configuration);
                await using (var consumer = await TestServiceBusEventConsumer.StartNewAsync(_configuration, _logger))
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
        public async Task CloudEventsBackgroundJobOnNamespaceWithIgnoringMissingMembersDeserialization_ReceivesCloudEvents_MessageGetsProcessedByDifferentMessageHandler()
        {
            // Arrange
            var topicConnectionString = _configuration.GetValue<string>(TopicConnectionStringSecretKey);
            var properties = ServiceBusConnectionStringProperties.Parse(topicConnectionString);
            
            var options = new WorkerOptions();
            options.Configure(host =>
            {
                host.ConfigureAppConfiguration(context => context.AddConfiguration(_configuration))
                    .ConfigureSecretStore((config, stores) => stores.AddConfiguration(config)); 
            });
            options.ConfigureLogging(_logger)
                   .ConfigureServices(services =>
                   {
                       services.AddEventGridPublisher(_configuration);
                       services.AddCloudEventBackgroundJob(
                               topicName: properties.EntityPath,
                               subscriptionNamePrefix: "Test-",
                               serviceBusNamespaceConnectionStringSecretKey: NamespaceConnectionStringSecretKey,
                               opt => opt.Deserialization.AdditionalMembers = AdditionalMemberHandling.Ignore)
                           .WithServiceBusMessageHandler<OrdersV2AzureServiceBusMessageHandler, OrderV2>();
                   });

            var operationId = $"operation-{Guid.NewGuid()}";
            OrderV2 order = OrderGenerator.GenerateOrderV2();
            ServiceBusMessage message = order.AsServiceBusMessage(operationId: operationId);

            await using (var worker = await Worker.StartNewAsync(options))
            {
                var producer = TestServiceBusEventProducer.Create(TopicConnectionStringSecretKey, _configuration);
                await using (var consumer = await TestServiceBusEventConsumer.StartNewAsync(_configuration, _logger))
                {
                    // Act
                    await producer.ProduceAsync(message);
                    
                    // Assert
                    EventBatch<Event> eventBatch = consumer.Consume(operationId);
                    Event @event = Assert.Single(eventBatch.Events);

                    var orderCreatedEventData = @event.GetPayload<OrderCreatedEventData>();
                    Assert.NotNull(orderCreatedEventData);
                    Assert.NotNull(orderCreatedEventData.CorrelationInfo);
                    Assert.Equal(order.Id, orderCreatedEventData.Id);
                    Assert.Equal(order.Amount, orderCreatedEventData.Amount);
                    Assert.Equal(order.ArticleNumber, orderCreatedEventData.ArticleNumber);
                    Assert.NotEmpty(orderCreatedEventData.CorrelationInfo.CycleId);
                }
            }
        }
        
        [Fact]
        public async Task CloudEventsBackgroundJobOnNamespace_WithNoneTopicSubscription_DoesntCreateTopicSubscription()
        {
            // Arrange
            string topicConnectionString = _configuration.GetValue<string>(TopicConnectionStringSecretKey);
            var properties = ServiceBusConnectionStringProperties.Parse(topicConnectionString);
            
            var options = new WorkerOptions();
            string subscriptionPrefix = BogusGenerator.Name.Prefix();
            options.ConfigureServices(services => 
            {
                services.AddEventGridPublisher(_configuration);
                services.AddCloudEventBackgroundJob(
                            topicName: properties.EntityPath,
                            subscriptionNamePrefix: subscriptionPrefix,
                            serviceBusNamespaceConnectionStringSecretKey: NamespaceConnectionStringSecretKey,
                            opt => opt.TopicSubscription = TopicSubscription.None)
                        .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();
            });

            // Act
            await using (var worker = await Worker.StartNewAsync(options))
            {
                var client = new ServiceBusAdministrationClient(topicConnectionString);

                SubscriptionProperties subscription = await GetTopicSubscriptionFromPrefix(client, subscriptionPrefix, properties.EntityPath);
                if (subscription != null)
                {
                    await client.DeleteSubscriptionAsync(properties.EntityPath, subscription.SubscriptionName); 
                }
                
                Assert.Null(subscription);
            }
        }

        [Fact]
        public async Task CloudEventsBackgroundJobOnTopic_ReceivesCloudEvents_ProcessesCorrectly()
        {
            // Arrange
            var options = new WorkerOptions();
            options.Configure(host =>
                   {
                       host.ConfigureAppConfiguration(context => context.AddConfiguration(_configuration))
                           .ConfigureSecretStore((config, stores) => stores.AddConfiguration(config));
                   }).ConfigureLogging(_logger)
                   .ConfigureServices(services =>
                   {
                       services.AddEventGridPublisher(_configuration);
                       services.AddCloudEventBackgroundJob(
                               subscriptionNamePrefix: "Test-",
                               serviceBusTopicConnectionStringSecretKey: TopicConnectionStringSecretKey)
                           .WithServiceBusMessageHandler<CloudEventToEventGridAzureServiceBusMessageHandler, CloudEvent>();
                   });

            CloudEvent expected = CreateCloudEvent();
            ServiceBusMessage message = CreateServiceBusMessageFor(expected);

            await using (var worker = await Worker.StartNewAsync(options))
            {
                var producer = TestServiceBusEventProducer.Create(TopicConnectionStringSecretKey, _configuration);
                await using (var consumer = await TestServiceBusEventConsumer.StartNewAsync(_configuration, _logger))
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
        public async Task CloudEventsBackgroundJobWithIgnoringMissingMembersDeserialization_ReceivesCloudEvents_MessageGetsProcessedByDifferentMessageHandler()
        {
            // Arrange
            var options = new WorkerOptions();
            options.Configure(host =>
                   {
                       host.ConfigureAppConfiguration(context => context.AddConfiguration(_configuration))
                           .ConfigureSecretStore((config, stores) => stores.AddConfiguration(config));
                   }).ConfigureLogging(_logger)
                   .ConfigureServices(services =>
                   {
                       services.AddEventGridPublisher(_configuration);
                       services.AddCloudEventBackgroundJob(
                                   subscriptionNamePrefix: "Test-",
                                   serviceBusTopicConnectionStringSecretKey: TopicConnectionStringSecretKey,
                                   opt => opt.Deserialization.AdditionalMembers = AdditionalMemberHandling.Ignore)
                               .WithServiceBusMessageHandler<OrdersV2AzureServiceBusMessageHandler, OrderV2>();
                   });

            var operationId = $"operation-{Guid.NewGuid()}";
            OrderV2 order = OrderGenerator.GenerateOrderV2();
            ServiceBusMessage message = order.AsServiceBusMessage(operationId: operationId);
            
            // Act
            await using (var worker = await Worker.StartNewAsync(options))
            {
                var producer = TestServiceBusEventProducer.Create(TopicConnectionStringSecretKey, _configuration);
                await using (var consumer = await TestServiceBusEventConsumer.StartNewAsync(_configuration, _logger))
                {
                    // Act
                    await producer.ProduceAsync(message);
                    
                    // Assert
                    EventBatch<Event> eventBatch = consumer.Consume(operationId);
                    Event @event = Assert.Single(eventBatch.Events);

                    var orderCreatedEventData = @event.GetPayload<OrderCreatedEventData>();
                    Assert.NotNull(orderCreatedEventData);
                    Assert.NotNull(orderCreatedEventData.CorrelationInfo);
                    Assert.Equal(order.Id, orderCreatedEventData.Id);
                    Assert.Equal(order.Amount, orderCreatedEventData.Amount);
                    Assert.Equal(order.ArticleNumber, orderCreatedEventData.ArticleNumber);
                    Assert.NotEmpty(orderCreatedEventData.CorrelationInfo.CycleId);
                }
            }
        }
        
        [Fact]
        public async Task CloudEventsBackgroundJobOnTopic_WithNoneTopicSubscription_DoesntCreateTopicSubscription()
        {
            // Arrange
            string topicConnectionString = _configuration.GetValue<string>(TopicConnectionStringSecretKey);
            var properties = ServiceBusConnectionStringProperties.Parse(topicConnectionString);
            
            var options = new WorkerOptions();
            string subscriptionPrefix = BogusGenerator.Name.Prefix();
            options.ConfigureServices(services => 
            {
                services.AddEventGridPublisher(_configuration);
                services.AddCloudEventBackgroundJob(
                            subscriptionNamePrefix: subscriptionPrefix,
                            serviceBusTopicConnectionStringSecretKey: TopicConnectionStringSecretKey,
                            opt => opt.TopicSubscription = TopicSubscription.None)
                        .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();
            });

            // Act
            await using (var worker = await Worker.StartNewAsync(options))
            {
                var client = new ServiceBusAdministrationClient(topicConnectionString);

                SubscriptionProperties subscription = await GetTopicSubscriptionFromPrefix(client, subscriptionPrefix, properties.EntityPath);
                if (subscription != null)
                {
                    await client.DeleteSubscriptionAsync(properties.EntityPath, subscription.SubscriptionName); 
                }
                
                Assert.Null(subscription);
            }
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

        private static async Task<SubscriptionProperties> GetTopicSubscriptionFromPrefix(ServiceBusAdministrationClient client, string subscriptionPrefix, string topicName)
        {
            AsyncPageable<SubscriptionProperties> subscriptionsResult = client.GetSubscriptionsAsync(topicName);
                
            await foreach (SubscriptionProperties subscriptionProperties in subscriptionsResult)
            {
                if (subscriptionProperties.SubscriptionName.StartsWith(subscriptionPrefix))
                {
                    return subscriptionProperties;
                }
            }

            return null;
        }
    }
}
