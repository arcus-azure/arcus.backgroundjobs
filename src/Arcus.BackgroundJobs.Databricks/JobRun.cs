using System;
using GuardNet;
using Microsoft.Azure.Databricks.Client;

namespace Arcus.BackgroundJobs.Databricks
{
    /// <summary>
    /// Represents a Databricks <see cref="Run"/> in a Databricks job.
    /// </summary>
    public class JobRun
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JobRun"/> class.
        /// </summary>
        /// <param name="jobName">The name of the job that was ran.</param>
        /// <param name="run">The Databricks run that ran during the job.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="jobName"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="run"/> is <c>null</c>.</exception>
        public JobRun(string jobName, Run run)
        {
            Guard.NotNullOrWhitespace(jobName, nameof(jobName));
            Guard.NotNull(run, nameof(run));

            JobName = jobName;
            Run = run;
        }

        /// <summary>
        /// Gets the name of the job that was ran.
        /// </summary>
        public string JobName { get; }

        /// <summary>
        /// Gets the Databricks run that ran during the job.
        /// </summary>
        public Run Run { get; }
    }
}
