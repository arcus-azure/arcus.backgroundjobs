using GuardNet;
using Microsoft.Extensions.DependencyInjection;

namespace Arcus.BackgroundJobs.Databricks
{
    /// <summary>
    /// Represents the additional options that the user can configure during the <see cref="IServiceCollectionExtensions.AddDatabricksJobMetrics"/> call.
    /// </summary>
    public class DatabricksJobMetricsAdditionalOptions
    {
        private int _intervalInMinutes;

        /// <summary>
        /// Gets or sets the value which will be used when reporting the metric for finished Databricks job runs.
        /// </summary>
        public double MetricValue { get; set; } = 1;

        /// <summary>
        /// Gets or sets the interval (minutes) in which to query for Databricks finished job runs.
        /// </summary>
        public int IntervalInMinutes
        {
            get => _intervalInMinutes;
            set
            {
                Guard.NotLessThan(value, 0, nameof(value));
                _intervalInMinutes = value;
            }
        }
    }
}