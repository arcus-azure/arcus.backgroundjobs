using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using CloudNative.CloudEvents;
using GuardNet;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Logging;

namespace Arcus.BackgroundJobs.AzureAppConfiguration
{
    /// <summary>
    /// Represents an Azure Service Bus message handler that refreshes Azure App Configuration resources based on received <see cref="CloudEvent"/>s
    /// that notify a change (modified or deleted) in the App Configuration.
    /// </summary>
    public class RefreshAppConfigurationMessageHandler : IAzureServiceBusMessageHandler<CloudEvent>
    {
        private readonly IConfigurationRefresherProvider _appConfigurationRefresherProvider;
        private readonly ILogger<RefreshAppConfigurationMessageHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RefreshAppConfigurationMessageHandler" /> class.
        /// </summary>
        /// <param name="appConfigurationRefresherProvider">
        ///     The instance that provides all <see cref="IConfigurationRefresher"/> instances for Azure App Configuration resources.
        /// </param>
        /// <param name="logger">The logger instance to write information messages during the refreshing of the Azure App Configuration resources.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="appConfigurationRefresherProvider"/> or the <paramref name="logger"/> is <c>null</c>.
        /// </exception>
        public RefreshAppConfigurationMessageHandler(
            IConfigurationRefresherProvider appConfigurationRefresherProvider,
            ILogger<RefreshAppConfigurationMessageHandler> logger)
        {
            Guard.NotNull(appConfigurationRefresherProvider, nameof(appConfigurationRefresherProvider), "Requires an instance that provides Azure App Configuration refreshers");
            Guard.NotNull(logger, nameof(logger), "Requires an logger instance to write information messages during the refreshing of the Azure App Configuration resources");
            
            _appConfigurationRefresherProvider = appConfigurationRefresherProvider;
            _logger = logger;
        }
        
        /// <summary>
        /// Process a new message that was received.
        /// </summary>
        /// <param name="message">The message that was received.</param>
        /// <param name="messageContext">The context providing more information concerning the processing.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The token to cancel the processing.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="message" />, <paramref name="messageContext" />, or the <paramref name="correlationInfo" /> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when no Azure App Configuration event was received or the event didn't contain any Azure App Configuration endpoint reference.
        /// </exception>
        public async Task ProcessMessageAsync(
            CloudEvent message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            Guard.NotNull(message, nameof(message), "Requires an CloudEvent event to refresh the Azure App Configuration values");

            if (message.Type != "Microsoft.AppConfiguration.KeyValueModified" 
                && message.Type != "Microsoft.AppConfiguration.KeyValueDeleted")
            {
                throw new InvalidOperationException("Cannot process CloudEvent event because it's not an Azure App Configuration modified or deleted event");
            }

            if (!Uri.TryCreate(message.Subject, UriKind.Absolute, out Uri subjectUri))
            {
                throw new InvalidOperationException(
                    "Cannot find specific Azure App Configuration resource because the CloudEvent's subject doesn't represents a valid URI");
            }

            IEnumerable<IConfigurationRefresher> refreshers = 
                _appConfigurationRefresherProvider.Refreshers.Where(
                    refresher => refresher.AppConfigurationEndpoint?.IsBaseOf(subjectUri) is true);

            foreach (IConfigurationRefresher refresher in refreshers)
            {
                await RefreshAppConfigurationAsync(refresher);
            }
        }

        private async Task RefreshAppConfigurationAsync(IConfigurationRefresher refresher)
        {
            refresher.SetDirty(TimeSpan.Zero);
            
            bool isSuccessfullyRefreshed = await refresher.TryRefreshAsync();
            if (isSuccessfullyRefreshed)
            {
                _logger.LogInformation("Refreshed Azure App Configuration '{Endpoint}'", refresher.AppConfigurationEndpoint);
            }
            else
            {
                _logger.LogWarning("Could not refresh Azure App Configuration '{Endpoint}' due to an internal problem, could be related to contacting the App Configuration", refresher.AppConfigurationEndpoint);
            }
        }
    }
}
