using System;
using GuardNet;

namespace Arcus.BackgroundJobs.Tests.Integration.KeyVault.Fixture
{
    /// <summary>
    /// Represents a secret inside an Azure Key Vault instance.
    /// </summary>
    public class KeyVaultConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultConfig" /> class.
        /// </summary>
        /// <param name="vaultUri">The URI referencing the Azure Key Vault instance.</param>
        /// <param name="secretNewVersionCreated">The event endpoint of the Azure Key Vault 'Secret new version created' event.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="vaultUri"/> or <paramref name="secretName"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="secretNewVersionCreated"/> is <c>null</c>.</exception>
        public KeyVaultConfig(string vaultUri, KeyVaultEventEndpoint secretNewVersionCreated)
        {
            Guard.NotNullOrWhitespace(vaultUri, nameof(vaultUri));
            Guard.NotNull(secretNewVersionCreated, nameof(secretNewVersionCreated));

            VaultUri = vaultUri;
            SecretNewVersionCreated = secretNewVersionCreated;
        }

        /// <summary>
        /// Gets the URI referencing the Azure Key Vault instance.
        /// </summary>
        public string VaultUri { get; }

        /// <summary>
        /// Gets the endpoint where Azure Key Vault events will be available, including 'Secret new version created' event.
        /// </summary>
        public KeyVaultEventEndpoint SecretNewVersionCreated { get; }
    }
}
