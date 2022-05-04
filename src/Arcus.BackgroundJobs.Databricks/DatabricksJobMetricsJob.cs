using System;
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
    public class DatabricksJobMetricsJob : IScheduledJob
    {
        private readonly DatabricksJobMetricsJobSchedulerOptions _options;
        private readonly ISecretProvider _secretProvider;
        private readonly ILogger<DatabricksJobMetricsJob> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabricksJobMetricsJob"/> class.
        /// </summary>
        /// <param name="options">The options to configure the job to query the Databricks report.</param>
        /// <param name="secretProvider">The instance to provide the token to authenticate with Databricks.</param>
        /// <param name="logger">The logger instance to to write telemetry to.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="options"/>, <paramref name="secretProvider"/>, <paramref name="logger"/> is <c>null</c>
        ///     or the <see cref="IOptionsMonitor{TOptions}.Get"/> on the  <paramref name="options"/> returns <c>null</c>.
        /// </exception>
        public DatabricksJobMetricsJob(
            IOptionsMonitor<DatabricksJobMetricsJobSchedulerOptions> options,
            ISecretProvider secretProvider,
            ILogger<DatabricksJobMetricsJob> logger)
        {
            Guard.NotNull(options, nameof(options));
            Guard.NotNull(secretProvider, nameof(secretProvider));
            Guard.NotNull(logger, nameof(logger));

            DatabricksJobMetricsJobSchedulerOptions value = options.Get(Name);
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
        public string Name { get; } = nameof(DatabricksJobMetricsJob);

        /// <summary>
        /// This method is called when the <see cref="T:Microsoft.Extensions.Hosting.IHostedService" /> starts. The implementation should return a task that represents
        /// the lifetime of the long running operation(s) being performed.
        /// </summary>
        /// <param name="stoppingToken">
        ///     Triggered when <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StopAsync(System.Threading.CancellationToken)" /> is called.
        /// </param>
        /// <returns>
        ///     A <see cref="T:System.Threading.Tasks.Task" /> that represents the long running operations.
        /// </returns>
        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                using (DatabricksClient client = await _options.CreateDatabricksClientAsync(_secretProvider))
                using (var databricksInfoProvider = new DatabricksInfoProvider(client, _logger))
                {
                    (DateTimeOffset start, DateTimeOffset end) = _options.DetermineNextTimeWindow();

                    _logger.LogInformation(
                        "Job monitor for Databricks is starting at {TriggerTime} for time windows {WindowStart} - {WindowEnd}",
                        DateTimeOffset.UtcNow, start, end);

                    string metricName = _options.UserOptions.MetricName;
                    await databricksInfoProvider.MeasureJobOutcomesAsync(metricName, start, end);
                }
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Could not measure the finished Databricks jobs due to an exception");
            }
        }
    }
}
