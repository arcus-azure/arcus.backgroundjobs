using System;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.AzureActiveDirectory;
using Arcus.BackgroundJobs.Tests.Integration.AzureActiveDirectory.Fixture;
using Arcus.BackgroundJobs.Tests.Integration.Fixture;
using Arcus.BackgroundJobs.Tests.Integration.Hosting;
using Arcus.Testing.Logging;
using CloudNative.CloudEvents;
using Microsoft.Extensions.DependencyInjection;
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
            AzureActiveDirectoryConfig activeDirectoryConfig = _config.GetActiveDirectoryConfig();
            using var connection = TemporaryManagedIdentityConnection.Create(activeDirectoryConfig);

            int expirationThreshold = 14;
            var options = new WorkerOptions();
            options.ConfigureLogging(_logger)
                   .ConfigureServices(services =>
                   {
                       services.AddClientSecretExpirationJob(opt =>
                       {
                           opt.RunImmediately = true;
                           opt.ExpirationThreshold = expirationThreshold;
                           opt.EventUri = new Uri("https://github.com/arcus-azure/arcus.backgroundjobs");
                       });

                       services.AddCorrelation();
                       services.AddEventGridPublisher(_config);
                   });

            await using var consumer = await TestServiceBusEventConsumer.StartNewAsync(_config, _logger);
            
            // Act
            await using var worker = await Worker.StartNewAsync(options);

            // Assert
            CloudEvent cloudEvent = consumer.Consume();
            AssertCloudSecretExpirationEvent(cloudEvent, expirationThreshold);
        }

        private static void AssertCloudSecretExpirationEvent(CloudEvent cloudEvent, int expirationThreshold)
        {
            Assert.NotNull(cloudEvent.Id);
            Assert.True(Enum.TryParse(cloudEvent.Type, out ClientSecretExpirationEventType eventType),
                $"Event should have either '{ClientSecretExpirationEventType.ClientSecretAboutToExpire}' or '{ClientSecretExpirationEventType.ClientSecretExpired}' as event type");

            var data = cloudEvent.GetPayload<AzureApplication>();
            Assert.IsType<Guid>(data.KeyId);

            bool isAboutToExpire = eventType == ClientSecretExpirationEventType.ClientSecretAboutToExpire;
            bool isExpired = eventType == ClientSecretExpirationEventType.ClientSecretExpired;

            Assert.True(isAboutToExpire == (data.RemainingValidDays > 0 && data.RemainingValidDays < expirationThreshold),
                $"Remaining days should be between 1-{expirationThreshold - 1} when the secret is about to expire");
            
            Assert.True(isExpired == data.RemainingValidDays < 0,
                "Remaining days should be negative when the secret is expired");
        }
    }
}
