using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.Tests.Integration.Hosting;
using Arcus.Security.Core;
using Arcus.Testing.Logging;
using Microsoft.Azure.Databricks.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Polly;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Arcus.BackgroundJobs.Tests.Integration.Jobs
{
    public class DatabricksJobMetricsJobTests : IAsyncLifetime
    {
        private readonly ITestOutputHelper _outputWriter;
        private readonly TestHost _host;
        private readonly TestConfig _config;
        private readonly InMemoryLogger _spyLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabricksJobMetricsJobTests"/> class.
        /// </summary>
        public DatabricksJobMetricsJobTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
            _config = TestConfig.Create();
            _spyLogger = new InMemoryLogger();
            _host = new TestHost(_config, ConfigureServices);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            var loggerProvider = new Mock<ILoggerProvider>();
            loggerProvider.Setup(p => p.CreateLogger(It.IsAny<string>()))
                          .Returns(_spyLogger);

            services.AddLogging(builder => builder.AddProvider(loggerProvider.Object));

            string baseUrl = GetDatabricksUrl();
            string token = GetDatabricksToken();
            string tokenSecretKey = "Databricks.Token";

            var secretProvider = new Mock<ISecretProvider>();
            secretProvider.Setup(p => p.GetRawSecretAsync(tokenSecretKey))
                          .ReturnsAsync(token);

            services.AddSingleton<ISecretProvider>(secretProvider.Object);
            services.AddDatabricksJobMetricsJob(baseUrl, tokenSecretKey, options => options.IntervalInMinutes = 1);
        }

        /// <summary>
        /// Called immediately after the class has been created, before it is used.
        /// </summary>
        public async Task InitializeAsync()
        {
            var host = _host.Services.GetService<IHost>();
            await host.StartAsync();
        }

        [Fact(Skip = "Databricks cluster is to expensive for testing")]
        public async Task FinishedDatabricksJobRun_GetsNoticedByRepeatedlyDatabricksJob_ReportsAsMetric()
        {
            // Arrange
            string baseUrl = GetDatabricksUrl();
            string token = GetDatabricksToken();

            using (var client = DatabricksClient.CreateClient(baseUrl, token))
            {
                var parameters = new[]
                {
                    new KeyValuePair<string, string>("RequestId", "1"),
                    new KeyValuePair<string, string>("SenderName", "ArcusSender"),
                    new KeyValuePair<string, string>("ThrowError", "False")
                };

                // Act
                await client.Jobs.RunNow(2, RunParameters.CreateNotebookParams(parameters));

                // Assert
                RetryAssertion(
                    () => Assert.Contains(_spyLogger.Messages, msg => msg.StartsWith("Metric Databricks Job Completed")),
                    timeout: TimeSpan.FromMinutes(3),
                    interval: TimeSpan.FromSeconds(1));
            }
        }

        private string GetDatabricksUrl()
        {
            string baseUrl = _config.GetValue<string>("Arcus:Databricks:Url");
            return baseUrl;
        }

        private string GetDatabricksToken()
        {
            string token = _config.GetValue<string>("Arcus:Databricks:Token");
            return token;
        }

        private static void RetryAssertion(Action assertion, TimeSpan timeout, TimeSpan interval)
        {
            Policy.Timeout(timeout)
                  .Wrap(Policy.Handle<ContainsException>()
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
