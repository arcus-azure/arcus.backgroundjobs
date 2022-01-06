using System;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.Tests.Integration.Fixture.ServiceBus;
using Arcus.BackgroundJobs.Tests.Integration.Hosting;
using Arcus.BackgroundJobs.Tests.Integration.Hosting.ServiceBus;
using Arcus.BackgroundJobs.Tests.Integration.KeyVault.Fixture;
using Arcus.EventGrid.Publishing;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Security.Providers.AzureKeyVault.Authentication;
using Arcus.Testing.Logging;
using Azure;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
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
        private const string ConnectionStringSecretKey = "ARCUS_KEYVAULT_SECRETNEWVERSIONCREATED_CONNECTIONSTRING";
        
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

            var jobId = Guid.NewGuid().ToString();
            var options = new WorkerOptions();
            options.Configuration.Add(ConnectionStringSecretKey, rotationConfig.KeyVault.SecretNewVersionCreated.ConnectionString);
            AddEventGridPublisher(options, config);
            AddSecretStore(options, tenantId, keyVaultUri, applicationId, clientKey);
            AddServiceBusMessagePump(options, rotationConfig, jobId);
            
            options.AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob(
                jobId: jobId,
                subscriptionNamePrefix: "TestSub",
                serviceBusTopicConnectionStringSecretKey: ConnectionStringSecretKey,
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

        [Theory]
        [InlineData(TopicSubscription.None, false)]
        [InlineData(TopicSubscription.CreateOnStart, true)]
        [InlineData(TopicSubscription.DeleteOnStop, false)]
        [InlineData(TopicSubscription.CreateOnStart | TopicSubscription.DeleteOnStop, true)]
        public async Task AutoRestartServiceBusMessagePump_WithTopicSubscriptionInOptions_UsesOptions(
            TopicSubscription topicSubscription,
            bool expected)
        {
            var config = TestConfig.Create();
            KeyRotationConfig rotationConfig = config.GetKeyRotationConfig();
            var options = new WorkerOptions();
            options.Configuration.Add(ConnectionStringSecretKey, rotationConfig.KeyVault.SecretNewVersionCreated.ConnectionString);
            
            var tenantId = config.GetValue<string>("Arcus:KeyRotation:ServiceBus:TenantId");
            var applicationId = config.GetValue<string>("Arcus:ServicePrincipal:ApplicationId");
            var clientKey = config.GetValue<string>("Arcus:ServicePrincipal:AccessKey");
            var keyVaultUri = config.GetValue<string>("Arcus:KeyVault:Uri");
            var jobId = Guid.NewGuid().ToString();

            AddSecretStore(options, tenantId, keyVaultUri, applicationId, clientKey);

            var subscriptionPrefix = "AutoRestart";
            options.AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob(
                jobId: jobId,
                subscriptionNamePrefix: subscriptionPrefix,
                serviceBusTopicConnectionStringSecretKey: ConnectionStringSecretKey,
                messagePumpConnectionStringKey: rotationConfig.KeyVault.SecretName,
                opt => opt.TopicSubscription = topicSubscription);

            // Act
            await using (var worker = await Worker.StartNewAsync(options))
            {
                // Assert
                var client = new ServiceBusAdministrationClient(rotationConfig.KeyVault.SecretNewVersionCreated.ConnectionString);
                var properties = ServiceBusConnectionStringProperties.Parse(rotationConfig.KeyVault.SecretNewVersionCreated.ConnectionString);

                bool actual = false;
                AsyncPageable<SubscriptionProperties> subscriptions = client.GetSubscriptionsAsync(properties.EntityPath);
                await foreach (SubscriptionProperties sub in subscriptions)
                {
                    if (sub.SubscriptionName.StartsWith(subscriptionPrefix))
                    {
                        await client.DeleteSubscriptionAsync(properties.EntityPath, sub.SubscriptionName);
                        actual = true;
                    }
                }

                Assert.True(expected == actual, $"Azure Service Bus topic subscription was not created/deleted as expected {expected} != {actual} when topic subscription '{topicSubscription}'");
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
