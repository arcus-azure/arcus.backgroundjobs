using System;
using Arcus.BackgroundJobs.Tests.Integration.Fixture.ServiceBus;
using GuardNet;

namespace Arcus.BackgroundJobs.Tests.Integration.KeyVault.Fixture
{
    /// <summary>
    /// Represents all the configuration values related to testing key rotation.
    /// </summary>
    public class KeyRotationConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyRotationConfig" /> class.
        /// </summary>
        /// <param name="keyVault">The config to represent a Azure Key Vault secret.</param>
        /// <param name="serviceBusNamespace">The config to represent a Azure Service Bus namespace.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="keyVault"/> or <paramref name="serviceBusNamespace"/> is <c>null</c>.
        /// </exception>
        public KeyRotationConfig(KeyVaultConfig keyVault, ServiceBusNamespace serviceBusNamespace)
        {
            Guard.NotNull(keyVault, nameof(keyVault));
            Guard.NotNull(serviceBusNamespace, nameof(serviceBusNamespace));

            KeyVault = keyVault;
            ServiceBusNamespace = serviceBusNamespace;
        }

        /// <summary>
        /// Gets the config representing a Azure Key Vault secret.
        /// </summary>
        public KeyVaultConfig KeyVault { get; }

        /// <summary>
        /// Gets the config to represent a Azure Service Bus Queue.
        /// </summary>
        public ServiceBusNamespace ServiceBusNamespace { get; }
    }
}
