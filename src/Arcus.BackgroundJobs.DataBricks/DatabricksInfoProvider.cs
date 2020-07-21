using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.Azure.Databricks.Client;

namespace Arcus.BackgroundJobs.Databricks
{
    /// <summary>
    /// Provides dev-friendly access to the Databricks instance.
    /// </summary>
    internal class DatabricksInfoProvider : IDisposable
    {
        private readonly DatabricksClient _databricksClient;

        /// <summary>
        /// Initializes a new instance of hte <see cref="DatabricksInfoProvider"/> class.
        /// </summary>
        /// <param name="databricksClient">The client to interact with Databricks.</param>
        internal DatabricksInfoProvider(DatabricksClient databricksClient)
        {
            Guard.NotNull(databricksClient, nameof(databricksClient));

            _databricksClient = databricksClient;
        }

        /// <summary>
        /// Get all job runs that were finished in a given time window
        /// </summary>
        /// <param name="startOfWindow">Start of time window which we are interested in. (Inclusive)</param>
        /// <param name="endOfWindow">End of time window which we are interested in. (Exclusive)</param>
        internal async Task<IEnumerable<(string jobName, Run jobRun)>> GetFinishedJobRunsAsync(DateTimeOffset startOfWindow, DateTimeOffset endOfWindow)
        {
            Guard.NotLessThan(endOfWindow, startOfWindow, nameof(endOfWindow), "Requires the date of the end window to be greater than the start date of the window");

            IEnumerable<Run> jobRuns = await GetJobRunsAsync(startOfWindow, endOfWindow);
            IEnumerable<Job> availableJobs = await _databricksClient.Jobs.List();

            return from finishedJobRun in jobRuns
                   where HasFinished(finishedJobRun)
                   let jobName = availableJobs.FirstOrDefault(job => job.JobId == finishedJobRun.JobId)?.Settings?.Name
                   select (jobName, finishedJobRun);
        }

        private static bool HasFinished(Run run)
        {
            return run.State?.ResultState != null;
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
                    DateTimeOffset? jobEndDate = GetEndDate(jobRun);
                    if (jobEndDate != null && jobEndDate > startOfWindow && jobEndDate < endOfWindow)
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

        private static DateTimeOffset? GetEndDate(Run run)
        {
            if (run.StartTime.HasValue == false)
            {
                return null;
            }

            /*
             * This is based on the documentation available here:
             * - https://github.com/Azure/azure-databricks-client/issues/24
             * - https://docs.microsoft.com/en-us/azure/databricks/dev-tools/api/latest/jobs?toc=https%3A%2F%2Fdocs.microsoft.com%2Fen-us%2Fazure%2Fazure-databricks%2FTOC.json&bc=https%3A%2F%2Fdocs.microsoft.com%2Fen-us%2Fazure%2Fbread%2Ftoc.json#--run
             * - This will come in upstream soon via https://github.com/Azure/azure-databricks-client/pull/28
             */

            DateTimeOffset runStartTime = run.StartTime.Value;

            TimeSpan setupDuration = TimeSpan.FromMilliseconds(run.SetupDuration);
            TimeSpan cleanupDuration = TimeSpan.FromMilliseconds(run.CleanupDuration);
            TimeSpan executionDuration = TimeSpan.FromMilliseconds(run.ExecutionDuration);
            TimeSpan jobExecution = setupDuration.Add(cleanupDuration).Add(executionDuration);

            return runStartTime.Add(jobExecution);
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