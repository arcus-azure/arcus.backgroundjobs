using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Security.Core;
using CronScheduler.Extensions.Scheduler;
using GuardNet;
using Microsoft.Azure.Databricks.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arcus.BackgroundJobs.Databricks
{
    /// <summary>
    /// Representing a background job that repeatedly queries the Databricks instance for finished job runs and writes the report as a metric.
    /// </summary>
    public class DatabricksJobMetrics : IScheduledJob
    {
        private readonly DatabricksJobMetricsSchedulerOptions _options;
        private readonly ISecretProvider _secretProvider;
        private readonly ILogger<DatabricksJobMetrics> _logger;

        private DateTimeOffset _last = DateTime.UtcNow;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabricksJobMetrics"/> class.
        /// </summary>
        /// <param name="options">The options to configure the job to query the Databricks report.</param>
        /// <param name="secretProvider">The instance to provide the token to authenticate with Databricks.</param>
        /// <param name="logger">The logger instance to to write telemetry to.</param>
        public DatabricksJobMetrics(
            IOptionsMonitor<DatabricksJobMetricsSchedulerOptions> options,
            ISecretProvider secretProvider,
            ILogger<DatabricksJobMetrics> logger)
        {
            Guard.NotNull(options, nameof(options));
            Guard.NotNull(secretProvider, nameof(secretProvider));
            Guard.NotNull(logger, nameof(logger));

            DatabricksJobMetricsSchedulerOptions value = options.Get(Name);
            Guard.NotNull(options, nameof(options), "Requires a registered options instance for this background job");

            _options = value;
            _secretProvider = secretProvider;
            _logger = logger;
        }

        /// <summary>
        /// The name of the executing job.
        /// In order for the <see cref="T:CronScheduler.Extensions.Scheduler.SchedulerOptions" /> options to work correctly make sure that the name is matched
        /// between the job and the named job options.
        /// </summary>
        public string Name { get; } = nameof(DatabricksJobMetrics);

        /// <summary>
        /// This method is called when the <see cref="T:Microsoft.Extensions.Hosting.IHostedService" /> starts. The implementation should return a task that represents
        /// the lifetime of the long running operation(s) being performed.
        /// </summary>
        /// <param name="stoppingToken">Triggered when <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StopAsync(System.Threading.CancellationToken)" /> is called.</param>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the long running operations.</returns>
        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (DatabricksClient client = await _options.CreateDatabricksClientAsync(_secretProvider))
            using (var databricksInfoProvider = new DatabricksInfoProvider(client))
            {
                DateTimeOffset next = DateTimeOffset.UtcNow;
                _logger.LogInformation(
                    "Job monitor for Databricks is starting at {TriggerTime} for time windows {WindowStart} - {WindowEnd}",
                    DateTimeOffset.UtcNow, _last, next);

                IEnumerable<JobRun> jobRunHistory = await databricksInfoProvider.GetFinishedJobRunsAsync(_last, next);
                foreach (JobRun jobRun in jobRunHistory)
                {
                    ReportJobRunAsMetric(jobRun);
                }

                _last = next;
            }
        }

        private void ReportJobRunAsMetric(JobRun jobRun)
        {
            TextInfo text = new CultureInfo("en-US", useUserOverride: false).TextInfo;
            _logger.LogInformation("Found finished job run with id {RunId}", jobRun.Run.RunId);

            RunResultState resultState = jobRun.Run.State.ResultState ?? default(RunResultState);
            string outcome = text.ToLower(resultState.ToString());

            _logger.LogMetric("Databricks Job Completed", value: _options.MetricValue, context: new Dictionary<string, object> 
            {
                { "Run Id", jobRun.Run.RunId },
                { "Job Id", jobRun.Run.JobId },
                { "Job Name", jobRun.Name },
                { "Outcome", outcome }
            });
        }
    }
}
