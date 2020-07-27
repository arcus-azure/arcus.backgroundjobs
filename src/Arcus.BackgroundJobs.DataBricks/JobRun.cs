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
        public JobRun(string name, Run run)
        {
            Guard.NotNullOrWhitespace(name, nameof(name));
            Guard.NotNull(run, nameof(run));

            Name = name;
            Run = run;
        }

        /// <summary>
        /// Gets the name of the job that was ran.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the Databricks run that ran during the job.
        /// </summary>
        public Run Run { get; }
    }
}