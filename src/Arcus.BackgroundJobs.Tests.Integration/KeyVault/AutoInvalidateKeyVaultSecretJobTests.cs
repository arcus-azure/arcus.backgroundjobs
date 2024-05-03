using System;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.Tests.Integration.Fixture;
using Arcus.BackgroundJobs.Tests.Integration.Hosting;
using Arcus.BackgroundJobs.Tests.Integration.KeyVault.Fixture;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Security.Core;
using Arcus.Security.Core.Caching;
using Arcus.Testing.Logging;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Arcus.BackgroundJobs.Tests.Integration.KeyVault
{
    [Trait(name: "Category", value: "Integration")]
    [Collection(TestCollections.Integration)]
    public class AutoInvalidateKeyVaultSecretJobTests
    {
        private const string ServiceBusTopicConnectionStringKey = "Arcus:KeyVault:SecretNewVersionCreated:ServiceBus:ConnectionStringWithTopic";

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
        public async Task NewSecretVersion_TriggersKeyVaultJobUsingManagedIdentity_AutoValidatesSecret()
        {
            // Arrange
            string vaultUri = _config.GetKeyVaultUri();
            const string secretKey = "Arcus:TestSecret";

            using (TemporaryManagedIdentityConnection.Create(_config))
            {
                var client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());
                string connectionString = _config[ServiceBusTopicConnectionStringKey];
                var spySecretProvider = new SpyCachedSecretProvider(secretKey, connectionString);
                var properties = ServiceBusConnectionStringProperties.Parse(connectionString);

                var options = new WorkerOptions();
                options.ConfigureLogging(_logger)
                       .ConfigureServices(services =>
                       {
                           services.AddSecretStore(stores => stores.AddProvider(spySecretProvider, configureOptions: null));
                           services.AddAutoInvalidateKeyVaultSecretUsingManagedIdentityBackgroundJob(
                               properties.EntityPath, 
                               subscriptionNamePrefix: "TestSub", 
                               properties.FullyQualifiedNamespace, 
                               opt => opt.TopicSubscription = TopicSubscription.Automatic);
                       });

                await using (await Worker.StartNewAsync(options))
                await using (var tempSecret = await TemporaryAzureKeyVaultSecret.CreateNewAsync(client))
                {
                    await tempSecret.UpdateSecretAsync(Guid.NewGuid().ToString());

                    RetryAssertion(
                        () => Assert.True(spySecretProvider.IsSecretInvalidated), 
                        timeout: TimeSpan.FromMinutes(8),
                        interval: TimeSpan.FromMilliseconds(500));
                }
            }
        }

        [Fact]
        public async Task NewSecretVersion_TriggersKeyVaultJob_AutoInvalidatesSecret()
        {
            // Arrange
            AzureEnvironment environment = _config.GetAzureEnvironment();
            ServicePrincipal servicePrincipal = _config.GetServicePrincipal();
            string keyVaultUri = _config.GetKeyVaultUri();
            var credential = new ClientSecretCredential(environment.TenantId, servicePrincipal.ClientId, servicePrincipal.ClientSecret);
            var secretValue = Guid.NewGuid().ToString("N");

            var client = new SecretClient(new Uri(keyVaultUri), credential);

            const string secretKey = "Arcus:TestSecret";
            var spySecretProvider = new SpyCachedSecretProvider(secretKey, _config[ServiceBusTopicConnectionStringKey]);
            
            var options = new WorkerOptions();
            options.ConfigureLogging(_logger)
                   .ConfigureServices(services =>
                   {
                       services.AddSingleton<ISecretProvider>(spySecretProvider)
                               .AddSingleton<ICachedSecretProvider>(spySecretProvider)
                               .AddAutoInvalidateKeyVaultSecretBackgroundJob(
                                   subscriptionNamePrefix: "TestSub",
                                   serviceBusTopicConnectionStringSecretKey: secretKey,
                                   opt => opt.TopicSubscription = TopicSubscription.Automatic);
                   });

            // Act
            await using (await Worker.StartNewAsync(options))
            await using (var tempSecret = await TemporaryAzureKeyVaultSecret.CreateNewAsync(client))
            {
                await tempSecret.UpdateSecretAsync(secretValue);

                // Assert
                RetryAssertion(
                    () => Assert.True(spySecretProvider.IsSecretInvalidated), 
                    timeout: TimeSpan.FromMinutes(8),
                    interval: TimeSpan.FromMilliseconds(500));
            }
        }

        private static void RetryAssertion(Action assertion, TimeSpan timeout, TimeSpan interval)
        {
            Policy.Timeout(timeout)
                  .Wrap(Policy.Handle<XunitException>()
                              .WaitAndRetryForever(_ => interval))
                  .Execute(assertion);
        }
    }
}
