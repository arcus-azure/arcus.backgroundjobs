using System;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.Tests.Integration.Fixture.KeyVault;
using Arcus.BackgroundJobs.Tests.Integration.Fixture.ServiceBus;
using Arcus.BackgroundJobs.Tests.Integration.Hosting;
using Arcus.BackgroundJobs.Tests.Integration.Hosting.ServiceBus;
using Arcus.EventGrid.Publishing;
using Arcus.Security.Providers.AzureKeyVault.Authentication;
using Arcus.Testing.Logging;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.ServiceBus.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.BackgroundJobs.Tests.Integration.Jobs
{
    [Trait("Category", "Integration")]
    public class AutoRestartServiceBusMessagePumpOnRotatedCredentialsJobTests
    {
        private readonly ILogger _logger;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AutoRestartServiceBusMessagePumpOnRotatedCredentialsJobTests" /> class.
        /// </summary>
        public AutoRestartServiceBusMessagePumpOnRotatedCredentialsJobTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }
        
        [Fact]
        public async Task ServiceBusMessagePump_RotateServiceBusConnectionKeysOnSecretNewVersionNotification_MessagePumpRestartsThenMessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            KeyRotationConfig rotationConfig = config.GetKeyRotationConfig();
            _logger.LogInformation("Using Service Principal [ClientID: '{0}']", rotationConfig.ServicePrincipal.ClientId);

            var client = new ServiceBusConfiguration(rotationConfig, _logger);
            string freshConnectionString = await client.RotateConnectionStringKeysForQueueAsync(KeyType.PrimaryKey);

            IKeyVaultClient keyVaultClient = await CreateKeyVaultClientAsync(rotationConfig);
            await SetConnectionStringInKeyVaultAsync(keyVaultClient, rotationConfig, freshConnectionString);

            string jobId = Guid.NewGuid().ToString();
            const string connectionStringSecretKey = "ARCUS_KEYVAULT_SECRETNEWVERSIONCREATED_CONNECTIONSTRING";

            var options = new WorkerOptions();
            options.Configuration.Add(connectionStringSecretKey, rotationConfig.KeyVault.SecretNewVersionCreated.ConnectionString);
            AddEventGridPublisher(options, config);
            AddSecretStore(options, rotationConfig);
            AddServiceBusMessagePump(options, rotationConfig, jobId);
            
            options.AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob(
                jobId: jobId,
                subscriptionNamePrefix: "TestSub",
                serviceBusTopicConnectionStringSecretKey: connectionStringSecretKey,
                messagePumpConnectionStringKey: rotationConfig.KeyVault.SecretName);

            await using (var worker = await Worker.StartNewAsync(options))
            {
                string newSecondaryConnectionString = await client.RotateConnectionStringKeysForQueueAsync(KeyType.SecondaryKey);
                await SetConnectionStringInKeyVaultAsync(keyVaultClient, rotationConfig, newSecondaryConnectionString);

                await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
                {
                    // Act
                    string newPrimaryConnectionString = await client.RotateConnectionStringKeysForQueueAsync(KeyType.PrimaryKey);

                    // Assert
                    await service.SimulateMessageProcessingAsync(newPrimaryConnectionString);
                }
            }
        }

        private static async Task<IKeyVaultClient> CreateKeyVaultClientAsync(KeyRotationConfig rotationConfig)
        {
            ServicePrincipalAuthentication authentication = rotationConfig.ServicePrincipal.CreateAuthentication();
            IKeyVaultClient keyVaultClient = await authentication.AuthenticateAsync();
            
            return keyVaultClient;
        }

        private static async Task SetConnectionStringInKeyVaultAsync(IKeyVaultClient keyVaultClient, KeyRotationConfig keyRotationConfig, string rotatedConnectionString)
        {
            await keyVaultClient.SetSecretAsync(
                vaultBaseUrl: keyRotationConfig.KeyVault.VaultUri,
                secretName: keyRotationConfig.KeyVault.SecretName,
                value: rotatedConnectionString);
        }

        private static void AddEventGridPublisher(WorkerOptions options, TestConfig config)
        {
            options.Services.AddTransient(svc =>
            {
                string eventGridTopic = config.GetTestInfraEventGridTopicUri();
                string eventGridKey = config.GetTestInfraEventGridAuthKey();
                return EventGridPublisherBuilder
                       .ForTopic(eventGridTopic)
                       .UsingAuthenticationKey(eventGridKey)
                       .Build();
            });
        }

        private static void AddSecretStore(WorkerOptions options, KeyRotationConfig rotationConfig)
        {
            options.Configure(host => host.ConfigureSecretStore((configuration, stores) =>
            {
                stores.AddAzureKeyVaultWithServicePrincipal(
                          rotationConfig.KeyVault.VaultUri,
                          rotationConfig.ServicePrincipal.ClientId,
                          rotationConfig.ServicePrincipal.ClientSecret)
                      .AddConfiguration(configuration);
            }));
        }

        private static void AddServiceBusMessagePump(WorkerOptions options, KeyRotationConfig rotationConfig, string jobId)
        {
            options.AddServiceBusQueueMessagePump(rotationConfig.KeyVault.SecretName, opt =>
            {
                opt.JobId = jobId;
                // Unrealistic big maximum exception count so that we're certain that the message pump gets restarted based on the notification and not the unauthorized exception.
                opt.MaximumUnauthorizedExceptionsBeforeRestart = 1000;
            }).WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();
        }
    }
}
