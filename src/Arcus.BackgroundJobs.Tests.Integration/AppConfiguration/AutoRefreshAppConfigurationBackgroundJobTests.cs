using System;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.Tests.Integration.AppConfiguration.Fixture;
using Arcus.BackgroundJobs.Tests.Integration.Fixture;
using Arcus.BackgroundJobs.Tests.Integration.Hosting;
using Arcus.Testing.Logging;
using Azure.Data.AppConfiguration;
using Azure.Messaging.ServiceBus;
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

        private AppTestConfiguration AppConfiguration => _testConfiguration.GetAppConfiguration();
        private ConfigurationClient AppConfigurationClient => new ConfigurationClient(AppConfiguration.ConnectionString);

        [Fact]
        public async Task AutoAppConfigurationRefreshUsingManagedIdentity_WithModifiedKeyValue_UpdatesConfiguration()
        {
            // Arrange
            const string key = "ArcusKey", value = "ArcusValue", updatedValue = "ArcusUpdatedValue";
            await AppConfigurationClient.SetConfigurationSettingAsync(key, value);

            ServicePrincipal servicePrincipal = _testConfiguration.GetServicePrincipal();
            AzureEnvironment environment = _testConfiguration.GetAzureEnvironment();

            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureTenantId, environment.TenantId))
            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureServicePrincipalClientId, servicePrincipal.ClientId))
            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureServicePrincipalClientSecret, servicePrincipal.ClientSecret))
            {
                var properties = ServiceBusConnectionStringProperties.Parse(AppConfiguration.ServiceBusTopicConnectionString);
                var options = new WorkerOptions();
                options.ConfigureAppConfiguration(configBuilder =>
                       {
                           configBuilder.AddAzureAppConfiguration(opt =>
                           {
                               opt.Connect(AppConfiguration.ConnectionString)
                                  .ConfigureRefresh(refresh => refresh.Register(key));
                           });
                       })
                       .ConfigureLogging(_logger)
                       .ConfigureServices(services =>
                       {
                           services.AddSecretStore(stores => stores.AddInMemory(ServiceBusTopicConnectionStringSecretKey, AppConfiguration.ServiceBusTopicConnectionString))
                                   .AddAutoRefreshAppConfigurationBackgroundJobUsingManagedIdentity(properties.EntityPath, "TestSub", properties.FullyQualifiedNamespace);
                       });

                await using (var worker = await Worker.StartNewAsync(options))
                {
                    var workerConfiguration = worker.ServiceProvider.GetRequiredService<IConfiguration>();
                    Assert.Equal(value, workerConfiguration.GetValue<string>(key));

                    // Act
                    await AppConfigurationClient.SetConfigurationSettingAsync(key, updatedValue);

                    // Assert
                    RetryAssertion(() => Assert.Equal(updatedValue, workerConfiguration.GetValue<string>(key)));
                } 
            }
        }

        [Fact]
        public async Task AutoAppConfigurationRefreshUsingManagedIdentity_WithDeletedKeyValue_UpdatesConfiguration()
        {
            // Arrange
            const string key = "ArcusKey", value = "ArcusValue";
            await AppConfigurationClient.SetConfigurationSettingAsync(key, value);

            ServicePrincipal servicePrincipal = _testConfiguration.GetServicePrincipal();
            AzureEnvironment environment = _testConfiguration.GetAzureEnvironment();

            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureTenantId, environment.TenantId))
            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureServicePrincipalClientId, servicePrincipal.ClientId))
            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureServicePrincipalClientSecret, servicePrincipal.ClientSecret))
            {
                var properties = ServiceBusConnectionStringProperties.Parse(AppConfiguration.ServiceBusTopicConnectionString);
                var options = new WorkerOptions();
                options.ConfigureAppConfiguration(configBuilder =>
                       {
                           configBuilder.AddAzureAppConfiguration(opt =>
                           {
                               opt.Connect(AppConfiguration.ConnectionString)
                                  .ConfigureRefresh(refresh => refresh.Register(key));
                           });
                       })
                       .ConfigureLogging(_logger)
                       .ConfigureServices(services =>
                       {
                           services.AddSecretStore(stores => stores.AddInMemory(ServiceBusTopicConnectionStringSecretKey, AppConfiguration.ServiceBusTopicConnectionString))
                                   .AddAutoRefreshAppConfigurationBackgroundJobUsingManagedIdentity(properties.EntityPath, "TestSub", properties.FullyQualifiedNamespace);
                       });

                await using (var worker = await Worker.StartNewAsync(options))
                {
                    var workerConfiguration = worker.ServiceProvider.GetRequiredService<IConfiguration>();
                    Assert.Equal(value, workerConfiguration.GetValue<string>(key));

                    // Act
                    await AppConfigurationClient.DeleteConfigurationSettingAsync(key);
                
                    // Assert
                    RetryAssertion(() => Assert.Null(workerConfiguration.GetValue<string>(key)));
                } 
            }
        }

        [Fact]
        public async Task AutoAppConfigurationRefresh_WithModifiedKeyValue_UpdatesConfiguration()
        {
            // Arrange
            const string key = "ArcusKey", value = "ArcusValue", updatedValue = "ArcusUpdatedValue";
            await AppConfigurationClient.SetConfigurationSettingAsync(key, value);

            var options = new WorkerOptions();
            options.ConfigureAppConfiguration(configBuilder =>
                   {
                       configBuilder.AddAzureAppConfiguration(opt =>
                       {
                           opt.Connect(AppConfiguration.ConnectionString)
                              .ConfigureRefresh(refresh => refresh.Register(key));
                       });
                   })
                   .ConfigureLogging(_logger)
                   .ConfigureServices(services =>
                   {
                       services.AddSecretStore(stores => stores.AddInMemory(ServiceBusTopicConnectionStringSecretKey, AppConfiguration.ServiceBusTopicConnectionString))
                               .AddAutoRefreshAppConfigurationBackgroundJob("TestSub", ServiceBusTopicConnectionStringSecretKey);
                   });

            await using (var worker = await Worker.StartNewAsync(options))
            {
                var workerConfiguration = worker.ServiceProvider.GetRequiredService<IConfiguration>();
                Assert.Equal(value, workerConfiguration.GetValue<string>(key));

                // Act
                await AppConfigurationClient.SetConfigurationSettingAsync(key, updatedValue);
                
                // Assert
                RetryAssertion(() => Assert.Equal(updatedValue, workerConfiguration.GetValue<string>(key)));
            }
        }

         [Fact]
        public async Task AutoAppConfigurationRefresh_WithDeletedKeyValue_UpdatesConfiguration()
        {
            // Arrange
            const string key = "ArcusKey", value = "ArcusValue";
            await AppConfigurationClient.SetConfigurationSettingAsync(key, value);

            var options = new WorkerOptions();
            options.ConfigureAppConfiguration(configBuilder =>
                   {
                       configBuilder.AddAzureAppConfiguration(opt =>
                       {
                           opt.Connect(AppConfiguration.ConnectionString)
                              .ConfigureRefresh(refresh => refresh.Register(key));
                       });
                   })
                   .ConfigureLogging(_logger)
                   .ConfigureServices(services =>
                   {
                       services.AddSecretStore(stores => stores.AddInMemory(ServiceBusTopicConnectionStringSecretKey, AppConfiguration.ServiceBusTopicConnectionString))
                               .AddAutoRefreshAppConfigurationBackgroundJob("TestSub", ServiceBusTopicConnectionStringSecretKey);
                   });

            await using (var worker = await Worker.StartNewAsync(options))
            {
                var workerConfiguration = worker.ServiceProvider.GetRequiredService<IConfiguration>();
                Assert.Equal(value, workerConfiguration.GetValue<string>(key));

                // Act
                await AppConfigurationClient.DeleteConfigurationSettingAsync(key);
                
                // Assert
               RetryAssertion(() => Assert.Null(workerConfiguration.GetValue<string>(key)));
            }
        }

        [Fact]
        public async Task AutoAppConfigurationRefreshUsingManagedIdentity_WithModifiedFeatureToggle_UpdatesConfiguration()
        {
            // Arrange
            const string key = "ArcusFeature";
            await AppConfigurationClient.SetConfigurationSettingAsync(new FeatureFlagConfigurationSetting(key, isEnabled: true));

            ServicePrincipal servicePrincipal = _testConfiguration.GetServicePrincipal();
            AzureEnvironment environment = _testConfiguration.GetAzureEnvironment();

            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureTenantId, environment.TenantId))
            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureServicePrincipalClientId, servicePrincipal.ClientId))
            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureServicePrincipalClientSecret, servicePrincipal.ClientSecret))
            {
                var properties = ServiceBusConnectionStringProperties.Parse(AppConfiguration.ServiceBusTopicConnectionString);
                var options = new WorkerOptions();
                options.ConfigureAppConfiguration(configBuilder =>
                       {
                           configBuilder.AddAzureAppConfiguration(opt =>
                           {
                               opt.Connect(AppConfiguration.ConnectionString)
                                  .ConfigureRefresh(refreshOptions => refreshOptions.Register(key))
                                  .UseFeatureFlags();
                           });
                       })
                       .ConfigureLogging(_logger)
                       .ConfigureServices(services =>
                       {
                           services.AddSecretStore(stores => stores.AddInMemory(ServiceBusTopicConnectionStringSecretKey, AppConfiguration.ServiceBusTopicConnectionString))
                                   .AddAutoRefreshAppConfigurationBackgroundJobUsingManagedIdentity(properties.EntityPath, "TestSub", properties.FullyQualifiedNamespace)
                                   .AddFeatureManagement();
                       });

                await using (var worker = await Worker.StartNewAsync(options))
                {
                    var featureManager = worker.ServiceProvider.GetRequiredService<IFeatureManager>();
                    Assert.True(await featureManager.IsEnabledAsync(key));

                    // Act
                    await AppConfigurationClient.SetConfigurationSettingAsync(new FeatureFlagConfigurationSetting(key, isEnabled: false));

                    // Assert
                    await RetryAssertionAsync(async () =>
                    {
                        bool isEnabled = await featureManager.IsEnabledAsync(key);
                        Assert.False(isEnabled);
                    });
                } 
            }
        }

         [Fact]
        public async Task AutoAppConfigurationRefreshUsingManagedIdentity_WithDeletedFeatureToggle_UpdatesConfiguration()
        {
             // Arrange
            const string key = "ArcusFeature";
            await AppConfigurationClient.SetConfigurationSettingAsync(new FeatureFlagConfigurationSetting(key, isEnabled: true));

            ServicePrincipal servicePrincipal = _testConfiguration.GetServicePrincipal();
            AzureEnvironment environment = _testConfiguration.GetAzureEnvironment();

            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureTenantId, environment.TenantId))
            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureServicePrincipalClientId, servicePrincipal.ClientId))
            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureServicePrincipalClientSecret, servicePrincipal.ClientSecret))
            {
                var properties = ServiceBusConnectionStringProperties.Parse(AppConfiguration.ServiceBusTopicConnectionString);
                var options = new WorkerOptions();
                options.ConfigureAppConfiguration(configBuilder =>
                       {
                           configBuilder.AddAzureAppConfiguration(opt =>
                           {
                               opt.Connect(AppConfiguration.ConnectionString)
                                  .ConfigureRefresh(refreshOptions => refreshOptions.Register(key))
                                  .UseFeatureFlags();
                           });
                       })
                       .ConfigureLogging(_logger)
                       .ConfigureServices(services =>
                       {
                           services.AddSecretStore(stores => stores.AddInMemory(ServiceBusTopicConnectionStringSecretKey, AppConfiguration.ServiceBusTopicConnectionString))
                                   .AddAutoRefreshAppConfigurationBackgroundJobUsingManagedIdentity(properties.EntityPath, "TestSub", properties.FullyQualifiedNamespace)
                                   .AddFeatureManagement();
                       });

                await using (var worker = await Worker.StartNewAsync(options))
                {
                    var featureManager = worker.ServiceProvider.GetRequiredService<IFeatureManager>();
                    Assert.True(await featureManager.IsEnabledAsync(key));

                    // Act
                    await AppConfigurationClient.DeleteConfigurationSettingAsync(new FeatureFlagConfigurationSetting(key, isEnabled: false));

                    // Assert
                    await RetryAssertionAsync(async () =>
                    {
                        bool isEnabled = await featureManager.IsEnabledAsync(key);
                        Assert.False(isEnabled);
                    });
                } 
            }
        }

        [Fact]
        public async Task AutoAppConfigurationRefresh_WithModifiedFeatureToggle_UpdatesConfiguration()
        {
            // Arrange
            const string key = "ArcusFeature";
            await AppConfigurationClient.SetConfigurationSettingAsync(new FeatureFlagConfigurationSetting(key, isEnabled: true));

            var options = new WorkerOptions();
            options.ConfigureAppConfiguration(configBuilder =>
                   {
                       configBuilder.AddAzureAppConfiguration(opt =>
                       {
                           opt.Connect(AppConfiguration.ConnectionString)
                              .ConfigureRefresh(refreshOptions => refreshOptions.Register(key))
                              .UseFeatureFlags();
                       });
                   })
                   .ConfigureLogging(_logger)
                   .ConfigureServices(services =>
                   {
                       services.AddSecretStore(stores => stores.AddInMemory(ServiceBusTopicConnectionStringSecretKey, AppConfiguration.ServiceBusTopicConnectionString))
                               .AddAutoRefreshAppConfigurationBackgroundJob("TestSub", ServiceBusTopicConnectionStringSecretKey)
                               .AddFeatureManagement();
                   });

            await using (var worker = await Worker.StartNewAsync(options))
            {
                var featureManager = worker.ServiceProvider.GetRequiredService<IFeatureManager>();
                Assert.True(await featureManager.IsEnabledAsync(key));

                // Act
                await AppConfigurationClient.SetConfigurationSettingAsync(new FeatureFlagConfigurationSetting(key, isEnabled: false));
                
                // Assert
                await RetryAssertionAsync(async () =>
                {
                    bool isEnabled = await featureManager.IsEnabledAsync(key);
                    Assert.False(isEnabled);
                });
            }
        }

        [Fact]
        public async Task AutoAppConfigurationRefresh_WithDeletedFeatureToggle_UpdatesConfiguration()
        {
             // Arrange
            const string key = "ArcusFeature";
            await AppConfigurationClient.SetConfigurationSettingAsync(new FeatureFlagConfigurationSetting(key, isEnabled: true));

            var options = new WorkerOptions();
            options.ConfigureAppConfiguration(configBuilder =>
                   {
                       configBuilder.AddAzureAppConfiguration(opt =>
                       {
                           opt.Connect(AppConfiguration.ConnectionString)
                              .ConfigureRefresh(refreshOptions => refreshOptions.Register(key))
                              .UseFeatureFlags();
                       });
                   })
                   .ConfigureLogging(_logger)
                   .ConfigureServices(services =>
                   {
                       services.AddSecretStore(stores => stores.AddInMemory(ServiceBusTopicConnectionStringSecretKey, AppConfiguration.ServiceBusTopicConnectionString))
                               .AddAutoRefreshAppConfigurationBackgroundJob("TestSub", ServiceBusTopicConnectionStringSecretKey)
                               .AddFeatureManagement();
                   });

            await using (var worker = await Worker.StartNewAsync(options))
            {
                var featureManager = worker.ServiceProvider.GetRequiredService<IFeatureManager>();
                Assert.True(await featureManager.IsEnabledAsync(key));

                // Act
                await AppConfigurationClient.DeleteConfigurationSettingAsync(new FeatureFlagConfigurationSetting(key, isEnabled: false));
                
                // Assert
                await RetryAssertionAsync(async () =>
                {
                    bool isEnabled = await featureManager.IsEnabledAsync(key);
                    Assert.False(isEnabled);
                });
            }
        }

        private static async Task RetryAssertionAsync(Func<Task> assertion)
        {
            await Policy.TimeoutAsync(TimeSpan.FromSeconds(10))
                        .WrapAsync(Policy.Handle<AssertActualExpectedException>()
                                         .WaitAndRetryForeverAsync(index => TimeSpan.FromSeconds(1)))
                        .ExecuteAsync(assertion);
        }
        
        private static void RetryAssertion(Action assertion)
        {
            Policy.Timeout(TimeSpan.FromSeconds(10))
                  .Wrap(Policy.Handle<AssertActualExpectedException>()
                              .WaitAndRetryForever(index => TimeSpan.FromSeconds(1)))
                  .Execute(assertion);
        }
    }
}
