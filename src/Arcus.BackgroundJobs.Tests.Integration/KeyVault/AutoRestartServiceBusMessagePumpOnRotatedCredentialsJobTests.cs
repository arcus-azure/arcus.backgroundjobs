using System;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.Tests.Integration.Fixture.ServiceBus;
using Arcus.BackgroundJobs.Tests.Integration.Hosting;
using Arcus.BackgroundJobs.Tests.Integration.Hosting.ServiceBus;
using Arcus.BackgroundJobs.Tests.Integration.KeyVault.Fixture;
using Arcus.EventGrid.Publishing;
using Arcus.Security.Providers.AzureKeyVault.Authentication;
using Arcus.Testing.Logging;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.ServiceBus.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.BackgroundJobs.Tests.Integration.KeyVault
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
            var tenantId = config.GetValue<string>("Arcus:KeyRotation:ServiceBus:TenantId");
            var applicationId = config.GetValue<string>("Arcus:ServicePrincipal:ApplicationId");
            var clientKey = config.GetValue<string>("Arcus:ServicePrincipal:AccessKey");
            var keyVaultUri = config.GetValue<string>("Arcus:KeyVault:Uri");
            
            _logger.LogInformation("Using Service Principal [ClientID: '{0}']", applicationId);

            var client = new ServiceBusConfiguration(rotationConfig, _logger);
            string freshConnectionString = await client.RotateConnectionStringKeysForQueueAsync(KeyType.PrimaryKey);

            SecretClient keyVaultClient = CreateKeyVaultClient(tenantId, keyVaultUri, applicationId, clientKey);
            await keyVaultClient.SetSecretAsync(rotationConfig.KeyVault.SecretName, freshConnectionString);

            string jobId = Guid.NewGuid().ToString();
            const string connectionStringSecretKey = "ARCUS_KEYVAULT_SECRETNEWVERSIONCREATED_CONNECTIONSTRING";

            var options = new WorkerOptions();
            options.Configuration.Add(connectionStringSecretKey, rotationConfig.KeyVault.SecretNewVersionCreated.ConnectionString);
            AddEventGridPublisher(options, config);
            AddSecretStore(options, tenantId, keyVaultUri, applicationId, clientKey);
            AddServiceBusMessagePump(options, rotationConfig, jobId);
            
            options.AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob(
                jobId: jobId,
                subscriptionNamePrefix: "TestSub",
                serviceBusTopicConnectionStringSecretKey: connectionStringSecretKey,
                messagePumpConnectionStringKey: rotationConfig.KeyVault.SecretName);

            await using (var worker = await Worker.StartNewAsync(options))
            {
                string newSecondaryConnectionString = await client.RotateConnectionStringKeysForQueueAsync(KeyType.SecondaryKey);
                await keyVaultClient.SetSecretAsync(rotationConfig.KeyVault.SecretName, newSecondaryConnectionString);

                await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
                {
                    // Act
                    string newPrimaryConnectionString = await client.RotateConnectionStringKeysForQueueAsync(KeyType.PrimaryKey);

                    // Assert
                    await service.SimulateMessageProcessingAsync(newPrimaryConnectionString);
                }
            }
        }

        private static SecretClient CreateKeyVaultClient(string tenantId, string keyVaultUri, string applicationId, string clientKey)
        {
            var credential = new ClientSecretCredential(
                tenantId,
                applicationId,
                clientKey);
            
            var secretClient = new SecretClient(new Uri(keyVaultUri), credential);
            return secretClient;
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

        private static void AddSecretStore(WorkerOptions options, string tenantId, string vaultUri, string clientId, string clientSecret)
        {
            options.Configure(host => host.ConfigureSecretStore((configuration, stores) =>
            {
                stores.AddAzureKeyVaultWithServicePrincipal(vaultUri, tenantId, clientId, clientSecret)
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
