using GuardNet;
using Microsoft.Extensions.DependencyInjection;

namespace Arcus.BackgroundJobs.Databricks
{
    /// <summary>
    /// Represents the additional options that the user can configure during the <see cref="IServiceCollectionExtensions.AddDatabricksJobMetricsJob"/> call.
    /// </summary>
    public class DatabricksJobMetricsJobOptions
    {
        private int _intervalInMinutes = 5;

        /// <summary>
        /// Gets or sets the name which will be used when reporting the metric for finished Databricks job runs.
        /// </summary>
        public string MetricName { get; set; } = "Databricks Job Completed";

        /// <summary>
        /// Gets or sets the interval (minutes) in which to query for Databricks finished job runs.
        /// </summary>
        public int IntervalInMinutes
        {
            get => _intervalInMinutes;
            set
            {
                Guard.NotLessThan(value, 0, nameof(value));
                Guard.NotGreaterThan(value, 59, nameof(value));

                _intervalInMinutes = value;
            }
        }
    }
}