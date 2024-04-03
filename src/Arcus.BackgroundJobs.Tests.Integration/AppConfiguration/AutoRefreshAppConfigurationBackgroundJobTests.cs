using System;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.Tests.Integration.AppConfiguration.Fixture;
using Arcus.BackgroundJobs.Tests.Integration.Fixture;
using Arcus.BackgroundJobs.Tests.Integration.Hosting;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Testing.Logging;
using Azure;
using Azure.Data.AppConfiguration;
using Azure.Messaging.ServiceBus;
using Bogus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Polly;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Arcus.BackgroundJobs.Tests.Integration.AppConfiguration
{
    public enum AuthType { ConnectionString, ManagedIdentity }

    [Trait("Category", "Integration")]
    [Collection(TestCollections.Integration)]
    public class AutoRefreshAppConfigurationBackgroundJobTests
    {
        private readonly ILogger _logger;
        private readonly TestConfig _testConfiguration;
        
        private const string ServiceBusTopicConnectionStringSecretKey = "Arcus.AppConfiguration.ServiceBusConnectionStringWithKey",
                             TopicSubscriptionNamePrefix = "TestSub";

        private static readonly Faker Bogus = new();
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AutoRefreshAppConfigurationBackgroundJobTests" /> class.
        /// </summary>
        public AutoRefreshAppConfigurationBackgroundJobTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
            _testConfiguration = TestConfig.Create();
            TopicConnectionString = ServiceBusConnectionStringProperties.Parse(AppConfig.ServiceBusTopicConnectionString);
        }

        private AppTestConfiguration AppConfig => _testConfiguration.GetAppConfiguration();
        private ConfigurationClient AppConfigClient => new(AppConfig.ConnectionString);
        private ServiceBusConnectionStringProperties TopicConnectionString { get; }
        private string ConfigKey { get; } = $"Key-{Bogus.Lorem.Word()}";
        private string ConfigValue { get; } = $"Value-{Bogus.Lorem.Word()}";
        private string ConfigUpdatedValue { get; } = $"Updated-{Bogus.Lorem.Word()}";

        [Theory]
        [InlineData(AuthType.ConnectionString)]
        [InlineData(AuthType.ManagedIdentity)]
        public async Task AutoAppConfigurationRefresh_WithModifiedKeyValue_UpdatesConfiguration(AuthType authentication)
        {
            // Arrange
            await SetConfigSettingAsync(ConfigKey, ConfigValue);

            using IDisposable connection = EnsureRemoteAuthenticationForType(authentication);
            await using Worker worker = await CreateJobOnKeyAsync(ConfigKey, authentication);
            
            Assert.Equal(ConfigValue, GetConfigValue(worker, ConfigKey));

            // Act
            await SetConfigSettingAsync(ConfigKey, ConfigUpdatedValue);

            // Assert
            RetryAssertion(() => Assert.Equal(ConfigUpdatedValue, GetConfigValue(worker, ConfigKey)));
        }

        [Theory]
        [InlineData(AuthType.ConnectionString)]
        [InlineData(AuthType.ManagedIdentity)]
        public async Task AutoAppConfigurationRefresh_WithDeletedKeyValue_UpdatesConfiguration(AuthType authentication)
        {
            // Arrange
            await SetConfigSettingAsync(ConfigKey, ConfigValue);

            using IDisposable connection = EnsureRemoteAuthenticationForType(authentication);
            await using Worker worker = await CreateJobOnKeyAsync(ConfigKey, authentication);
            
            Assert.Equal(ConfigValue, GetConfigValue(worker, ConfigKey));

            // Act
            await DeleteConfigSettingAsync(ConfigKey);
                
            // Assert
            RetryAssertion(() => Assert.Null(GetConfigValue(worker, ConfigKey)));
        }

        private string GetConfigValue(Worker worker, string configKey)
        {
            _logger.LogTrace("Get local app configuration key '{Key}'", configKey);

            var config = worker.ServiceProvider.GetRequiredService<IConfiguration>();
            return config.GetValue<string>(configKey);
        }

        [Theory]
        [InlineData(AuthType.ConnectionString)]
        [InlineData(AuthType.ManagedIdentity)]
        public async Task AutoAppConfigurationRefresh_WithModifiedFeatureToggle_UpdatesConfiguration(AuthType authentication)
        {
            // Arrange
            await EnableConfigSettingAsync(ConfigKey);

            using IDisposable connection = EnsureRemoteAuthenticationForType(authentication);
            await using Worker worker = await CreateJobOnKeyAsync(ConfigKey, authentication);

            await EnsureFeatureManagerEnabledAsync(worker, ConfigKey);

            // Act
            await DisableConfigSettingAsync(ConfigKey);

            // Assert
            await RetryAssertionAsync(async () =>
            {
                await EnsureFeatureManagerEnabledAsync(worker, ConfigKey);
            });
        }

        [Theory]
        [InlineData(AuthType.ConnectionString)]
        [InlineData(AuthType.ManagedIdentity)]
        public async Task AutoAppConfigurationRefresh_WithDeletedFeatureToggle_UpdatesConfiguration(AuthType authentication)
        {
            // Arrange
            await EnableConfigSettingAsync(ConfigKey);

            using IDisposable connection = EnsureRemoteAuthenticationForType(authentication);
            await using Worker worker = await CreateJobOnKeyAsync(ConfigKey, authentication);

            await EnsureFeatureManagerEnabledAsync(worker, ConfigKey);

            // Act
            await DeleteConfigSettingAsync(ConfigKey);

            // Assert
            await RetryAssertionAsync(async () =>
            {
                await EnsureFeatureManagerEnabledAsync(worker, ConfigKey);
            });
        }

        private IDisposable EnsureRemoteAuthenticationForType(AuthType type)
        {
            if (type is AuthType.ManagedIdentity)
            {
                return TemporaryManagedIdentityConnection.Create(_testConfiguration);
            }

            return null;
        }

        private async Task<Worker> CreateJobOnKeyAsync(string key)
        {
            WorkerOptions options = CreateDefaultOptionsOnKey(key);
            options.ConfigureServices(services =>
            {
                services.AddSecretStore(stores => stores.AddInMemory(ServiceBusTopicConnectionStringSecretKey, AppConfig.ServiceBusTopicConnectionString))
                        .AddAutoRefreshAppConfigurationBackgroundJob(
                            TopicSubscriptionNamePrefix,
                            ServiceBusTopicConnectionStringSecretKey,
                            opt =>
                            {
                                opt.TopicSubscription = TopicSubscription.Automatic;
                            });
            });

            return await Worker.StartNewAsync(options);
        }

        private Task<Worker> CreateJobOnKeyAsync(string key, AuthType authentication)
        {
            return authentication switch
            {
                AuthType.ConnectionString => CreateJobOnKeyAsync(key),
                AuthType.ManagedIdentity => CreateJobUsingManagedIdentityOnKeyAsync(key),
                _ => throw new ArgumentOutOfRangeException(nameof(authentication), authentication, "Unknown authentication type")
            };
        }

        private async Task<Worker> CreateJobUsingManagedIdentityOnKeyAsync(string key)
        {
            WorkerOptions options = CreateDefaultOptionsOnKey(key);
            options.ConfigureServices(services =>
            {
                services.AddAutoRefreshAppConfigurationBackgroundJobUsingManagedIdentity(
                    TopicConnectionString.EntityPath,
                    TopicSubscriptionNamePrefix,
                    TopicConnectionString.FullyQualifiedNamespace,
                    opt =>
                    {
                        opt.TopicSubscription = TopicSubscription.Automatic;
                    });
            });

            return await Worker.StartNewAsync(options);
        }

        private WorkerOptions CreateDefaultOptionsOnKey(string key)
        {
            var options = new WorkerOptions();
            options.ConfigureAppConfiguration(configBuilder =>
            {
                configBuilder.AddAzureAppConfiguration(opt =>
                {
                    opt.Connect(AppConfig.ConnectionString)
                       .ConfigureRefresh(refresh => refresh.Register(key))
                       .UseFeatureFlags();
                });
            }).ConfigureLogging(_logger)
              .ConfigureServices(services => services.AddFeatureManagement());

            return options;
        }

        private static async Task EnsureFeatureManagerEnabledAsync(Worker worker, string configKey)
        {
            var featureManager = worker.ServiceProvider.GetRequiredService<IFeatureManager>();
            Assert.True(await featureManager.IsEnabledAsync(configKey), "Feature manager should be enabled at this point");
        }

        private async Task SetConfigSettingAsync(string configKey, string configValue)
        {
            _logger.LogTrace("Set remote app configuration key '{Key}'='{Value}'", configKey, configValue);

            Response<ConfigurationSetting> response = await AppConfigClient.SetConfigurationSettingAsync(configKey, configValue);
            using Response raw = response.GetRawResponse();
            Assert.False(raw.IsError, $"HTTP {raw.Status} {raw.ReasonPhrase}: {raw.Content}");
        }

        private async Task EnableConfigSettingAsync(string configKey)
        {
            _logger.LogTrace("Enable remote app configuration key '{Key}'", configKey);

            Response<ConfigurationSetting> response = await AppConfigClient.SetConfigurationSettingAsync(new FeatureFlagConfigurationSetting(configKey, isEnabled: true));
            using Response raw = response.GetRawResponse();
            Assert.False(raw.IsError, $"HTTP {raw.Status} {raw.ReasonPhrase}: {raw.Content}");
        }

        private async Task DisableConfigSettingAsync(string configKey)
        {
            _logger.LogTrace("Disable remote app configuration key '{Key}'", configKey);

            Response<ConfigurationSetting> response = await AppConfigClient.SetConfigurationSettingAsync(new FeatureFlagConfigurationSetting(configKey, isEnabled: false));
            using Response raw = response.GetRawResponse();
            Assert.False(raw.IsError, $"HTTP {raw.Status} {raw.ReasonPhrase}: {raw.Content}");
        }

        private async Task DeleteConfigSettingAsync(string configKey)
        {
            _logger.LogTrace("Delete remote app configuration key '{Key}'", configKey);

            await AppConfigClient.DeleteConfigurationSettingAsync(new FeatureFlagConfigurationSetting(configKey, isEnabled: false));
            await AppConfigClient.DeleteConfigurationSettingAsync(ConfigKey);
        }

        private static void RetryAssertion(Action assertion)
        {
            Policy.Timeout(TimeSpan.FromSeconds(10))
                  .Wrap(Policy.Handle<AssertActualExpectedException>()
                              .WaitAndRetryForever(index => TimeSpan.FromSeconds(1)))
                  .Execute(assertion);
        }

        private static async Task RetryAssertionAsync(Func<Task> assertion)
        {
            await Policy.TimeoutAsync(TimeSpan.FromSeconds(10))
                        .WrapAsync(Policy.Handle<AssertActualExpectedException>()
                                         .WaitAndRetryForeverAsync(index => TimeSpan.FromSeconds(1)))
                        .ExecuteAsync(assertion);
        }
    }
}
