using System;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.Tests.Integration.Hosting;
using Arcus.Security.Core;
using Arcus.Security.Core.Caching;
using Arcus.Security.Providers.AzureKeyVault.Authentication;
using Arcus.Testing.Logging;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Polly;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.BackgroundJobs.Tests.Integration.Jobs
{
    [Trait(name: "Category", value: "Integration")]
    public class AutoInvalidateKeyVaultSecretJobTests : IAsyncLifetime
    {
        private readonly ITestOutputHelper _outputWriter;
        private readonly TestConfig _config;
        private readonly TestHost _host;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoInvalidateKeyVaultSecretJobTests"/> class.
        /// </summary>
        public AutoInvalidateKeyVaultSecretJobTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
            _config = TestConfig.Create();
            _host = new TestHost(_config, ConfigureServices);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            const string secretKey = "Arcus:ServiceBus:ConnectionStringWithTopic";

            var cachedSecretProvider = new Mock<ICachedSecretProvider>();
            cachedSecretProvider
                .Setup(p => p.GetRawSecretAsync(secretKey))
                .ReturnsAsync(() => _config[secretKey]);

            cachedSecretProvider
                .Setup(p => p.InvalidateSecretAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            services.AddSingleton<ILogger>(new XunitTestLogger(_outputWriter));
            services.AddLogging(builder => builder.AddConsole());
            services.AddSingleton<ISecretProvider>(cachedSecretProvider.Object);
            services.AddSingleton<ICachedSecretProvider>(cachedSecretProvider.Object);
            services.AddAutoInvalidateKeyVaultSecretBackgroundJob(
                subscriptionNamePrefix: "TestSub",
                serviceBusTopicConnectionStringSecretKey: secretKey);
        }

        /// <summary>
        /// Called immediately after the class has been created, before it is used.
        /// </summary>
        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        [Fact(Skip = "Problem with Azure Key Vault events firing")]
        public async Task NewSecretVersion_TriggersKeyVaultJob_AutoInvalidatesSecret()
        {
            // Arrange
            var applicationId = _config.GetValue<string>("Arcus:ServicePrincipal:ApplicationId");
            var clientKey = _config.GetValue<string>("Arcus:ServicePrincipal:AccessKey");
            var keyVaultUri = _config.GetValue<string>("Arcus:KeyVault:Uri");
            var authentication = new ServicePrincipalAuthentication(applicationId, clientKey);
            var cachedSecretProvider = _host.Services.GetService<ICachedSecretProvider>();
            var secretValue = Guid.NewGuid().ToString("N");

            using (IKeyVaultClient client = await authentication.AuthenticateAsync()) 
            // Act
            await using (var tempSecret = await TemporaryAzureKeyVaultSecret.CreateNewAsync(client, keyVaultUri))
            {
                await tempSecret.UpdateSecretAsync(secretValue);

                // Assert
                RetryAssertion(
                    // ReSharper disable once AccessToDisposedClosure - disposal happens after retry.
                    () => Mock.Get(cachedSecretProvider)
                              .Verify(p => p.InvalidateSecretAsync(It.Is<string>(n => n == tempSecret.Name)), Times.Once), 
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

        /// <summary>
        /// Called when an object is no longer needed. Called just before <see cref="M:System.IDisposable.Dispose" />
        /// if the class also implements that.
        /// </summary>
        public async Task DisposeAsync()
        {
            var host = _host.Services.GetService<IHost>();
            await host.StopAsync();

            _host?.Dispose();
        }
    }
}
