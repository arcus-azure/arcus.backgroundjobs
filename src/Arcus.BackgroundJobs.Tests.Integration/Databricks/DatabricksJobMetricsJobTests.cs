using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.Tests.Integration.Hosting;
using Arcus.Testing.Logging;
using Microsoft.Azure.Databricks.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Arcus.BackgroundJobs.Tests.Integration.Databricks
{
    public class DatabricksJobMetricsJobTests
    {
        private readonly ILogger _logger;
        private readonly TestConfig _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabricksJobMetricsJobTests"/> class.
        /// </summary>
        public DatabricksJobMetricsJobTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
            _config = TestConfig.Create();
        }

        [Fact]
        public async Task FinishedDatabricksJobRun_GetsNoticedByRepeatedlyDatabricksJob_ReportsAsMetric()
        {
            // Arrange
            var baseUrl = _config.GetValue<string>("Arcus:Databricks:Url");
            var token = _config.GetValue<string>("Arcus:Databricks:Token");

            var spyLogger = new InMemoryLogger();
            var tokenSecretKey = "Databricks.Token";
            var options = new WorkerOptions();
            options.ConfigureLogging(spyLogger);
            options.ConfigureLogging(_logger);
            options.ConfigureServices(services =>
            {
                services.AddSecretStore(stores => stores.AddInMemory(tokenSecretKey, token));
                services.AddDatabricksJobMetricsJob(baseUrl, tokenSecretKey, opt => opt.IntervalInMinutes = 1);
            });

            using (var client = DatabricksClient.CreateClient(baseUrl, token))
            {
                JobSettings settings = CreateEmptyJobSettings();
                long jobId = await client.Jobs.Create(settings);
                
                await using (var worker = await Worker.StartNewAsync(options))
                {
                    try
                    {
                        // Act
                        await client.Jobs.RunNow(jobId, RunParameters.CreateNotebookParams(Enumerable.Empty<KeyValuePair<string, string>>()));

                        // Assert
                        RetryAssertion(
                            () => Assert.Contains(spyLogger.Messages, msg => msg.StartsWith("Metric Databricks Job Completed")),
                            timeout: TimeSpan.FromMinutes(5),
                            interval: TimeSpan.FromSeconds(1));
                    }
                    finally
                    {
                        await client.Jobs.Delete(jobId);
                    }
                }
            }
        }

        private static JobSettings CreateEmptyJobSettings()
        {
            var settings = new JobSettings
            {
                Name = "(temp) Arcus Background Jobs - Integration Testing",
                NewCluster = new ClusterInfo
                {
                    RuntimeVersion = "8.3.x-scala2.12",
                    NodeTypeId = "Standard_DS3_v2",
                    SparkEnvironmentVariables = new Dictionary<string, string>
                    {
                        ["PYSPARK_PYTHON"] = "/databricks/python3/bin/python3"
                    },
                    EnableElasticDisk = true,
                    NumberOfWorkers = 8
                },
                MaxConcurrentRuns = 10,
                NotebookTask = new NotebookTask
                {
                    NotebookPath = "/Arcus - Automation"
                }
            };

            return settings;
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
