using System.Collections.Generic;
using Arcus.BackgroundJobs.Tests.Integration.AppConfiguration.Fixture;
using Arcus.BackgroundJobs.Tests.Integration.Fixture;
using Arcus.BackgroundJobs.Tests.Integration.Fixture.ServiceBus;
using Arcus.BackgroundJobs.Tests.Integration.KeyVault.Fixture;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using GuardNet;

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
        /// Gets the test configuration to interact with the Azure App Configuration resource.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown when a value in the test configuration cannot be found.</exception>
        public AppTestConfiguration GetAppConfiguration()
        {
            var connectionString = GetRequiredValue<string>("Arcus:AppConfiguration:ConnectionString");
            var serviceBusTopicConnectionString = GetRequiredValue<string>("Arcus:AppConfiguration:ServiceBus:ConnectionStringWithTopic");

            return new AppTestConfiguration(connectionString, serviceBusTopicConnectionString);
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

        private TValue GetRequiredValue<TValue>(string key)
        {
            var value = _config.GetValue<TValue>(key);
            if (value is null)
            {
                throw new KeyNotFoundException($"Could not found configuration key '{key}' in test configuration");
            }

            return value;
        }
    }
}
