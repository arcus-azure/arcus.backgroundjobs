using System.Threading;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.KeyVault.Events;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Security.Core.Caching;
using CloudNative.CloudEvents;
using GuardNet;
using Microsoft.Extensions.Logging;
using Microsoft.Rest.Azure;

namespace Arcus.BackgroundJobs.KeyVault
{
    /// <summary>
    /// Message pump implementation to automatically invalidate Azure Key Vault secrets based on the <see cref="SecretNewVersionCreated"/> emitted event.
    /// </summary>
    public class InvalidateKeyVaultSecretHandler : IAzureServiceBusMessageHandler<CloudEvent>
    {
        private readonly ICachedSecretProvider _cachedSecretProvider;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidateKeyVaultSecretHandler"/> class.
        /// </summary>
        public InvalidateKeyVaultSecretHandler(ICachedSecretProvider secretProvider, ILogger<InvalidateKeyVaultSecretHandler> logger)
        {
            Guard.NotNull(secretProvider, nameof(secretProvider));
            Guard.NotNull(logger, nameof(logger));

            _cachedSecretProvider = secretProvider;
            _logger = logger;
        }

        /// <summary>
        /// Process a new message that was received
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
            Guard.NotNull(message, nameof(message), "Cannot invalidate Azure KeyVault secret from a 'null' CloudEvent");

            var secretNewVersionCreated = message.GetPayload<SecretNewVersionCreated>();
            if (secretNewVersionCreated is null)
            {
                throw new CloudException(
                    "Azure Key Vault job cannot map Event Grid event to CloudEvent because the event data isn't recognized as a 'SecretNewVersionCreated' schema");
            }

            await _cachedSecretProvider.InvalidateSecretAsync(secretNewVersionCreated.ObjectName);
            _logger.LogInformation("Invalidated Azure Key Vault '{SecretName}' secret in vault '{VaultName}'", secretNewVersionCreated.ObjectName, secretNewVersionCreated.VaultName);
        }
    }
}
