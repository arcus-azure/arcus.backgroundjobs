using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Pumps.Abstractions;
using Arcus.Security.Core;
using GuardNet;
using Microsoft.Azure.Databricks.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arcus.BackgroundJobs.DataBricks
{
    /// <summary>
    /// Representing a background job that repeatedly queries the Databricks instance for finished job runs and writes the report as a metric.
    /// </summary>
    public class DatabricksJobMetrics : MessagePump
    {
        private readonly DatabricksJobMetricsOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabricksJobMetrics"/> class.
        /// </summary>
        /// <param name="options">The options to configure the job to query the Data Bricks report.</param>
        /// <param name="configuration">Configuration of the application</param>
        /// <param name="serviceProvider">Collection of services that are configured</param>
        /// <param name="logger">Logger to write telemetry to</param>
        public DatabricksJobMetrics(
            DatabricksJobMetricsOptions options,
            IConfiguration configuration,
            IServiceProvider serviceProvider,
            ILogger logger) : base(configuration, serviceProvider, logger)
        {
            Guard.NotNull(options, nameof(options));

            _options = options;
        }

        /// <summary>
        /// This method is called when the <see cref="T:Microsoft.Extensions.Hosting.IHostedService" /> starts. The implementation should return a task that represents
        /// the lifetime of the long running operation(s) being performed.
        /// </summary>
        /// <param name="stoppingToken">Triggered when <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StopAsync(System.Threading.CancellationToken)" /> is called.</param>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the long running operations.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var secretProvider = ServiceProvider.GetRequiredService<ISecretProvider>();

            using (DatabricksClient client = await _options.CreateDatabricksClientAsync(secretProvider))
            using (var databricksInfoProvider = new DatabricksInfoProvider(client))
            {
                DateTimeOffset last = DateTime.UtcNow;
                while (!stoppingToken.IsCancellationRequested)
                {
                    DateTimeOffset next = DateTimeOffset.UtcNow;
                    Logger.LogInformation(
                        "Job monitor for Databricks is starting at {TriggerTime} for time windows {WindowStart} - {WindowEnd}",
                        DateTimeOffset.UtcNow, last, next);

                    IEnumerable<(string jobName, Run jobRun)> jobRunHistory = await databricksInfoProvider.GetFinishedJobRunsAsync(last, next);
                    foreach ((string jobName, Run run) in jobRunHistory)
                    {
                        ReportJobRunAsMetric(jobName, run);
                    }

                    last = next;
                    await Task.Delay(_options.Interval, stoppingToken);
                }
            }
        }

        private void ReportJobRunAsMetric(string jobName, Run run)
        {
            TextInfo text = new CultureInfo("en-US", useUserOverride: false).TextInfo;
            Logger.LogInformation("Found finished job run with id {RunId}", run.RunId);

            RunResultState resultState = run.State.ResultState ?? default(RunResultState);
            string outcome = text.ToLower(resultState.ToString());

            Logger.LogMetric("Databricks Job Completed", value: 1, context: new Dictionary<string, object> 
            {
                { "Run Id", run.RunId },
                { "Job Id", run.JobId },
                { "Job Name", jobName },
                { "Outcome", outcome }
            });
        }
    }
}
