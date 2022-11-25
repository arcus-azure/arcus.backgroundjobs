using System.Threading;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.Tests.Integration.Fixture.ServiceBus;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using CloudNative.CloudEvents;
using GuardNet;
using Microsoft.Extensions.Logging;

namespace Arcus.BackgroundJobs.Tests.Integration.CloudEvents.Fixture
{
    public class OldCloudEventToEventGridAzureServiceBusMessageHandler : IAzureServiceBusMessageHandler<CloudEvent>
    {
        private readonly IEventGridPublisher _eventGridPublisher;
        private readonly ILogger<OrdersAzureServiceBusMessageHandler> _logger;

        public OldCloudEventToEventGridAzureServiceBusMessageHandler(IEventGridPublisher eventGridPublisher, ILogger<OrdersAzureServiceBusMessageHandler> logger)
        {
            Guard.NotNull(eventGridPublisher, nameof(eventGridPublisher));
            Guard.NotNull(logger, nameof(logger));

            _eventGridPublisher = eventGridPublisher;
            _logger = logger;
        }
        
        /// <summary>
        /// Process a new message that was received.
        /// </summary>
        /// <param name="message">Message that was received</param>
        /// <param name="messageContext">Context providing more information concerning the processing</param>
        /// <param name="correlationInfo">
        ///     Information concerning correlation of telemetry and processes by using a variety of unique
        ///     identifiers
        /// </param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task ProcessMessageAsync(
            CloudEvent message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            Guard.NotNull(message, nameof(message), "Requires an CloudEvent to delegate to Azure Event Grid");
            
            _logger.LogInformation("Processing CloudEvent {Id}", message.Id);

            await _eventGridPublisher.PublishAsync(message);

            _logger.LogInformation("CloudEvent {Id} processed", message.Id);
        }
    }
}
