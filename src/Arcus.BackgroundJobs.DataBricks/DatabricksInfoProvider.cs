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
    public class DatabricksInfoProvider : IDisposable
    {
        private readonly DatabricksClient _databricksClient;

        /// <summary>
        /// Initializes a new instance of hte <see cref="DatabricksInfoProvider"/> class.
        /// </summary>
        /// <param name="databricksClient">The client to interact with Databricks.</param>
        public DatabricksInfoProvider(DatabricksClient databricksClient)
        {
            Guard.NotNull(databricksClient, nameof(databricksClient));

            _databricksClient = databricksClient;
        }

        /// <summary>
        /// Get all job runs that were finished in a given time window
        /// </summary>
        /// <param name="startOfWindow">Start of time window which we are interested in. (Inclusive)</param>
        /// <param name="endOfWindow">End of time window which we are interested in. (Exclusive)</param>
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