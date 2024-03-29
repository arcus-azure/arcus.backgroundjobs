﻿using System;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.AzureActiveDirectory;
using Arcus.BackgroundJobs.Tests.Integration.AzureActiveDirectory.Fixture;
using Arcus.BackgroundJobs.Tests.Integration.Fixture;
using Arcus.BackgroundJobs.Tests.Integration.Hosting;
using Arcus.Testing.Logging;
using CloudNative.CloudEvents;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.BackgroundJobs.Tests.Integration.AzureActiveDirectory
{
    [Trait(name: "Category", value: "Integration")]
    [Collection(TestCollections.Integration)]
    public class ClientSecretExpirationJobTests
    {
        private readonly ILogger _logger;
        private readonly TestConfig _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientSecretExpirationJobTests"/> class.
        /// </summary>
        public ClientSecretExpirationJobTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
            _config = TestConfig.Create();
        }

        [Fact]
        public async Task CheckPotentialExpiredClientSecretsInAzureActiveDirectory_WithExpiredSecrets_PublishEventsViaMicrosoftEventGridPublisherClient()
        {
            // Arrange
            var authKeyName = "EventGrid.AuthKey";
            var topicEndpoint = _config.GetRequiredValue<string>("Arcus:Infra:EventGrid:TopicUri");
            var topicEndpointSecret = _config.GetRequiredValue<string>("Arcus:Infra:EventGrid:AuthKey");

            // Act / Assert
            await TestEventPublishingOfPotentialExpiredClientSecretsAsync(services =>
            {
                services.AddCorrelation();
                services.AddSecretStore(stores => stores.AddInMemory(authKeyName, topicEndpointSecret));
                services.AddAzureClients(clients =>
                {
                    clients.AddEventGridPublisherClient(topicEndpoint, authKeyName);
                });
            });
        }

        [Fact]
        public async Task CheckPotentialExpiredClientSecretsInAzureActiveDirectory_WithExpiredSecrets_PublishEventsViaArcusEventGridPublisher()
        {
            await TestEventPublishingOfPotentialExpiredClientSecretsAsync(services =>
            {
                services.AddEventGridPublisher(_config);
            });
        }

        private async Task TestEventPublishingOfPotentialExpiredClientSecretsAsync(
            Action<IServiceCollection> configureServices)
        {
             // Arrange
            int expirationThreshold = 14;
            AzureActiveDirectoryConfig activeDirectoryConfig = _config.GetActiveDirectoryConfig();

            // Act
            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureTenantId, activeDirectoryConfig.TenantId))
            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureServicePrincipalClientId, activeDirectoryConfig.ServicePrincipal.ClientId))
            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureServicePrincipalClientSecret, activeDirectoryConfig.ServicePrincipal.ClientSecret))
            {
                var options = new WorkerOptions();
                options.ConfigureLogging(_logger)
                       .ConfigureServices(services =>
                       {
                           services.AddEventGridPublisher(_config);
                           services.AddClientSecretExpirationJob(opt =>
                           {
                               opt.RunImmediately = true;
                               opt.ExpirationThreshold = expirationThreshold;
                               opt.EventUri = new Uri("https://github.com/arcus-azure/arcus.backgroundjobs");
                           });
                       });

                await using (var worker = await Worker.StartNewAsync(options))
                {
                    await using (var consumer = await TestServiceBusEventConsumer.StartNewAsync(_config, _logger))
                    {
                        // Assert
                        CloudEvent cloudEvent = consumer.Consume();
                        Assert.NotNull(cloudEvent.Id);
                        Assert.True(Enum.TryParse(cloudEvent.Type, out ClientSecretExpirationEventType eventType),
                            $"Event should have either '{ClientSecretExpirationEventType.ClientSecretAboutToExpire}' or '{ClientSecretExpirationEventType.ClientSecretExpired}' as event type");

                        var data = cloudEvent.GetPayload<AzureApplication>();
                        Assert.IsType<Guid>(data.KeyId);

                        bool isAboutToExpire = eventType == ClientSecretExpirationEventType.ClientSecretAboutToExpire;
                        bool isExpired = eventType == ClientSecretExpirationEventType.ClientSecretExpired;

                        Assert.True(isAboutToExpire == (data.RemainingValidDays > 0 && data.RemainingValidDays < expirationThreshold), $"Remaining days should be between 1-{expirationThreshold - 1} when the secret is about to expire");
                        Assert.True(isExpired == data.RemainingValidDays < 0, "Remaining days should be negative when the secret is expired");
                    }
                }
            }
        }
    }
}
