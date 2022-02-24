using System;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.Tests.Integration.Hosting;
using Azure.Messaging.ServiceBus;
using GuardNet;
using Microsoft.Extensions.Configuration;

namespace Arcus.BackgroundJobs.Tests.Integration.CloudEvents.Fixture
{
    /// <summary>
    /// Represents an event producer which sends events to an Azure Service Bus.
    /// </summary>
    public class TestServiceBusEventProducer
    {
        private readonly string _connectionString;
        
        private TestServiceBusEventProducer(string connectionString)
        {
            Guard.NotNullOrWhitespace(connectionString, nameof(connectionString), "Requires a non-blank Azure Service Bus entity-scoped connection string");
            _connectionString = connectionString;
        }
        
        /// <summary>
        /// Creates an <see cref="TestServiceBusEventProducer"/> instance which sends events to an Azure Service Bus.
        /// </summary>
        /// <param name="configurationKey">The configuration key towards the Azure Service Bus entity-scoped connection string.</param>
        /// <param name="configuration">The test configuration used in this test suite.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="configurationKey" /> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configuration"/> is <c>null</c>.</exception>
        public static TestServiceBusEventProducer Create(string configurationKey, TestConfig configuration)
        {
            Guard.NotNullOrWhitespace(configurationKey, nameof(configurationKey), "Requires a non-blank configuration key to retrieve the Azure Service Bus entity-scoped connection string");
            Guard.NotNull(configuration, nameof(configuration), "Requires a test configuration to retrieve the Azure Service Bus entity-scoped connection string");

            var connectionString = configuration.GetValue<string>(configurationKey);
            TestServiceBusEventProducer producer = Create(connectionString);

            return producer;
        }

        /// <summary>
        /// Creates an <see cref="TestServiceBusEventProducer"/> instance which sends events to an Azure Service Bus.
        /// </summary>
        /// <param name="entityScopedConnectionString">The Azure Service Bus entity-scoped connection string.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="entityScopedConnectionString" /> is blank.</exception>
        public static TestServiceBusEventProducer Create(string entityScopedConnectionString)
        {
            Guard.NotNullOrWhitespace(entityScopedConnectionString, nameof(entityScopedConnectionString), "Requires a non-blank Azure Service Bus entity-scoped connection string");
            return new TestServiceBusEventProducer(entityScopedConnectionString);
        }

        /// <summary>
        /// Sends the <paramref name="message"/> to the configured Azure Service Bus.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> is <c>null</c>.</exception>
        public async Task ProduceAsync(ServiceBusMessage message)
        {
            Guard.NotNull(message, nameof(message), "Requires an Azure Service Bus message to send");

            var connectionStringProperties = ServiceBusConnectionStringProperties.Parse(_connectionString);
            await using (var client = new ServiceBusClient(_connectionString))
            {
                ServiceBusSender messageSender = client.CreateSender(connectionStringProperties.EntityPath);

                try
                {
                    await messageSender.SendMessageAsync(message);
                }
                finally
                {
                    await messageSender.CloseAsync();
                }
            }
        }
    }
}