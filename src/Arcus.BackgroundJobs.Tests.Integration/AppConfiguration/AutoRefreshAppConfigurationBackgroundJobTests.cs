using System;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.Tests.Integration.AppConfiguration.Fixture;
using Arcus.BackgroundJobs.Tests.Integration.Hosting;
using Arcus.Testing.Logging;
using Azure.Data.AppConfiguration;
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
    [Trait("Category", "Integration")]
    [Collection(TestCollections.Integration)]
    public class AutoRefreshAppConfigurationBackgroundJobTests
    {
        private readonly ILogger _logger;
        private readonly TestConfig _testConfiguration;
        
        private const string ServiceBusTopicConnectionStringSecretKey = "Arcus.AppConfiguration.ServiceBusConnectionStringWithKey";
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AutoRefreshAppConfigurationBackgroundJobTests" /> class.
        /// </summary>
        public AutoRefreshAppConfigurationBackgroundJobTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
            _testConfiguration = TestConfig.Create();
        }
        
        [Fact]
        public async Task AutoAppConfigurationRefresh_WithModifiedKeyValue_UpdatesConfiguration()
        {
            // Arrange
            AppTestConfiguration appConfiguration = _testConfiguration.GetAppConfiguration();
            
            const string key = "ArcusKey", value = "ArcusValue", updatedValue = "ArcusUpdatedValue";
            var client = new ConfigurationClient(appConfiguration.ConnectionString);
            await client.SetConfigurationSettingAsync(key, value);

            var options = new WorkerOptions();
            options.ConfigureAppConfiguration(configBuilder =>
                   {
                       configBuilder.AddAzureAppConfiguration(opt =>
                       {
                           opt.Connect(appConfiguration.ConnectionString)
                              .ConfigureRefresh(refresh => refresh.Register(key));
                       });
                   })
                   .ConfigureLogging(_logger)
                   .ConfigureServices(services =>
                   {
                       services.AddSecretStore(stores => stores.AddInMemory(ServiceBusTopicConnectionStringSecretKey, appConfiguration.ServiceBusTopicConnectionString))
                               .AddAutoRefreshAppConfigurationBackgroundJob("TestSub", ServiceBusTopicConnectionStringSecretKey);
                   });

            await using (var worker = await Worker.StartNewAsync(options))
            {
                var workerConfiguration = worker.ServiceProvider.GetService<IConfiguration>();
                Assert.Equal(value, workerConfiguration.GetValue<string>(key));

                // Act
                await client.SetConfigurationSettingAsync(key, updatedValue);
                
                // Assert
                RetryAssertion(() => Assert.Equal(updatedValue, workerConfiguration.GetValue<string>(key)),
                    timeout: TimeSpan.FromSeconds(10),
                    interval: TimeSpan.FromSeconds(1));
            }
        }

        [Fact]
        public async Task AutoAppConfigurationRefresh_WithDeletedKeyValue_UpdatesConfiguration()
        {
            // Arrange
            AppTestConfiguration appConfiguration = _testConfiguration.GetAppConfiguration();
            
            const string key = "ArcusKey", value = "ArcusValue";
            var client = new ConfigurationClient(appConfiguration.ConnectionString);
            await client.SetConfigurationSettingAsync(key, value);

            var options = new WorkerOptions();
            options.ConfigureAppConfiguration(configBuilder =>
                   {
                       configBuilder.AddAzureAppConfiguration(opt =>
                       {
                           opt.Connect(appConfiguration.ConnectionString)
                              .ConfigureRefresh(refresh => refresh.Register(key));
                       });
                   })
                   .ConfigureLogging(_logger)
                   .ConfigureServices(services =>
                   {
                       services.AddSecretStore(stores => stores.AddInMemory(ServiceBusTopicConnectionStringSecretKey, appConfiguration.ServiceBusTopicConnectionString))
                               .AddAutoRefreshAppConfigurationBackgroundJob("TestSub", ServiceBusTopicConnectionStringSecretKey);
                   });

            await using (var worker = await Worker.StartNewAsync(options))
            {
                var workerConfiguration = worker.ServiceProvider.GetService<IConfiguration>();
                Assert.Equal(value, workerConfiguration.GetValue<string>(key));

                // Act
                await client.DeleteConfigurationSettingAsync(key);
                
                // Assert
                RetryAssertion(() => Assert.Null(workerConfiguration.GetValue<string>(key)),
                    timeout: TimeSpan.FromSeconds(10),
                    interval: TimeSpan.FromSeconds(1));
            }
        }

        [Fact]
        public async Task AutoAppConfigurationRefresh_WithModifiedFeatureToggle_UpdatesConfiguration()
        {
            // Arrange
            AppTestConfiguration appConfiguration = _testConfiguration.GetAppConfiguration();

            const string key = "ArcusFeature";
            var client = new ConfigurationClient(appConfiguration.ConnectionString);
            await client.SetConfigurationSettingAsync(new FeatureFlagConfigurationSetting(key, isEnabled: true));

            var options = new WorkerOptions();
            options.ConfigureAppConfiguration(configBuilder =>
                   {
                       configBuilder.AddAzureAppConfiguration(opt =>
                       {
                           opt.Connect(appConfiguration.ConnectionString)
                              .ConfigureRefresh(refreshOptions => refreshOptions.Register(key))
                              .UseFeatureFlags();
                       });
                   })
                   .ConfigureLogging(_logger)
                   .ConfigureServices(services =>
                   {
                       services.AddSecretStore(stores => stores.AddInMemory(ServiceBusTopicConnectionStringSecretKey, appConfiguration.ServiceBusTopicConnectionString))
                               .AddAutoRefreshAppConfigurationBackgroundJob("TestSub", ServiceBusTopicConnectionStringSecretKey)
                               .AddFeatureManagement();
                   });

            await using (var worker = await Worker.StartNewAsync(options))
            {
                var featureManager = worker.ServiceProvider.GetService<IFeatureManager>();
                Assert.True(await featureManager.IsEnabledAsync(key));

                // Act
                await client.SetConfigurationSettingAsync(new FeatureFlagConfigurationSetting(key, isEnabled: false));
                
                // Assert
                await RetryAssertionAsync(async () =>
                    {
                        bool isEnabled = await featureManager.IsEnabledAsync(key);
                        Assert.False(isEnabled);
                    },
                    timeout: TimeSpan.FromSeconds(10),
                    interval: TimeSpan.FromSeconds(1));
            }
        }

        [Fact]
        public async Task AutoAppConfigurationRefresh_WithDeletedFeatureToggle_UpdatesConfiguration()
        {
             // Arrange
            AppTestConfiguration appConfiguration = _testConfiguration.GetAppConfiguration();

            const string key = "ArcusFeature";
            var client = new ConfigurationClient(appConfiguration.ConnectionString);
            await client.SetConfigurationSettingAsync(new FeatureFlagConfigurationSetting(key, isEnabled: true));

            var options = new WorkerOptions();
            options.ConfigureAppConfiguration(configBuilder =>
                   {
                       configBuilder.AddAzureAppConfiguration(opt =>
                       {
                           opt.Connect(appConfiguration.ConnectionString)
                              .ConfigureRefresh(refreshOptions => refreshOptions.Register(key))
                              .UseFeatureFlags();
                       });
                   })
                   .ConfigureLogging(_logger)
                   .ConfigureServices(services =>
                   {
                       services.AddSecretStore(stores => stores.AddInMemory(ServiceBusTopicConnectionStringSecretKey, appConfiguration.ServiceBusTopicConnectionString))
                               .AddAutoRefreshAppConfigurationBackgroundJob("TestSub", ServiceBusTopicConnectionStringSecretKey)
                               .AddFeatureManagement();
                   });

            await using (var worker = await Worker.StartNewAsync(options))
            {
                var featureManager = worker.ServiceProvider.GetService<IFeatureManager>();
                Assert.True(await featureManager.IsEnabledAsync(key));

                // Act
                await client.DeleteConfigurationSettingAsync(new FeatureFlagConfigurationSetting(key, isEnabled: false));
                
                // Assert
                await RetryAssertionAsync(async () =>
                    {
                        bool isEnabled = await featureManager.IsEnabledAsync(key);
                        Assert.False(isEnabled);
                    },
                    timeout: TimeSpan.FromSeconds(10),
                    interval: TimeSpan.FromSeconds(1));
            }
        }

        private static async Task RetryAssertionAsync(Func<Task> assertion, TimeSpan timeout, TimeSpan interval)
        {
            await Policy.TimeoutAsync(timeout)
                        .WrapAsync(Policy.Handle<AssertActualExpectedException>()
                                         .WaitAndRetryForeverAsync(index => interval))
                        .ExecuteAsync(assertion);
        }
        
        private static void RetryAssertion(Action assertion, TimeSpan timeout, TimeSpan interval)
        {
            Policy.Timeout(timeout)
                  .Wrap(Policy.Handle<AssertActualExpectedException>()
                              .WaitAndRetryForever(index => interval))
                  .Execute(assertion);
        }
    }
}
