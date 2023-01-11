using System;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.Tests.Integration.CloudEvents.Fixture;
using Arcus.BackgroundJobs.Tests.Integration.Fixture;
using Arcus.BackgroundJobs.Tests.Integration.Fixture.ServiceBus;
using Arcus.BackgroundJobs.Tests.Integration.Hosting;
using Arcus.EventGrid;
using Arcus.EventGrid.Contracts;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Testing.Logging;
using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Bogus;
using CloudNative.CloudEvents;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using OldCloudEvent = CloudNative.CloudEvents.CloudEvent;
using CloudEvent = Azure.Messaging.CloudEvent;

namespace Arcus.BackgroundJobs.Tests.Integration.CloudEvents
{
    [Trait("Category", "Integration")]
    [Collection(TestCollections.Integration)]
    public class CloudEventBackgroundJobTests
    {
        private const string TopicConnectionStringSecretKey = "Arcus:CloudEvents:ServiceBus:ConnectionStringWithTopic",
                             NamespaceConnectionStringSecretKey = "Arcus:CloudEvents:ServiceBus:NamespaceConnectionString",
                             TopicEndpointSecretKey = "Arcus:Infra:EventGrid:AuthKey";

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

        private string TopicEndpoint => _configuration.GetRequiredValue<string>("Arcus:Infra:EventGrid:TopicUri");

        [Fact]
        public async Task CloudEventsBackgroundJobOnNamespace_ReceivesCloudEvents_ProcessesCorrectly()
        {
            // Arrange
            var options = new WorkerOptions();
            ConfigureCloudEventsBackgroundJobOnNamespace<CloudEventToEventGridAzureServiceBusMessageHandler, CloudEvent>(options)
                .ConfigureServices(services => services.AddAzureClients(clients => clients.AddEventGridPublisherClient(TopicEndpoint, TopicEndpointSecretKey)));

            CloudEvent expected = CreateCloudEvent();

            await using (var worker = await Worker.StartNewAsync(options))
            {
                TestServiceBusEventProducer producer = CreateEventProducer();
                await using (TestServiceBusEventConsumer consumer = await CreateEventConsumerAsync())
                {
                    // Act
                    await producer.ProduceAsync(expected);

                    // Assert
                    CloudEvent actual = consumer.ConsumeCloudEvent(expected.Id);
                    AssertCloudEvent(expected, actual);
                }
            }
        }

        [Fact]
        public async Task CloudEventsBackgroundJobOnNamespace_ReceivesOldCloudEvents_ProcessesCorrectly()
        {
            // Arrange
            var options = new WorkerOptions();
            ConfigureCloudEventsBackgroundJobOnNamespace<OldCloudEventToEventGridAzureServiceBusMessageHandler, OldCloudEvent>(options)
                .ConfigureServices(services => services.AddEventGridPublisher(_configuration));

            OldCloudEvent expected = CreateOldCloudEvent();

            await using (var worker = await Worker.StartNewAsync(options))
            {
                TestServiceBusEventProducer producer = CreateEventProducer();
                await using (TestServiceBusEventConsumer consumer = await CreateEventConsumerAsync())
                {
                    // Act
                    await producer.ProduceAsync(expected);

                    // Assert
                    EventBatch<Event> eventBatch = consumer.Consume(expected.Id);
                    Event @event = Assert.Single(eventBatch.Events);
                    OldCloudEvent actual = @event.AsCloudEvent();
                    
                    AssertCloudEvent(expected, actual);
                }
            }
        }

        [Fact]
        public async Task CloudEventsBackgroundJobOnNamespaceUsingManagedIdentity_ReceivesCloudEvents_ProcessesCorrectly()
        {
            // Arrange
            ServicePrincipal servicePrincipal = _configuration.GetServicePrincipal();
            AzureEnvironment environment = _configuration.GetAzureEnvironment();
            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureTenantId, environment.TenantId))
            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureServicePrincipalClientId, servicePrincipal.ClientId))
            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureServicePrincipalClientSecret, servicePrincipal.ClientSecret))
            {
                var options = new WorkerOptions();
                ConfigureCloudEventsBackgroundJobOnNamespaceUsingManagedIdentity<CloudEventToEventGridAzureServiceBusMessageHandler, CloudEvent>(options)
                    .ConfigureServices(services => services.AddAzureClients(clients => clients.AddEventGridPublisherClient(TopicEndpoint, TopicEndpointSecretKey)));

                CloudEvent expected = CreateCloudEvent();

                await using (var worker = await Worker.StartNewAsync(options))
                {
                    TestServiceBusEventProducer producer = CreateEventProducer();
                    await using (TestServiceBusEventConsumer consumer = await CreateEventConsumerAsync())
                    {
                        // Act
                        await producer.ProduceAsync(expected);

                        // Assert
                        CloudEvent actual = consumer.ConsumeCloudEvent(expected.Id);
                        AssertCloudEvent(expected, actual);
                    }
                }
            }
        }

        [Fact]
        public async Task CloudEventsBackgroundJobOnNamespaceUsingManagedIdentity_ReceivesOldCloudEvents_ProcessesCorrectly()
        {
            // Arrange
            ServicePrincipal servicePrincipal = _configuration.GetServicePrincipal();
            AzureEnvironment environment = _configuration.GetAzureEnvironment();
            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureTenantId, environment.TenantId))
            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureServicePrincipalClientId, servicePrincipal.ClientId))
            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureServicePrincipalClientSecret, servicePrincipal.ClientSecret))
            {
                var options = new WorkerOptions();
                ConfigureCloudEventsBackgroundJobOnNamespaceUsingManagedIdentity<OldCloudEventToEventGridAzureServiceBusMessageHandler, OldCloudEvent>(options)
                    .ConfigureServices(services => services.AddEventGridPublisher(_configuration));

                OldCloudEvent expected = CreateOldCloudEvent();

                await using (var worker = await Worker.StartNewAsync(options))
                {
                    TestServiceBusEventProducer producer = CreateEventProducer();
                    await using (TestServiceBusEventConsumer consumer = await CreateEventConsumerAsync())
                    {
                        // Act
                        await producer.ProduceAsync(expected);

                        // Assert
                        EventBatch<Event> eventBatch = consumer.Consume(expected.Id);
                        Event @event = Assert.Single(eventBatch.Events);
                        OldCloudEvent actual = @event.AsCloudEvent();
                        AssertCloudEvent(expected, actual);
                    }
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
            ConfigureCloudEventsBackgroundJobOnTopic<CloudEventToEventGridAzureServiceBusMessageHandler, CloudEvent>(options)
                .ConfigureServices(services => services.AddAzureClients(clients => clients.AddEventGridPublisherClient(TopicEndpoint, TopicEndpointSecretKey)));

            CloudEvent expected = CreateCloudEvent();

            await using (var worker = await Worker.StartNewAsync(options))
            {
                TestServiceBusEventProducer producer = CreateEventProducer();
                await using (TestServiceBusEventConsumer consumer = await CreateEventConsumerAsync())
                {
                    // Act
                    await producer.ProduceAsync(expected);

                    // Assert
                    CloudEvent actual = consumer.ConsumeCloudEvent(expected.Id);
                    AssertCloudEvent(expected, actual);
                }
            }
        }

        [Fact]
        public async Task CloudEventsBackgroundJobOnTopic_ReceivesOldCloudEvents_ProcessesCorrectly()
        {
            // Arrange
            var options = new WorkerOptions();
            ConfigureCloudEventsBackgroundJobOnTopic<OldCloudEventToEventGridAzureServiceBusMessageHandler, OldCloudEvent>(options)
                .ConfigureServices(services => services.AddEventGridPublisher(_configuration));

            OldCloudEvent expected = CreateOldCloudEvent();

            await using (var worker = await Worker.StartNewAsync(options))
            {
                TestServiceBusEventProducer producer = CreateEventProducer();
                await using (TestServiceBusEventConsumer consumer = await CreateEventConsumerAsync())
                {
                    // Act
                    await producer.ProduceAsync(expected);

                    // Assert
                    EventBatch<Event> eventBatch = consumer.Consume(expected.Id);
                    Event @event = Assert.Single(eventBatch.Events);
                    OldCloudEvent actual = @event.AsCloudEvent();
                    AssertCloudEvent(expected, actual);
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

        private async Task<TestServiceBusEventConsumer> CreateEventConsumerAsync()
        {
            return await TestServiceBusEventConsumer.StartNewAsync(_configuration, _logger);
        }

        private TestServiceBusEventProducer CreateEventProducer()
        {
            return TestServiceBusEventProducer.Create(TopicConnectionStringSecretKey, _configuration);
        }

        private static void AssertCloudEvent(CloudEvent expected, CloudEvent actual)
        {
            Assert.Equal(expected.Id, actual.Id);

            var expectedData = expected.Data.ToObjectFromJson<StorageBlobCreatedEventData>();
            var actualData = actual.Data.ToObjectFromJson<StorageBlobCreatedEventData>();
            Assert.Equal(expectedData.Api, actualData.Api);
            Assert.Equal(expectedData.ClientRequestId, actualData.ClientRequestId);
        }

        private static void AssertCloudEvent(OldCloudEvent expected, OldCloudEvent actual)
        {
            Assert.Equal(expected.Id, actual.Id);

            var expectedData = expected.GetPayload<StorageBlobCreatedEventData>();
            var actualData = actual.GetPayload<StorageBlobCreatedEventData>();
            Assert.Equal(expectedData.Api, actualData.Api);
            Assert.Equal(expectedData.ClientRequestId, actualData.ClientRequestId);
        }

        private WorkerOptions ConfigureCloudEventsBackgroundJobOnTopic<TMessageHandler, TMessage>(WorkerOptions options) 
            where TMessageHandler : class, IAzureServiceBusMessageHandler<TMessage> 
            where TMessage : class
        {
            ConfigureSecretStoreWithConfiguration(options);
            options.ConfigureLogging(_logger)
                   .ConfigureServices(services =>
                   {
                       services.AddCloudEventBackgroundJob(
                                   subscriptionNamePrefix: "Test-",
                                   serviceBusTopicConnectionStringSecretKey: TopicConnectionStringSecretKey)
                               .WithServiceBusMessageHandler<TMessageHandler, TMessage>();
                   });

            return options;
        }

        private WorkerOptions ConfigureCloudEventsBackgroundJobOnNamespaceUsingManagedIdentity<TMessageHandler, TMessage>(WorkerOptions options) 
            where TMessageHandler : class, IAzureServiceBusMessageHandler<TMessage> 
            where TMessage : class
        {
            var topicConnectionString = _configuration.GetValue<string>(TopicConnectionStringSecretKey);
            var properties = ServiceBusConnectionStringProperties.Parse(topicConnectionString);
            
            ConfigureSecretStoreWithConfiguration(options);
            options.ConfigureLogging(_logger)
                   .ConfigureServices(services =>
                   {
                       services.AddCloudEventBackgroundJobUsingManagedIdentity(
                                   topicName: properties.EntityPath, 
                                   subscriptionNamePrefix: "Test-", 
                                   serviceBusNamespace: properties.FullyQualifiedNamespace)
                               .WithServiceBusMessageHandler<TMessageHandler, TMessage>();
                   });

            return options;
        }

        private WorkerOptions ConfigureCloudEventsBackgroundJobOnNamespace<TMessageHandler, TMessage>(WorkerOptions options) 
            where TMessageHandler : class, IAzureServiceBusMessageHandler<TMessage> 
            where TMessage : class
        {
            var topicConnectionString = _configuration.GetValue<string>(TopicConnectionStringSecretKey);
            var properties = ServiceBusConnectionStringProperties.Parse(topicConnectionString);
            
            ConfigureSecretStoreWithConfiguration(options);
            options.ConfigureLogging(_logger)
                   .ConfigureServices(services =>
                   {
                       services.AddCloudEventBackgroundJob(
                                   topicName: properties.EntityPath,
                                   subscriptionNamePrefix: "Test-",
                                   serviceBusNamespaceConnectionStringSecretKey: NamespaceConnectionStringSecretKey)
                               .WithServiceBusMessageHandler<TMessageHandler, TMessage>();
                   });

            return options;
        }

        private void ConfigureSecretStoreWithConfiguration(WorkerOptions options)
        {
            options.Configure(host =>
            {
                host.ConfigureAppConfiguration(context => context.AddConfiguration(_configuration))
                    .ConfigureSecretStore((config, stores) => stores.AddConfiguration(config));
            });
        }

        private static OldCloudEvent CreateOldCloudEvent()
        {
            var cloudEvent = new OldCloudEvent(
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
