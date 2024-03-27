using System;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.Tests.Integration.Fixture;
using Arcus.BackgroundJobs.Tests.Integration.Fixture.ServiceBus;
using Arcus.BackgroundJobs.Tests.Integration.Hosting;
using Arcus.BackgroundJobs.Tests.Integration.KeyVault.Fixture;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Testing.Logging;
using Azure;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Management.ServiceBus.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Arcus.BackgroundJobs.Tests.Integration.KeyVault
{
    [Trait("Category", "Integration")]
    [Collection(TestCollections.Integration)]
    public class AutoRestartServiceBusMessagePumpOnRotatedCredentialsJobTests
    {
        private const string ConnectionStringSecretKey = "ARCUS_KEYVAULT_SECRETNEWVERSIONCREATED_CONNECTIONSTRING",
                             KeyVaultSecretName = "ConnectionStringSecretName";
        
        private readonly ILogger _logger;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AutoRestartServiceBusMessagePumpOnRotatedCredentialsJobTests" /> class.
        /// </summary>
        public AutoRestartServiceBusMessagePumpOnRotatedCredentialsJobTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }

        private TestConfig Config { get; } = TestConfig.Create();
        private KeyRotationConfig KeyRotationConfig => Config.GetKeyRotationConfig();

        [Fact]
        public async Task ServiceBusMessagePump_RotateServiceBusConnectionKeysOnSecretNewVersionNotification_MessagePumpRestartsThenMessageSuccessfullyProcessed()
        {
            // Arrange
            ServiceBusConfiguration serviceBusClient = CreateServiceBusClient();
            string freshConnectionString = await serviceBusClient.RotateConnectionStringKeysForQueueAsync(KeyType.PrimaryKey);
            
            SecretClient keyVaultClient = CreateKeyVaultClient(Config);
            await keyVaultClient.SetSecretAsync(KeyVaultSecretName, freshConnectionString);

            var options = new WorkerOptions();
            AddTestMessagePumpWithAutoRestart(options);

            await using (var worker = await Worker.StartNewAsync(options))
            {
                string newSecondaryConnectionString = await serviceBusClient.RotateConnectionStringKeysForQueueAsync(KeyType.SecondaryKey);
                await keyVaultClient.SetSecretAsync(KeyVaultSecretName, newSecondaryConnectionString);

                // Act
                await serviceBusClient.RotateConnectionStringKeysForQueueAsync(KeyType.PrimaryKey);

                // Assert
                AssertTestMessagePumpRestarted(worker);
            }
        }

        private void AddTestMessagePumpWithAutoRestart(WorkerOptions options)
        {
            options.ConfigureLogging(_logger)
                   .ConfigureServices(services =>
                   {
                       services.AddEventGridPublisher(Config);
                       services.AddSecretStore(stores =>
                       {
                           KeyRotationConfig rotationConfig = Config.GetKeyRotationConfig();
                           stores.AddInMemory(ConnectionStringSecretKey, rotationConfig.KeyVault.SecretNewVersionCreated.ConnectionString)
                                 .AddAzureKeyVaultWithServicePrincipal(Config);
                       }); 
                       services.AddTestSpyServiceBusMessagePump()
                               .WithAutoRestartOnRotatedCredentials(
                                   subscriptionNamePrefix: "TestSub",
                                   serviceBusTopicConnectionStringSecretKey: ConnectionStringSecretKey,
                                   messagePumpConnectionStringKey: KeyVaultSecretName,
                                   opt => opt.TopicSubscription = TopicSubscription.Automatic);
                   });
        }

        private ServiceBusConfiguration CreateServiceBusClient()
        {
            return ServiceBusConfiguration.CreateFrom(Config, _logger);
        }

        private static void AssertTestMessagePumpRestarted(Worker worker)
        {
            var serviceBusPump = Assert.Single(
                worker.ServiceProvider.GetServices<IHostedService>(),
                p => p is TestSpyRestartServiceBusMessagePump);
            var testPump = Assert.IsType<TestSpyRestartServiceBusMessagePump>(serviceBusPump);

            Policy.Timeout(TimeSpan.FromSeconds(60))
                  .Wrap(Policy.Handle<XunitException>()
                              .WaitAndRetryForever(_ => TimeSpan.FromMicroseconds(500)))
                  .Execute(() => Assert.True(testPump.IsRestarted, "Rotated secret should restart message pump"));
        }

        [Theory]
        [InlineData(TopicSubscription.None, false)]
        [InlineData(TopicSubscription.Automatic, true)]
        public async Task AutoRestartServiceBusMessagePump_WithTopicSubscriptionInOptions_UsesOptions(
            TopicSubscription topicSubscription,
            bool expectedContainsTopicSubscription)
        {
            // Arrange
            var subscriptionPrefix = "AutoRestart";
            var options = new WorkerOptions();
            AddTopicMessagePumpWithAutoRestart(options, topicSubscription, subscriptionPrefix);

            // Act
            await using (await Worker.StartNewAsync(options))
            {
                // Assert
               bool actualContainsTopicSubscription = await ContainsTopicSubscriptionAsync(subscriptionPrefix);
                Assert.True(expectedContainsTopicSubscription == actualContainsTopicSubscription, 
                    $"Azure Service Bus topic subscription was not created/deleted as expected {expectedContainsTopicSubscription} != {actualContainsTopicSubscription} when topic subscription '{topicSubscription}'");
            }
        }

        private void AddTopicMessagePumpWithAutoRestart(
            WorkerOptions options,
            TopicSubscription topicSubscription,
            string subscriptionPrefix)
        {
            options.ConfigureServices(services =>
            {
                services.AddSecretStore(stores =>
                {
                    stores.AddInMemory(ConnectionStringSecretKey, KeyRotationConfig.KeyVault.SecretNewVersionCreated.ConnectionString)
                          .AddAzureKeyVaultWithServicePrincipal(Config);
                });
                var collection = new ServiceBusMessageHandlerCollection(services);
                collection.WithAutoRestartOnRotatedCredentials(
                    jobId: Guid.NewGuid().ToString(),
                    subscriptionNamePrefix: subscriptionPrefix,
                    serviceBusTopicConnectionStringSecretKey: ConnectionStringSecretKey,
                    messagePumpConnectionStringKey: KeyVaultSecretName,
                    opt => opt.TopicSubscription = topicSubscription);
            });
        }

        private async Task<bool> ContainsTopicSubscriptionAsync(string subscriptionPrefix)
        {
            var client = new ServiceBusAdministrationClient(KeyRotationConfig.KeyVault.SecretNewVersionCreated.ConnectionString);
            var properties = ServiceBusConnectionStringProperties.Parse(KeyRotationConfig.KeyVault.SecretNewVersionCreated.ConnectionString);

            var actual = false;
            AsyncPageable<SubscriptionProperties> subscriptions = client.GetSubscriptionsAsync(properties.EntityPath);
            await foreach (SubscriptionProperties sub in subscriptions)
            {
                if (sub.SubscriptionName.StartsWith(subscriptionPrefix))
                {
                    await client.DeleteSubscriptionAsync(properties.EntityPath, sub.SubscriptionName);
                    actual = true;
                }
            }

            return actual;
        }

        private static SecretClient CreateKeyVaultClient(TestConfig config)
        {
            AzureEnvironment environment = config.GetAzureEnvironment();
            ServicePrincipal servicePrincipal = config.GetServicePrincipal();
            string keyVaultUri = config.GetKeyVaultUri();

            var credential = new ClientSecretCredential(
                environment.TenantId,
                servicePrincipal.ClientId,
                servicePrincipal.ClientSecret);
            
            var secretClient = new SecretClient(new Uri(keyVaultUri), credential);
            return secretClient;
        }
    }
}
