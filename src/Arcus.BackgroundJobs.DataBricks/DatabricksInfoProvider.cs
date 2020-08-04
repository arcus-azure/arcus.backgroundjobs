using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.Databricks.Extensions;
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
        ///  <para><see cref="Extensions.ILoggerExtensions.LogMetricFinishedJobOutcome"/>.</para>
        /// </para>
        /// </summary>
        /// <param name="metricName">The name of the logging metric.</param>
        /// <param name="startOfWindow">The start of time window which we are interested in. (Inclusive)</param>
        /// <param name="endOfWindow">The snd of time window which we are interested in. (Exclusive)</param>
        /// <seealso cref="GetFinishedJobRunsAsync"/>
        /// <seealso cref="Extensions.ILoggerExtensions.LogMetricFinishedJobOutcome"/>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="metricName"/> is blank.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="endOfWindow"/> is less than the <paramref name="startOfWindow"/>.</exception>
        public async Task MeasureJobOutcomesAsync(string metricName, DateTimeOffset startOfWindow, DateTimeOffset endOfWindow)
        {
            Guard.NotNullOrWhitespace(metricName, nameof(metricName));
            Guard.NotLessThan(endOfWindow, startOfWindow, nameof(endOfWindow), "Requires the date of the end window to be greater than the start date of the window");

            IEnumerable<JobRun> finishedJobRuns = await GetFinishedJobRunsAsync(startOfWindow, endOfWindow);
            foreach (JobRun finishedJob in finishedJobRuns)
            {
                _logger.LogMetricFinishedJobOutcome(metricName, finishedJob);
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
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _databricksClient?.Dispose();
        }
    }
}