using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Azure.Messaging.EventGrid;
using GuardNet;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace Arcus.BackgroundJobs.Tests.Integration.CloudEvents.Fixture
{
    public class CloudEventToEventGridAzureServiceBusMessageHandler : IAzureServiceBusMessageHandler<Azure.Messaging.CloudEvent>
    {
        private readonly EventGridPublisherClient _eventGridPublisher;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEventToEventGridAzureServiceBusMessageHandler" /> class.
        /// </summary>
        public CloudEventToEventGridAzureServiceBusMessageHandler(IAzureClientFactory<EventGridPublisherClient> clientFactory, ILogger<CloudEventToEventGridAzureServiceBusMessageHandler> logger)
        {
            Guard.NotNull(clientFactory, nameof(clientFactory));
            Guard.NotNull(logger, nameof(logger));

            _eventGridPublisher = clientFactory.CreateClient("Default");
            _logger = logger;
        }

        /// <summary>
        /// Process a new message that was received.
        /// </summary>
        /// <param name="message">The message that was received.</param>
        /// <param name="messageContext">The context providing more information concerning the processing.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The token to cancel the processing.</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///     Thrown when the <paramref name="message" />, <paramref name="messageContext" />, or the <paramref name="correlationInfo" /> is <c>null</c>.
        /// </exception>
        public async Task ProcessMessageAsync(
            Azure.Messaging.CloudEvent message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            Guard.NotNull(message, nameof(message), "Requires an CloudEvent to delegate to Azure Event Grid");
            
            _logger.LogInformation("Processing CloudEvent {Id}", message.Id);

            await _eventGridPublisher.SendEventAsync(message, cancellationToken);

            _logger.LogInformation("CloudEvent {Id} processed", message.Id);
        }
    }
}