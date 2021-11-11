using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.AzureActiveDirectory;
using Arcus.BackgroundJobs.Tests.Integration.AzureActiveDirectory.Fixture;
using Arcus.BackgroundJobs.Tests.Integration.Fixture;
using Arcus.BackgroundJobs.Tests.Integration.Hosting;
using Arcus.EventGrid;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Publishing;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.Testing.Logging;
using CloudNative.CloudEvents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.BackgroundJobs.Tests.Integration.AzureActiveDirectory
{
    [Trait(name: "Category", value: "Integration")]
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
        public async Task CheckExpiredOrAboutToExpireClientSecretsInAzureActiveDirectory_PublishEvents()
        {
            // Arrange
            int expirationThreshold = 14;
            IEventGridPublisher eventGridPublisher = CreateEventGridPublisher();
            var options = new WorkerOptions();
            options.ConfigureLogging(_logger)
                   .AddSingleton<IEventGridPublisher>(eventGridPublisher)
                   .AddClientSecretExpirationJob(opt => 
                   {
                       opt.RunImmediately = true;
                       opt.ExpirationThreshold = expirationThreshold;
                       opt.EventUri = new Uri("https://github.com/arcus-azure/arcus.backgroundjobs");
                   });

            var tenantId = _config.GetValue<string>("Arcus:AzureActiveDirectory:TenantId");
            var clientId = _config.GetValue<string>("Arcus:AzureActiveDirectory:ServicePrincipal:ClientId");
            var clientSecret = _config.GetValue<string>("Arcus:AzureActiveDirectory:ServicePrincipal:ClientSecret");

            // Act
            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureTenantId, tenantId))
            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureServicePrincipalClientId, clientId))
            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureServicePrincipalClientSecret, clientSecret))
            await using (var worker = await Worker.StartNewAsync(options))
            {
                await using (var consumer = await TestServiceBusEventConsumer.StartNewAsync(_config, _logger))
                {
                    // Assert
                    EventBatch<Event> eventBatch = consumer.Consume();
                    Assert.NotEmpty(eventBatch.Events);
                    IEnumerable<CloudEvent> cloudEvents = eventBatch.Events.Select(ev => ev.AsCloudEvent());
                    Assert.All(cloudEvents, ev =>
                    {
                        Assert.NotNull(ev.Id);
                        Assert.True(Enum.TryParse(ev.Type, out ClientSecretExpirationEventType eventType), 
                            $"Event should have either '{ClientSecretExpirationEventType.ClientSecretAboutToExpire}' or '{ClientSecretExpirationEventType.ClientSecretExpired}' as event type");

                        var data = ev.GetPayload<AzureApplication>();
                        Assert.IsType<Guid>(data.KeyId);

                        bool isAboutToExpire = eventType == ClientSecretExpirationEventType.ClientSecretAboutToExpire;
                        bool isExpired = eventType == ClientSecretExpirationEventType.ClientSecretExpired;
                        
                        Assert.True(isAboutToExpire == (data.RemainingValidDays > 0 && data.RemainingValidDays < expirationThreshold), $"Remaining days should be between 1-{expirationThreshold - 1} when the secret is about to expire");
                        Assert.True(isExpired == data.RemainingValidDays < 0, "Remaining days should be negative when the secret is expired");
                    });
                }
            }
        }

        private IEventGridPublisher CreateEventGridPublisher()
        {
            var topicEndpoint = _config.GetValue<string>("Arcus:Infra:EventGrid:TopicUri");
            var topicEndpointSecret = _config.GetValue<string>("Arcus:Infra:EventGrid:AuthKey");

            IEventGridPublisher eventGridPublisher = 
                EventGridPublisherBuilder
                    .ForTopic(topicEndpoint)
                    .UsingAuthenticationKey(topicEndpointSecret)
                    .Build();

            return eventGridPublisher;
        }
    }
}
