using System;
using GuardNet;
using Microsoft.Extensions.DependencyInjection;

namespace Arcus.BackgroundJobs.Databricks
{
    /// <summary>
    /// Represents the additional options that the user can configure during the <see cref="IServiceCollectionExtensions.AddDatabricksJobMetricsJob"/> call.
    /// </summary>
    public class DatabricksJobMetricsJobOptions
    {
        private string _metricName = "Databricks Job Completed";
        private int _intervalInMinutes = 5;

        /// <summary>
        /// Gets or sets the name which will be used when reporting the metric for finished Databricks job runs.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is blank.</exception>
        public string MetricName
        {
            get => _metricName;
            set
            {
                Guard.NotNullOrWhitespace(value, nameof(value));
                _metricName = value;
            }
        }

        /// <summary>
        /// Gets or sets the interval (minutes) in which to query for Databricks finished job runs.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/> is less than zero or greater than 59.</exception>
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
