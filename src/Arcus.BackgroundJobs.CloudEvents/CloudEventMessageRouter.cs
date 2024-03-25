using System;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Azure.Messaging;
using GuardNet;
using Microsoft.Extensions.Logging;

namespace Arcus.BackgroundJobs.CloudEvents
{
    /// <summary>
    /// Represents a custom <see cref="IAzureServiceBusMessageRouter"/> that can deserialize incoming CloudEvents messages to valid JSON objects.
    /// </summary>
    public class CloudEventMessageRouter : AzureServiceBusMessageRouter
    {
        /// <inheritdoc />
        public CloudEventMessageRouter(
            IServiceProvider serviceProvider, 
            AzureServiceBusMessageRouterOptions options, 
            ILogger<AzureServiceBusMessageRouter> logger) 
            : base(serviceProvider, options, logger)
        {
        }

        /// <inheritdoc />
        protected override bool TryDeserializeToMessageFormat(string message, Type messageType, out object result)
        {
            Guard.NotNullOrWhitespace(message, nameof (message), "Requires a non-blank raw message to determine whether or not it can be parsed as a CloudEvent message");
            
            try
            {
                if (messageType == typeof(CloudEvent))
                {
                    Logger.LogTrace("Deserialize incoming message as 'CloudEvent'...");
                    CloudEvent cloudEvent = CloudEvent.Parse(BinaryData.FromString(message));
                    Logger.LogTrace("Deserialized incoming message as 'CloudEvent'");

                    result = cloudEvent;
                    return true;
                }
            }
            catch (Exception exception)
            {
                Logger.LogWarning(exception, "Unable to deserialize the CloudEvent due to an exception: {Message}", exception.Message);
            }
            
            return base.TryDeserializeToMessageFormat(message, messageType, out result);
        }
    }
}
