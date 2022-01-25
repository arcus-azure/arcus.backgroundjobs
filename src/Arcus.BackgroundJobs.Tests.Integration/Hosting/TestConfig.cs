using System;
using System.Collections.Generic;
using Arcus.BackgroundJobs.Tests.Integration.AzureActiveDirectory.Fixture;
using Arcus.BackgroundJobs.Tests.Integration.Fixture;
using Arcus.BackgroundJobs.Tests.Integration.Fixture.ServiceBus;
using Arcus.BackgroundJobs.Tests.Integration.KeyVault.Fixture;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Arcus.BackgroundJobs.Tests.Integration.Hosting
{
    /// <summary>
    /// Test representation for the configuration based on the application settings in the integration tests.
    /// </summary>
    public class TestConfig : IConfigurationRoot
    {
        private readonly IConfigurationRoot _config;

        private TestConfig(IConfigurationRoot config)
        {
            _config = config;
        }

        /// <summary>
        /// Creates a test configuration representation of the integration test application settings.
        /// </summary>
        public static TestConfig Create()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.local.json", optional: true)
                .Build();

            return new TestConfig(config);
        }

        /// <summary>
        /// Gets the current Azure environment where the test-related Azure resources are located.
        /// </summary>
        /// <returns></returns>
        public AzureEnvironment GetAzureEnvironment()
        {
            return new AzureEnvironment(
                _config.GetRequiredValue<string>("Arcus:SubscriptionId"),
                _config.GetRequiredValue<string>("Arcus:TenantId"),
                _config.GetRequiredValue<string>("Arcus:ResourceGroupName"));
        }

        /// <summary>
        /// Gets the Arcus service principal to contact the related Azure resources.
        /// </summary>
        public ServicePrincipal GetServicePrincipal()
        {
            return new ServicePrincipal(
                _config.GetRequiredValue<string>("Arcus:ServicePrincipal:ApplicationId"),
                _config.GetRequiredValue<string>("Arcus:ServicePrincipal:AccessKey"));
        }

        /// <summary>
        /// Gets the URI to the Azure Key Vault resource used in these tests.
        /// </summary>
        public string GetKeyVaultUri()
        {
            return _config.GetRequiredValue<string>("Arcus:KeyVault:Uri");
        }

        /// <summary>
        /// Gets all the configuration to run a complete key rotation integration test.
        /// </summary>
        public KeyRotationConfig GetKeyRotationConfig()
        {
            var azureEnv = new ServiceBusNamespace(
                @namespace: _config.GetRequiredValue<string>("Arcus:KeyRotation:ServiceBus:Namespace"),
                queueName: _config.GetRequiredValue<string>("Arcus:KeyRotation:ServiceBus:QueueName"),
                authorizationRuleName: _config.GetRequiredValue<string>("Arcus:KeyRotation:ServiceBus:AuthorizationRuleName"));

            var secret = new KeyVaultConfig(
                vaultUri: GetKeyVaultUri(),
                secretNewVersionCreated: new KeyVaultEventEndpoint(
                    _config.GetRequiredValue<string>("Arcus:KeyVault:SecretNewVersionCreated:ServiceBus:ConnectionStringWithTopic")));

            return new KeyRotationConfig(secret, azureEnv);
        }

        /// <summary>
        /// Gets the configuration values to connect to the test Azure Active Directory used for integration testing.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when one or more configuration values are missing from the loaded configuration.</exception>
        public AzureActiveDirectoryConfig GetActiveDirectoryConfig()
        {
            return new AzureActiveDirectoryConfig(
                _config.GetRequiredValue<string>("Arcus:AzureActiveDirectory:TenantId"),
                _config.GetRequiredValue<string>("Arcus:AzureActiveDirectory:ServicePrincipal:ClientId"),
                _config.GetRequiredValue<string>("Arcus:AzureActiveDirectory:ServicePrincipal:ClientSecret"));
        }

        /// <summary>
        /// Gets the immediate descendant configuration sub-sections.
        /// </summary>
        /// <returns>The configuration sub-sections.</returns>
        public IEnumerable<IConfigurationSection> GetChildren()
        {
            return _config.GetChildren();
        }

        /// <summary>
        /// Returns a <see cref="T:Microsoft.Extensions.Primitives.IChangeToken" /> that can be used to observe when this configuration is reloaded.
        /// </summary>
        /// <returns>A <see cref="T:Microsoft.Extensions.Primitives.IChangeToken" />.</returns>
        public IChangeToken GetReloadToken()
        {
            return _config.GetReloadToken();
        }

        /// <summary>
        /// Gets a configuration sub-section with the specified key.
        /// </summary>
        /// <param name="key">The key of the configuration section.</param>
        /// <returns>The <see cref="T:Microsoft.Extensions.Configuration.IConfigurationSection" />.</returns>
        /// <remarks>
        ///     This method will never return <c>null</c>. If no matching sub-section is found with the specified key,
        ///     an empty <see cref="T:Microsoft.Extensions.Configuration.IConfigurationSection" /> will be returned.
        /// </remarks>
        public IConfigurationSection GetSection(string key)
        {
            return _config.GetSection(key);
        }

        /// <summary>Gets or sets a configuration value.</summary>
        /// <param name="key">The configuration key.</param>
        /// <returns>The configuration value.</returns>
        public string this[string key]
        {
            get => _config[key];
            set => _config[key] = value;
        }

        /// <summary>
        /// Force the configuration values to be reloaded from the underlying <see cref="T:Microsoft.Extensions.Configuration.IConfigurationProvider" />s.
        /// </summary>
        public void Reload()
        {
            _config.Reload();
        }

        /// <summary>
        /// The <see cref="T:Microsoft.Extensions.Configuration.IConfigurationProvider" />s for this configuration.
        /// </summary>
        public IEnumerable<IConfigurationProvider> Providers => _config.Providers;
    }
}
