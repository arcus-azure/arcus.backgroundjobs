using System;
using Arcus.BackgroundJobs.Tests.Integration.Fixture.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.ServiceBus;
using Azure.Messaging.EventGrid;
using GuardNet;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Arcus.BackgroundJobs.Tests.Integration.Hosting
{
    /// <summary>
    /// Extensions on the <see cref="WorkerOptions"/> related to test setup.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds an <see cref="EventGridPublisherClient"/> instance to the current <paramref name="services"/>.
        /// </summary>
        /// <param name="services">The available application services to configure a test <see cref="Worker"/>.</param>
        /// <param name="config">The current configuration for the integration test.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or the <paramref name="config"/> is <c>null</c>.</exception>
        public static IServiceCollection AddEventGridPublisher(this IServiceCollection services, TestConfig config)
        {
            Guard.NotNull(services, nameof(services), "Requires a set of worker options to add the Azure EventGrid publisher");
            Guard.NotNull(config, nameof(config), "Requires a test configuration instance to configure the Azure EventGrid publisher");

            string secretName = "EventGridAuthKey";
            var topicEndpointSecret = config.GetRequiredValue<string>("Arcus:Infra:EventGrid:AuthKey");
            services.AddSecretStore(stores => stores.AddInMemory(secretName, topicEndpointSecret));

            services.AddAzureClients(clients =>
            {
                var topicEndpoint = config.GetRequiredValue<string>("Arcus:Infra:EventGrid:TopicUri");
                clients.AddEventGridPublisherClient(topicEndpoint, secretName);
            });

            return services;
        }

        /// <summary>
        /// Adds an <see cref="TestSpyRestartServiceBusMessagePump"/> instance to the current <paramref name="services"/>.
        /// </summary>
        /// <param name="services">The available application services to configure a test <see cref="Worker"/>.</param>
        public static ServiceBusMessageHandlerCollection AddTestSpyServiceBusMessagePump(this IServiceCollection services)
        {
            Guard.NotNull(services, nameof(services));

            string jobId = Guid.NewGuid().ToString();
            services.AddMessagePump(provider =>
            {
                return new TestSpyRestartServiceBusMessagePump(jobId, provider.GetService<ILogger<AzureServiceBusMessagePump>>());
            });
            var collection = new ServiceBusMessageHandlerCollection(services)
            {
                JobId = jobId
            };

            return collection;
        }
    }
}
