using System;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.AzureActiveDirectory;
using Arcus.BackgroundJobs.Tests.Integration.AzureActiveDirectory.Fixture;
using Arcus.BackgroundJobs.Tests.Integration.Hosting;
using Arcus.EventGrid;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Publishing;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.Security.Core;
using Arcus.Testing.Logging;
using CloudNative.CloudEvents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
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
            var topicEndpoint = _config.GetValue<string>("Arcus:Infra:EventGrid:TopicUri");
            var topicEndpointSecret = _config.GetValue<string>("Arcus:Infra:EventGrid:AuthKey");
            var topicEndpointSecretKey = "Infra.EventGrid.AuthKey";

            var secretProvider = new Mock<ISecretProvider>();
            secretProvider.Setup(p => p.GetRawSecretAsync(topicEndpointSecretKey))
                          .ReturnsAsync(topicEndpointSecret);

            IEventGridPublisher eventGridPublisher = EventGridPublisherBuilder
                .ForTopic(topicEndpoint)
                .UsingAuthenticationKey(topicEndpointSecret)
                .Build();

            var options = new WorkerOptions();
            options.ConfigureLogging(_logger)
                   .AddSingleton<ISecretProvider>(secretProvider.Object)
                   .AddSingleton<IEventGridPublisher>(eventGridPublisher)
                   .AddClientSecretExpirationJob(
                        options => {
                            options.RunImmediately = true;
                            options.ExpirationThreshold = 14;
                            options.EventUri = new Uri("https://github.com/arcus-azure/arcus.backgroundjobs");
                        });

            // Act
            await using (var worker = await Worker.StartNewAsync(options))
            {
                await using (var consumer = await TestServiceBusEventConsumer.StartNewAsync(_config, _logger))
                {
                    // Assert
                    EventBatch<Event> eventBatch = consumer.Consume();
                    Event @event = Assert.Single(eventBatch.Events);
                    CloudEvent cloudEvent = @event.AsCloudEvent();
                    Assert.NotNull(cloudEvent.Id);
                    Assert.True(cloudEvent.Type == ClientSecretExpirationEventType.ClientSecretAboutToExpire.ToString() || cloudEvent.Type == ClientSecretExpirationEventType.ClientSecretExpired.ToString());

                    var cloudEventData = cloudEvent.GetPayload<ApplicationWithExpiredAndAboutToExpireSecrets>();
                    Assert.IsType<Guid>(cloudEventData.KeyId);
                    
                    if (cloudEvent.Type == ClientSecretExpirationEventType.ClientSecretAboutToExpire.ToString())
                    {
                        Assert.True(cloudEventData.RemainingValidDays > 0 && cloudEventData.RemainingValidDays < 14);
                    }
                    else if (cloudEvent.Type == ClientSecretExpirationEventType.ClientSecretExpired.ToString())
                    {
                        Assert.True(cloudEventData.RemainingValidDays < 0);
                    }
                }
            }
        }
    }
}
