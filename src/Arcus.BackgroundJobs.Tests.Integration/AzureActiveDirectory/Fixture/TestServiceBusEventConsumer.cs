using System;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.Tests.Integration.Hosting;
using Arcus.EventGrid.Testing.Infrastructure.Hosts.ServiceBus;
using CloudNative.CloudEvents;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.BackgroundJobs.Tests.Integration.AzureActiveDirectory.Fixture
{
    /// <summary>
    /// Represents an event consumer which receives events from an Azure Service Bus.
    /// </summary>
    public class TestServiceBusEventConsumer : IAsyncDisposable
    {
        private readonly ServiceBusEventConsumerHost _serviceBusEventConsumerHost;

        private TestServiceBusEventConsumer(ServiceBusEventConsumerHost consumerHost)
        {
            _serviceBusEventConsumerHost = consumerHost;
        }

        /// <summary>
        /// Starts an new event consumer which receives events from an Azure Service Bus entity.
        /// </summary>
        /// <param name="configuration">The test configuration to retrieve the Azure Service Bus test infrastructure.</param>
        /// <param name="logger">The logger to write diagnostic messages during consuming the messages.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configuration"/> is <c>null</c>.</exception>
        public static async Task<TestServiceBusEventConsumer> StartNewAsync(TestConfig configuration, ILogger logger)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires a test configuration to retrieve the Azure Service Bus test infrastructure");

            logger = logger ?? NullLogger.Instance;
            
            var topicName = configuration.GetValue<string>("Arcus:Infra:ServiceBus:TopicName");
            var connectionString = configuration.GetValue<string>("Arcus:Infra:ServiceBus:ConnectionString");
            var serviceBusEventConsumerHostOptions = new ServiceBusEventConsumerHostOptions(topicName, connectionString);

            var serviceBusEventConsumerHost = await ServiceBusEventConsumerHost.StartAsync(serviceBusEventConsumerHostOptions, logger);
            return new TestServiceBusEventConsumer(serviceBusEventConsumerHost);
        }

        /// <summary>
        /// Receives an event produced on the Azure Service Bus.
        /// </summary>
        public CloudEvent Consume()
        {
            CloudEvent receivedEvent = _serviceBusEventConsumerHost.GetReceivedEvent(
                data => data.Source == new Uri("https://github.com/arcus-azure/arcus.backgroundjobs"),
                TimeSpan.FromMinutes(8));

            return receivedEvent;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await _serviceBusEventConsumerHost.StopAsync();
        }
    }
}
