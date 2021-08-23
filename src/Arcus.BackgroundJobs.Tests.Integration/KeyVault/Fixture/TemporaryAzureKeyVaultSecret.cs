using System;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
using GuardNet;

namespace Arcus.BackgroundJobs.Tests.Integration.KeyVault.Fixture 
{
    /// <summary>
    /// Representation of a Azure Key Vault secret with a lifetime the same as the type (dispose type = delete secret).
    /// </summary>
    public class TemporaryAzureKeyVaultSecret : IAsyncDisposable
    {
        private readonly SecretClient _client;

        private TemporaryAzureKeyVaultSecret(SecretClient client, string secretName)
        {
            Guard.NotNull(client, nameof(client));
            Guard.NotNullOrWhitespace(secretName, nameof(secretName));

            _client = client;
            Name = secretName;
        }

        /// <summary>
        /// Gets the name of the KeyVault secret.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Creates a temporary Azure Key Vault secret, deleting when the <see cref="DisposeAsync"/> is called.
        /// </summary>
        /// <param name="client">The client to the vault where the temporary secret should be set.</param>
        public static async Task<TemporaryAzureKeyVaultSecret> CreateNewAsync(SecretClient client)
        {
            Guard.NotNull(client, nameof(client));

            var testSecretName = Guid.NewGuid().ToString("N");
            var testSecretValue = Guid.NewGuid().ToString("N");
            await client.SetSecretAsync(testSecretName, testSecretValue);

            return new TemporaryAzureKeyVaultSecret(client, testSecretName);
        }

        /// <summary>
        /// Updates the temporary secret value.
        /// </summary>
        /// <param name="value">The new secret value.</param>
        public async Task UpdateSecretAsync(string value)
        {
            Guard.NotNullOrWhitespace(value, nameof(value));
            await _client.SetSecretAsync(Name, value);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await _client.StartDeleteSecretAsync(Name);
        }
    }
}
