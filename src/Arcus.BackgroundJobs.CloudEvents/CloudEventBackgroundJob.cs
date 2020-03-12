using System;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using CloudNative.CloudEvents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Arcus.BackgroundJobs.CloudEvents
{
    /// <summary>
    /// Representing a Azure Service Bus Topic message pump that will create and delete a Service Bus Topic subscription during the lifetime of the pump.
    /// </summary>
    public class CloudEventBackgroundJob : AzureServiceBusMessagePump
    {
        private static readonly JsonEventFormatter JsonEventFormatter = new JsonEventFormatter();

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEventBackgroundJob"/> class.
        /// </summary>
        /// <param name="settings">The settings to influence the behavior of the message pump.</param>
        /// <param name="configuration">The configuration of the application.</param>
        /// <param name="serviceProvider">The collection of services that are configured.</param>
        /// <param name="logger">The logger to write telemetry to.</param>
        /// <exception cref="ArgumentNullException">When the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">When the <paramref name="serviceProvider"/> doesn't have a registered <see cref="AzureServiceBusMessagePumpSettings"/> instance.</exception>
        public CloudEventBackgroundJob(
            AzureServiceBusMessagePumpSettings settings,
            IConfiguration configuration,
            IServiceProvider serviceProvider,
            ILogger<AzureServiceBusMessagePump> logger) 
            : base(settings, configuration, serviceProvider, logger)
        {
        }

        /// <summary>
        /// Tries to parse the given raw <paramref name="message" /> to the contract of the <see cref="T:Arcus.Messaging.Pumps.Abstractions.MessageHandling.IMessageHandler`2" />.
        /// </summary>
        /// <param name="message">The raw incoming message that will be tried to parse against the <see cref="T:Arcus.Messaging.Pumps.Abstractions.MessageHandling.IMessageHandler`2" />'s message contract.</param>
        /// <param name="messageType">The type of the message that the message handler can process.</param>
        /// <param name="result">The resulted parsed message when the <paramref name="message" /> conforms with the message handlers' contract.</param>
        /// <returns>
        ///     [true] if the <paramref name="message" /> conforms the <see cref="T:Arcus.Messaging.Pumps.Abstractions.MessageHandling.IMessageHandler`2" />'s contract; otherwise [false].
        /// </returns>
        public override bool TryDeserializeToMessageFormat(string message, Type messageType, out object result)
        {
            try
            {
                if (messageType == typeof(CloudEvent))
                {
                    CloudEvent cloudEvent = JsonEventFormatter.DecodeStructuredEvent(DefaultEncoding.GetBytes(message));
                    
                    result = cloudEvent; 
                    return true;
                }
            }
            catch (Exception exception)
            {
                Logger.LogWarning(exception, "Unable to deserialize the CloudEvent");
            }

            result = null;
            return false;
        }
    }
}
