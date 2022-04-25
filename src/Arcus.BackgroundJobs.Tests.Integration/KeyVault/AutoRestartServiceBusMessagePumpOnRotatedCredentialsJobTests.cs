using System;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.Tests.Integration.CloudEvents.Fixture;
using Arcus.BackgroundJobs.Tests.Integration.Fixture;
using Arcus.BackgroundJobs.Tests.Integration.Fixture.ServiceBus;
using Arcus.BackgroundJobs.Tests.Integration.Hosting;
using Arcus.BackgroundJobs.Tests.Integration.KeyVault.Fixture;
using Arcus.EventGrid;
using Arcus.EventGrid.Contracts;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Testing.Logging;
using Azure;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Management.ServiceBus.Models;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

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

        [Fact]
        public async Task ServiceBusMessagePump_RotateServiceBusConnectionKeysOnSecretNewVersionNotification_MessagePumpRestartsThenMessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            var serviceBusClient = ServiceBusConfiguration.CreateFrom(config, _logger);
            string freshConnectionString = await serviceBusClient.RotateConnectionStringKeysForQueueAsync(KeyType.PrimaryKey);
            SecretClient keyVaultClient = CreateKeyVaultClient(config);
            await keyVaultClient.SetSecretAsync(KeyVaultSecretName, freshConnectionString);

            var jobId = Guid.NewGuid().ToString();
            var options = new WorkerOptions();
            options.ConfigureLogging(_logger)
                   .ConfigureServices(services =>
                   {
                       services.AddEventGridPublisher(config);
                       services.AddSecretStore(stores =>
                       {
                           KeyRotationConfig rotationConfig = config.GetKeyRotationConfig();
                           stores.AddInMemory(ConnectionStringSecretKey, rotationConfig.KeyVault.SecretNewVersionCreated.ConnectionString)
                                 .AddAzureKeyVaultWithServicePrincipal(config);
                       }); 
                       services.AddServiceBusQueueMessagePump(KeyVaultSecretName, opt =>
                       { 
                           opt.JobId = jobId;
                           // Unrealistic big maximum exception count so that we're certain that the message pump gets restarted based on the notification and not the unauthorized exception.
                           opt.MaximumUnauthorizedExceptionsBeforeRestart = 1000;
                       }).WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();
                       
                       services.AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob(
                           jobId: jobId,
                           subscriptionNamePrefix: "TestSub",
                           serviceBusTopicConnectionStringSecretKey: ConnectionStringSecretKey,
                           messagePumpConnectionStringKey: KeyVaultSecretName);
                   });

            await using (var worker = await Worker.StartNewAsync(options))
            {
                string newSecondaryConnectionString = await serviceBusClient.RotateConnectionStringKeysForQueueAsync(KeyType.SecondaryKey);
                await keyVaultClient.SetSecretAsync(KeyVaultSecretName, newSecondaryConnectionString);

                Order order = OrderGenerator.GenerateOrder();
                ServiceBusMessage message = order.AsServiceBusMessage($"operation-{Guid.NewGuid()}");

                await using (var consumer = await TestServiceBusEventConsumer.StartNewAsync(config, _logger))
                {
                    // Act
                    string newPrimaryConnectionString = await serviceBusClient.RotateConnectionStringKeysForQueueAsync(KeyType.PrimaryKey);

                    // Assert
                    var producer = TestServiceBusEventProducer.Create(newPrimaryConnectionString);
                    await producer.ProduceAsync(message);
                    EventBatch<Event> eventBatch = consumer.Consume(message.CorrelationId);
                    Event @event = Assert.Single(eventBatch.Events);
                    var eventData = @event.GetPayload<OrderCreatedEventData>();
                    Assert.Equal(order.Id, eventData.Id);
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
            var subscriptionPrefix = "AutoRestart";

            var options = new WorkerOptions();
            options.ConfigureServices(services =>
            {
                services.AddSecretStore(stores =>
                {
                    stores.AddInMemory(ConnectionStringSecretKey, rotationConfig.KeyVault.SecretNewVersionCreated.ConnectionString)
                          .AddAzureKeyVaultWithServicePrincipal(config);
                });
                services.AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob(
                    jobId: Guid.NewGuid().ToString(),
                    subscriptionNamePrefix: subscriptionPrefix,
                    serviceBusTopicConnectionStringSecretKey: ConnectionStringSecretKey,
                    messagePumpConnectionStringKey: KeyVaultSecretName,
                    opt => opt.TopicSubscription = topicSubscription);
            });

            // Act
            await using (var worker = await Worker.StartNewAsync(options))
            {
                // Assert
                var client = new ServiceBusAdministrationClient(rotationConfig.KeyVault.SecretNewVersionCreated.ConnectionString);
                var properties = ServiceBusConnectionStringProperties.Parse(rotationConfig.KeyVault.SecretNewVersionCreated.ConnectionString);

                bool actual = await ContainsTopicSubscriptionAsync(client, properties, subscriptionPrefix);
                Assert.True(expected == actual, $"Azure Service Bus topic subscription was not created/deleted as expected {expected} != {actual} when topic subscription '{topicSubscription}'");
            }
        }

        private static async Task<bool> ContainsTopicSubscriptionAsync(
            ServiceBusAdministrationClient client,
            ServiceBusConnectionStringProperties properties,
            string subscriptionPrefix)
        {
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
