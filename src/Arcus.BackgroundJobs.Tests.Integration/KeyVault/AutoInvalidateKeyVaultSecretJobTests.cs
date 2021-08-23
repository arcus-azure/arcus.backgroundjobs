using System;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.Tests.Integration.Hosting;
using Arcus.BackgroundJobs.Tests.Integration.KeyVault.Fixture;
using Arcus.Security.Core;
using Arcus.Security.Core.Caching;
using Arcus.Testing.Logging;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Polly;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.BackgroundJobs.Tests.Integration.KeyVault
{
    [Trait(name: "Category", value: "Integration")]
    public class AutoInvalidateKeyVaultSecretJobTests
    {
        private readonly ILogger _logger;
        private readonly TestConfig _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoInvalidateKeyVaultSecretJobTests"/> class.
        /// </summary>
        public AutoInvalidateKeyVaultSecretJobTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
            _config = TestConfig.Create();
        }

        [Fact]
        public async Task NewSecretVersion_TriggersKeyVaultJob_AutoInvalidatesSecret()
        {
            // Arrange
            var tenantId = _config.GetValue<string>("Arcus:KeyRotation:ServiceBus:TenantId");
            var applicationId = _config.GetValue<string>("Arcus:ServicePrincipal:ApplicationId");
            var clientKey = _config.GetValue<string>("Arcus:ServicePrincipal:AccessKey");
            var keyVaultUri = _config.GetValue<string>("Arcus:KeyVault:Uri");
            var credential = new ClientSecretCredential(tenantId, applicationId, clientKey);
            var secretValue = Guid.NewGuid().ToString("N");

            var client = new SecretClient(new Uri(keyVaultUri), credential);

            const string secretKey = "Arcus:ServiceBus:ConnectionStringWithTopic";
            var cachedSecretProvider = new Mock<ICachedSecretProvider>();
            cachedSecretProvider
                .Setup(p => p.GetRawSecretAsync(secretKey))
                .ReturnsAsync(() => _config[secretKey]);

            cachedSecretProvider
                .Setup(p => p.InvalidateSecretAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            
            var options = new WorkerOptions();
            options.ConfigureLogging(_logger)
                   .AddSingleton<ISecretProvider>(cachedSecretProvider.Object)
                   .AddSingleton<ICachedSecretProvider>(cachedSecretProvider.Object)
                   .AddAutoInvalidateKeyVaultSecretBackgroundJob(
                       subscriptionNamePrefix: "TestSub",
                       serviceBusTopicConnectionStringSecretKey: secretKey);
            
            // Act
            await using (var worker = await Worker.StartNewAsync(options))
            await using (var tempSecret = await TemporaryAzureKeyVaultSecret.CreateNewAsync(client))
            {
                await tempSecret.UpdateSecretAsync(secretValue);

                // Assert
                RetryAssertion(
                    // ReSharper disable once AccessToDisposedClosure - disposal happens after retry.
                    () => cachedSecretProvider.Verify(p => p.InvalidateSecretAsync(It.Is<string>(n => n == tempSecret.Name)), Times.Once), 
                    timeout: TimeSpan.FromMinutes(5),
                    interval: TimeSpan.FromMilliseconds(500));
            }
        }

        private static void RetryAssertion(Action assertion, TimeSpan timeout, TimeSpan interval)
        {
            Policy.Timeout(timeout)
                  .Wrap(Policy.Handle<MockException>()
                              .WaitAndRetryForever(_ => interval))
                  .Execute(assertion);
        }
    }
}
