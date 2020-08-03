using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.Azure.Databricks.Client;
using Microsoft.Extensions.Logging;

namespace Arcus.BackgroundJobs.Databricks
{
    /// <summary>
    /// Provides dev-friendly access to the Databricks instance.
    /// </summary>
    public class DatabricksInfoProvider : IDisposable
    {
        private readonly DatabricksClient _databricksClient;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of hte <see cref="DatabricksInfoProvider"/> class.
        /// </summary>
        /// <param name="databricksClient">The client to interact with Databricks.</param>
        /// <param name="logger">The instance to log metric reports of job runs.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="databricksClient"/> or <paramref name="logger"/> is <c>null</c>.</exception>
        public DatabricksInfoProvider(DatabricksClient databricksClient, ILogger logger)
        {
            Guard.NotNull(databricksClient, nameof(databricksClient));
            Guard.NotNull(logger, nameof(logger));

            _databricksClient = databricksClient;
            _logger = logger;
        }

        /// <summary>
        /// <para>Measure the Databricks finished jobs by reporting the results as logging metrics.</para>
        /// <para>
        ///  Combines:
        ///  <para><see cref="GetFinishedJobRunsAsync"/> and </para>
        ///  <para><see cref="ReportFinishedJobOutcomeAsync"/>.</para>
        /// </para>
        /// </summary>
        /// <param name="metricName">The name of the logging metric.</param>
        /// <param name="startOfWindow">The start of time window which we are interested in. (Inclusive)</param>
        /// <param name="endOfWindow">The snd of time window which we are interested in. (Exclusive)</param>
        /// <seealso cref="GetFinishedJobRunsAsync"/>
        /// <seealso cref="ReportFinishedJobOutcomeAsync"/>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="metricName"/> is blank.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="endOfWindow"/> is less than the <paramref name="startOfWindow"/>.</exception>
        public async Task MeasureJobOutcomesAsync(string metricName, DateTimeOffset startOfWindow, DateTimeOffset endOfWindow)
        {
            Guard.NotNullOrWhitespace(metricName, nameof(metricName));
            Guard.NotLessThan(endOfWindow, startOfWindow, nameof(endOfWindow), "Requires the date of the end window to be greater than the start date of the window");

            IEnumerable<JobRun> finishedJobRuns = await GetFinishedJobRunsAsync(startOfWindow, endOfWindow);
            foreach (JobRun finishedJob in finishedJobRuns)
            {
                await ReportFinishedJobOutcomeAsync(metricName, finishedJob);
            }
        }

        /// <summary>
        /// Get all job runs that were finished in a given time window
        /// </summary>
        /// <param name="startOfWindow">The start of time window which we are interested in. (Inclusive)</param>
        /// <param name="endOfWindow">The snd of time window which we are interested in. (Exclusive)</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="endOfWindow"/> is less than the <paramref name="startOfWindow"/>.</exception>
        public async Task<IEnumerable<JobRun>> GetFinishedJobRunsAsync(DateTimeOffset startOfWindow, DateTimeOffset endOfWindow)
        {
            Guard.NotLessThan(endOfWindow, startOfWindow, nameof(endOfWindow), "Requires the date of the end window to be greater than the start date of the window");

            IEnumerable<Run> jobRuns = await GetJobRunsAsync(startOfWindow, endOfWindow);
            IEnumerable<Job> availableJobs = await _databricksClient.Jobs.List();

            return from finishedJobRun in jobRuns
                   where finishedJobRun.IsCompleted
                   let jobName = availableJobs.FirstOrDefault(job => job.JobId == finishedJobRun.JobId)?.Settings?.Name
                   select new JobRun(jobName, finishedJobRun);
        }

        private async Task<IEnumerable<Run>> GetJobRunsAsync(DateTimeOffset startOfWindow, DateTimeOffset endOfWindow)
        {
            var finishedJobs = new Collection<Run>();
            bool hasMoreInformation;
            var runOffset = 0;

            do
            {
                RunList jobHistory = await _databricksClient.Jobs.RunsList(offset: runOffset);
                if (jobHistory == null)
                {
                    break;
                }

                foreach (Run jobRun in jobHistory.Runs)
                {
                    if (jobRun.EndTime != null && jobRun.EndTime > startOfWindow && jobRun.EndTime < endOfWindow)
                    {
                        finishedJobs.Add(jobRun);
                    }
                    else
                    {
                        return finishedJobs;
                    }
                }

                hasMoreInformation = jobHistory.HasMore;
                runOffset += jobHistory.Runs.Count();
            } while (hasMoreInformation);

            return finishedJobs;
        }

        /// <summary>
        /// Report a given <paramref name="jobRun"/> as a logging metric with the specified <paramref name="metricName"/>.
        /// </summary>
        /// <param name="metricName">The name of the logging metric.</param>
        /// <param name="jobRun">The instance to report.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="metricName"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="jobRun"/> is <c>null</c>.</exception>
        public Task ReportFinishedJobOutcomeAsync(string metricName, JobRun jobRun)
        {
            Guard.NotNullOrWhitespace(metricName, nameof(metricName));
            Guard.NotNull(jobRun, nameof(jobRun));

            TextInfo text = new CultureInfo("en-US", useUserOverride: false).TextInfo;
            _logger.LogInformation("Found finished job run with ID {RunId}", jobRun.Run.RunId);

            RunResultState? resultState = jobRun.Run.State.ResultState;
            if (resultState is null)
            {
                _logger.LogWarning("Cannot find result state of finished job run with ID {RunId}", jobRun.Run.RunId);
            }
            else
            {
                string outcome = text.ToLower(resultState.ToString());

                _logger.LogMetric(metricName, value: 1, context: new Dictionary<string, object>
                {
                    { "Run Id", jobRun.Run.RunId },
                    { "Job Id", jobRun.Run.JobId },
                    { "Job Name", jobRun.JobName },
                    { "Outcome", outcome }
                });
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _databricksClient?.Dispose();
        }
    }
}