using System;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.Tests.Integration.Fixture.ServiceBus;
using Arcus.EventGrid;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Parsers;
using Arcus.EventGrid.Testing.Infrastructure.Hosts.ServiceBus;
using Azure.Messaging.ServiceBus;
using Bogus;
using GuardNet;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arcus.BackgroundJobs.Tests.Integration.Hosting.ServiceBus
{
    /// <summary>
    /// Represents a service to interact with the hosted-service.
    /// </summary>
    public class TestMessagePumpService : IAsyncDisposable
    {
        private readonly ILogger _logger;
        private readonly TestConfig _configuration;

        private ServiceBusEventConsumerHost _serviceBusEventConsumerHost;

        private TestMessagePumpService(TestConfig configuration, ILogger logger)
        {
            Guard.NotNull(configuration, nameof(configuration));
            Guard.NotNull(logger, nameof(logger));

            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Starts a new instance of the <see cref="TestMessagePumpService"/> type to simulate messages.
        /// </summary>
        /// <param name="config">The configuration instance to retrieve the Azure Service Bus test infrastructure authentication information.</param>
        /// <param name="logger">The instance to log diagnostic messages during the interaction with teh Azure Service Bus test infrastructure.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="config"/> or the <paramref name="logger"/> is <c>null</c>.</exception>
        public static async Task<TestMessagePumpService> StartNewAsync(
            TestConfig config,
            ILogger logger)
        {
            Guard.NotNull(config, nameof(config));
            Guard.NotNull(logger, nameof(logger));

            var service = new TestMessagePumpService(config, logger);
            await service.StartAsync();

            return service;
        }

        private async Task StartAsync()
        {
            if (_serviceBusEventConsumerHost is null)
            {
                var topicName = _configuration.GetValue<string>("Arcus:Infra:ServiceBus:TopicName");
                var connectionString = _configuration.GetValue<string>("Arcus:Infra:ServiceBus:ConnectionString");
                var serviceBusEventConsumerHostOptions = new ServiceBusEventConsumerHostOptions(topicName, connectionString);

                _serviceBusEventConsumerHost = await ServiceBusEventConsumerHost.StartAsync(serviceBusEventConsumerHostOptions, _logger);
            }
            else
            {
                throw new InvalidOperationException("Service is already started!");
            }
        }

        /// <summary>
        /// Simulate the message processing of the message pump using the Azure Service Bus.
        /// </summary>
        /// <param name="connectionString">The connection string used to send a Azure Service Bus message to the respectively running message pump.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="connectionString"/> is blank.</exception>
        public async Task SimulateMessageProcessingAsync(string connectionString)
        {
            Guard.NotNullOrWhitespace(connectionString, nameof(connectionString));

            if (_serviceBusEventConsumerHost is null)
            {
                throw new InvalidOperationException(
                    "Cannot simulate the message pump because the service is not yet started; please start this service before simulating");
            }

            var operationId = Guid.NewGuid().ToString();
            var transactionId = Guid.NewGuid().ToString();

            Order order = OrderGenerator.GenerateOrder();
            ServiceBusMessage orderMessage = order.AsServiceBusMessage(operationId, transactionId);
            orderMessage.ApplicationProperties["Topic"] = "Orders";
            await SendMessageToServiceBusAsync(connectionString, orderMessage);

            string receivedEvent = _serviceBusEventConsumerHost.GetReceivedEvent(operationId, retryCount: 10);
            Assert.NotEmpty(receivedEvent);

            EventBatch<Event> eventBatch = EventParser.Parse(receivedEvent);
            Assert.NotNull(eventBatch);
            Event orderCreatedEvent = Assert.Single(eventBatch.Events);
            Assert.NotNull(orderCreatedEvent);

            var orderCreatedEventData = orderCreatedEvent.GetPayload<OrderCreatedEventData>();
            Assert.NotNull(orderCreatedEventData);
            Assert.NotNull(orderCreatedEventData.CorrelationInfo);
            Assert.Equal(order.Id, orderCreatedEventData.Id);
            Assert.Equal(order.Amount, orderCreatedEventData.Amount);
            Assert.Equal(order.ArticleNumber, orderCreatedEventData.ArticleNumber);
            Assert.Equal(transactionId, orderCreatedEventData.CorrelationInfo.TransactionId);
            Assert.Equal(operationId, orderCreatedEventData.CorrelationInfo.OperationId);
            Assert.NotEmpty(orderCreatedEventData.CorrelationInfo.CycleId);
        }

        /// <summary>
        /// Sends an Azure Service Bus message to the message pump.
        /// </summary>
        /// <param name="connectionString">The connection string to connect to the service bus.</param>
        /// <param name="message">The message to send.</param>
        public async Task SendMessageToServiceBusAsync(string connectionString, ServiceBusMessage message)
        {
            Guard.NotNullOrWhitespace(connectionString, nameof(connectionString));

            ServiceBusConnectionStringProperties serviceBusConnectionString = ServiceBusConnectionStringProperties.Parse(connectionString);

            await using (var client = new ServiceBusClient(connectionString))
            await using (ServiceBusSender messageSender = client.CreateSender(serviceBusConnectionString.EntityPath))
            {
                await messageSender.SendMessageAsync(message);
            }
        }

        private static ServiceBusReceiver CreateServiceBusReceiver(
            ServiceBusClient client,
            string connectionString,
            string subscriptionName)
        {
            var properties = ServiceBusConnectionStringProperties.Parse(connectionString);
            if (subscriptionName is null)
            {
                return client.CreateReceiver(properties.EntityPath);
            }

            return client.CreateReceiver(properties.EntityPath, subscriptionName);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            if (_serviceBusEventConsumerHost != null)
            {
                await _serviceBusEventConsumerHost.StopAsync();
            }
        }
    }
}
