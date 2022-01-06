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
        /// Gets the ID of the current tenant where the Azure resources used in these integration tests are located.
        /// </summary>
        public string GetTenantId()
        {
            const string tenantIdKey = "Arcus:TenantId";
            var tenantId = _config.GetValue<string>(tenantIdKey);
            Guard.For<KeyNotFoundException>(() => tenantId is null, $"Requires a non-blank 'TenantId' at '{tenantIdKey}'");

            return tenantId;
        }

        /// <summary>
        /// Gets the service principal that can authenticate with the Azure Service Bus used in these integration tests.
        /// </summary>
        /// <returns></returns>
        public ServicePrincipal GetServiceBusServicePrincipal()
        {
            var servicePrincipal = new ServicePrincipal(
                clientId: _config.GetValue<string>("Arcus:ServicePrincipal:ApplicationId"),
                clientSecret: _config.GetValue<string>("Arcus:ServicePrincipal:AccessKey"));

            return servicePrincipal;
        }

        /// <summary>
        /// Gets the EventGrid topic URI for the test infrastructure.
        /// </summary>
        public string GetTestInfraEventGridTopicUri()
        {
            var value = _config.GetValue<string>("Arcus:Infra:EventGrid:TopicUri");
            Guard.NotNullOrWhitespace(value, "No non-blank EventGrid topic URI was found for the test infrastructure in the application configuration");

            return value;
        }

        /// <summary>
        /// Gets the EventGrid authentication key for the test infrastructure.
        /// </summary>
        public string GetTestInfraEventGridAuthKey()
        {
            var value = _config.GetValue<string>("Arcus:Infra:EventGrid:AuthKey");
            Guard.NotNullOrWhitespace(value, "No non-blank EventGrid authentication key was found for the test infrastructure in the application configuration");

            return value;
        }

        /// <summary>
        /// Gets all the configuration to run a complete key rotation integration test.
        /// </summary>
        public KeyRotationConfig GetKeyRotationConfig()
        {
            var azureEnv = new ServiceBusNamespace(
                tenantId: _config.GetValue<string>("Arcus:KeyRotation:ServiceBus:TenantId"),
                azureSubscriptionId: _config.GetValue<string>("Arcus:KeyRotation:ServiceBus:SubscriptionId"),
                resourceGroup: _config.GetValue<string>("Arcus:KeyRotation:ServiceBus:ResourceGroupName"),
                @namespace: _config.GetValue<string>("Arcus:KeyRotation:ServiceBus:Namespace"),
                queueName: _config.GetValue<string>("Arcus:KeyRotation:ServiceBus:QueueName"),
                topicName: _config.GetValue<string>("Arcus:KeyRotation:ServiceBus:TopicName"),
                authorizationRuleName: _config.GetValue<string>("Arcus:KeyRotation:ServiceBus:AuthorizationRuleName"));

            var servicePrincipal = new ServicePrincipal(
                clientId: _config.GetValue<string>("Arcus:KeyRotation:ServicePrincipal:ClientId"),
                clientSecret: _config.GetValue<string>("Arcus:KeyRotation:ServicePrincipal:ClientSecret"),
                clientSecretKey: _config.GetValue<string>("Arcus:KeyRotation:ServicePrincipal:ClientSecretKey"));

            var secret = new KeyVaultConfig(
                vaultUri: _config.GetValue<string>("Arcus:KeyRotation:KeyVault:VaultUri"),
                secretName: _config.GetValue<string>("Arcus:KeyRotation:KeyVault:ConnectionStringSecretName"),
                secretNewVersionCreated: new KeyVaultEventEndpoint(
                    _config.GetValue<string>("Arcus:KeyRotation:KeyVault:SecretNewVersionCreated:ServiceBusConnectionStringWithTopicEndpoint")));

            return new KeyRotationConfig(secret, servicePrincipal, azureEnv);
        }

        /// <summary>
        /// Gets the configuration values to connect to the test Azure Active Directory used for integration testing.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when one or more configuration values are missing from the loaded configuration.</exception>
        public AzureActiveDirectoryConfig GetActiveDirectoryConfig()
        {
            return new AzureActiveDirectoryConfig(
                _config.GetValue<string>("Arcus:AzureActiveDirectory:TenantId"),
                _config.GetValue<string>("Arcus:AzureActiveDirectory:ServicePrincipal:ClientId"),
                _config.GetValue<string>("Arcus:AzureActiveDirectory:ServicePrincipal:ClientSecret"));
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
