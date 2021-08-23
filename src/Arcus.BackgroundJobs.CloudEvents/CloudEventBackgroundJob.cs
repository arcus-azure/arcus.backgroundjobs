using System;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Arcus.BackgroundJobs.CloudEvents
{
    /// <summary>
    /// Representing a Azure Service Bus Topic message pump that will create and delete a Service Bus Topic subscription during the lifetime of the pump.
    /// </summary>
    [Obsolete("Replaced with a dedicated message router " + nameof(CloudEventMessageRouter))]
    public class CloudEventBackgroundJob : AzureServiceBusMessagePump
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEventBackgroundJob"/> class.
        /// </summary>
        /// <param name="settings">The settings to influence the behavior of the message pump.</param>
        /// <param name="configuration">The configuration of the application.</param>
        /// <param name="serviceProvider">The collection of services that are configured.</param>
        /// <param name="messageRouter">The router to route incoming Azure Service Bus messages through registered <see cref="IAzureServiceBusMessageRouter" />s.</param>
        /// <param name="logger">The logger to write telemetry to.</param>
        /// <exception cref="ArgumentNullException">When the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">When the <paramref name="serviceProvider"/> doesn't have a registered <see cref="AzureServiceBusMessagePumpSettings"/> instance.</exception>
        public CloudEventBackgroundJob(
            AzureServiceBusMessagePumpSettings settings,
            IConfiguration configuration,
            IServiceProvider serviceProvider,
            IAzureServiceBusMessageRouter messageRouter,
            ILogger<AzureServiceBusMessagePump> logger) 
            : base(settings, configuration, serviceProvider, messageRouter, logger)
        {
        }
    }
}
