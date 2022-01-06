using System;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.Tests.Integration.CloudEvents.Fixture;
using Arcus.BackgroundJobs.Tests.Integration.Fixture;
using Arcus.BackgroundJobs.Tests.Integration.Fixture.ServiceBus;
using Arcus.BackgroundJobs.Tests.Integration.Hosting;
using Arcus.BackgroundJobs.Tests.Integration.Hosting.ServiceBus;
using Arcus.EventGrid;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Publishing;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Testing.Logging;
using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Bogus;
using CloudNative.CloudEvents;
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
        private const string TopicConnectionStringSecretKey = "Arcus:ServiceBus:ConnectionStringWithTopic",
                             NamespaceConnectionStringSecretKey = "Arcus:ServiceBus:NamespaceConnectionString";

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
            IEventGridPublisher eventGridPublisher = CreateEventGridPublisher(_configuration);

            var options = new WorkerOptions();
            options.Configure(host =>
            {
                host.ConfigureAppConfiguration(context => context.AddConfiguration(_configuration))
                    .ConfigureSecretStore((config, stores) => stores.AddConfiguration(config));
            });
            options.ConfigureLogging(_logger)
                   .AddSingleton(eventGridPublisher)
                   .AddCloudEventBackgroundJob(
                       topicName: properties.EntityPath,
                       subscriptionNamePrefix: "Test-",
                       serviceBusNamespaceConnectionStringSecretKey: NamespaceConnectionStringSecretKey)
                   .WithServiceBusMessageHandler<CloudEventToEventGridAzureServiceBusMessageHandler, CloudEvent>();

            // Act
            await using (var worker = await Worker.StartNewAsync(options))
            await using (var service = await TestMessagePumpService.StartNewAsync(_configuration, _logger))
            {
                // Assert
                await service.SimulateCloudEventMessageProcessingAsync(topicConnectionString);
            }
        }

        [Fact]
        public async Task CloudEventsBackgroundJobOnNamespaceUsingManagedIdentity_ReceivesCloudEvents_ProcessesCorrectly()
        {
            // Arrange
            var topicConnectionString = _configuration.GetValue<string>(TopicConnectionStringSecretKey);

            var properties = ServiceBusConnectionStringProperties.Parse(topicConnectionString);
            IEventGridPublisher eventGridPublisher = CreateEventGridPublisher(_configuration);
            ServicePrincipal servicePrincipal = _configuration.GetServiceBusServicePrincipal();

            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureTenantId, _configuration.GetTenantId()))
            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureServicePrincipalClientId, servicePrincipal.ClientId))
            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureServicePrincipalClientSecret, servicePrincipal.ClientSecret))
            {
                var options = new WorkerOptions();
                options.Configure(host =>
                {
                    host.ConfigureAppConfiguration(context => context.AddConfiguration(_configuration))
                        .ConfigureSecretStore((config, stores) => stores.AddConfiguration(config));
                });
                options.ConfigureLogging(_logger)
                    .AddSingleton(eventGridPublisher)
                    .AddCloudEventBackgroundJobUsingManagedIdentity(
                        topicName: properties.EntityPath,
                        subscriptionNamePrefix: "Test-",
                        serviceBusNamespace: properties.FullyQualifiedNamespace)
                    .WithServiceBusMessageHandler<CloudEventToEventGridAzureServiceBusMessageHandler, CloudEvent>();

                // Act
                await using (var worker = await Worker.StartNewAsync(options))
                await using (var service = await TestMessagePumpService.StartNewAsync(_configuration, _logger))
                {
                    // Assert
                    await service.SimulateCloudEventMessageProcessingAsync(topicConnectionString);
                }
            }
        }

        [Fact]
        public async Task CloudEventsBackgroundJobOnNamespaceWithIgnoringMissingMembersDeserialization_ReceivesCloudEvents_MessageGetsProcessedByDifferentMessageHandler()
        {
            // Arrange
            var topicConnectionString = _configuration.GetValue<string>(TopicConnectionStringSecretKey);
            var properties = ServiceBusConnectionStringProperties.Parse(topicConnectionString);
            IEventGridPublisher eventGridPublisher = CreateEventGridPublisher(_configuration);

            var options = new WorkerOptions();
            options.Configure(host =>
            {
                host.ConfigureAppConfiguration(context => context.AddConfiguration(_configuration))
                    .ConfigureSecretStore((config, stores) => stores.AddConfiguration(config));
            });
            options.ConfigureLogging(_logger)
                   .AddSingleton(eventGridPublisher)
                   .AddCloudEventBackgroundJob(
                       topicName: properties.EntityPath,
                       subscriptionNamePrefix: "Test-",
                       serviceBusNamespaceConnectionStringSecretKey: NamespaceConnectionStringSecretKey,
                       opt => opt.Deserialization.AdditionalMembers = AdditionalMemberHandling.Ignore)
                   .WithServiceBusMessageHandler<OrdersV2AzureServiceBusMessageHandler, OrderV2>();

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
            IEventGridPublisher eventGridPublisher = CreateEventGridPublisher(_configuration);

            var options = new WorkerOptions();
            string subscriptionPrefix = BogusGenerator.Name.Prefix();
            options.AddSingleton(eventGridPublisher)
                   .AddCloudEventBackgroundJob(
                       topicName: properties.EntityPath,
                       subscriptionNamePrefix: subscriptionPrefix,
                       serviceBusNamespaceConnectionStringSecretKey: NamespaceConnectionStringSecretKey,
                       opt => opt.TopicSubscription = TopicSubscription.None)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();

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
            string topicConnectionString = _configuration.GetValue<string>(TopicConnectionStringSecretKey);
            IEventGridPublisher eventGridPublisher = CreateEventGridPublisher(_configuration);

            var options = new WorkerOptions();
            options.Configure(host =>
                   {
                       host.ConfigureAppConfiguration(context => context.AddConfiguration(_configuration))
                           .ConfigureSecretStore((config, stores) => stores.AddConfiguration(config));
                   }).ConfigureLogging(_logger)
                   .AddSingleton(eventGridPublisher)
                   .AddCloudEventBackgroundJob(
                       subscriptionNamePrefix: "Test-",
                       serviceBusTopicConnectionStringSecretKey: TopicConnectionStringSecretKey)
                   .WithServiceBusMessageHandler<CloudEventToEventGridAzureServiceBusMessageHandler, CloudEvent>();

            // Act
            await using (var worker = await Worker.StartNewAsync(options))
            await using (var service = await TestMessagePumpService.StartNewAsync(_configuration, _logger))
            {
                // Assert
                await service.SimulateCloudEventMessageProcessingAsync(topicConnectionString);
            }
        }

        [Fact]
        public async Task CloudEventsBackgroundJobWithIgnoringMissingMembersDeserialization_ReceivesCloudEvents_MessageGetsProcessedByDifferentMessageHandler()
        {
            // Arrange
            IEventGridPublisher eventGridPublisher = CreateEventGridPublisher(_configuration);

            var options = new WorkerOptions();
            options.Configure(host =>
                   {
                       host.ConfigureAppConfiguration(context => context.AddConfiguration(_configuration))
                           .ConfigureSecretStore((config, stores) => stores.AddConfiguration(config));
                   }).ConfigureLogging(_logger)
                   .AddSingleton(eventGridPublisher)
                   .AddCloudEventBackgroundJob(
                       subscriptionNamePrefix: "Test-",
                       serviceBusTopicConnectionStringSecretKey: TopicConnectionStringSecretKey,
                       opt => opt.Deserialization.AdditionalMembers = AdditionalMemberHandling.Ignore)
                   .WithServiceBusMessageHandler<OrdersV2AzureServiceBusMessageHandler, OrderV2>();

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
            IEventGridPublisher eventGridPublisher = CreateEventGridPublisher(_configuration);

            var options = new WorkerOptions();
            string subscriptionPrefix = BogusGenerator.Name.Prefix();
            options.AddSingleton(eventGridPublisher)
                   .AddCloudEventBackgroundJob(
                       subscriptionNamePrefix: subscriptionPrefix,
                       serviceBusTopicConnectionStringSecretKey: TopicConnectionStringSecretKey,
                       opt => opt.TopicSubscription = TopicSubscription.None)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();

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

        private static IEventGridPublisher CreateEventGridPublisher(TestConfig configuration)
        {
            IEventGridPublisher eventGridPublisher = EventGridPublisherBuilder
                .ForTopic(configuration.GetTestInfraEventGridTopicUri())
                .UsingAuthenticationKey(configuration.GetTestInfraEventGridAuthKey())
                .Build();

            return eventGridPublisher;
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
