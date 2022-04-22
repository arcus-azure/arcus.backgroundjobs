using System;
using System.Collections.Generic;
using Arcus.BackgroundJobs.Tests.Integration.Fixture;
using Arcus.BackgroundJobs.Tests.Integration.Hosting;
using Arcus.Security.Core.Caching.Configuration;
using GuardNet;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Extensions on the <see cref="SecretStoreBuilder"/> to add more test dev-friendly registrations.
    /// </summary>
    internal static class SecretStoreBuilderExtensions
    {
        /// <summary>
        /// Adds Azure Key Vault as a secret source which uses client secret authentication.
        /// </summary>
        /// <param name="builder">The builder to create the secret store.</param>
        /// <param name="config">The test configuration instance to retrieve the connection details to interact with the Azure Key Vault via service principal authentication.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> or the <paramref name="config"/> is <c>null</c>.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when one or more configuration values in the test <paramref name="config"/> cannot be found.</exception>
        public static SecretStoreBuilder AddAzureKeyVaultWithServicePrincipal(
            this SecretStoreBuilder builder,
            TestConfig config)
        {
            Guard.NotNull(builder, nameof(builder), "Requires a secret store builder to add the Azure Key Vault registration with service principal authentication");
            Guard.NotNull(config, nameof(config), "Requires a test configuration instance to retrieve the connection details to authenticate with the Azure Key Vault via service principal authentication");

            AzureEnvironment environment = config.GetAzureEnvironment();
            ServicePrincipal servicePrincipal = config.GetServicePrincipal();
            string keyVaultUri = config.GetKeyVaultUri();

            return builder.AddAzureKeyVaultWithServicePrincipal(
                keyVaultUri,
                environment.TenantId,
                servicePrincipal.ClientId,
                servicePrincipal.ClientSecret,
                CacheConfiguration.Default);
        }
    }
}
