using System;
using System.Threading.Tasks;
using Arcus.Security.Core;
using CronScheduler.Extensions.Scheduler;
using GuardNet;
using Microsoft.Azure.Databricks.Client;

namespace Arcus.BackgroundJobs.Databricks
{
    /// <summary>
    /// Options to configure how the <see cref="DatabricksJobMetrics"/> background job.
    /// </summary>
    public class DatabricksJobMetricsSchedulerOptions : SchedulerOptions
    {
        private string _baseUrl, _tokenSecretKey;

        /// <summary>
        /// Gets or sets the URL of the Databricks location.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is blank.</exception>
        public string BaseUrl
        {
            get => _baseUrl;
            set
            {
                Guard.NotNullOrWhitespace(value, nameof(value));
                _baseUrl = value;
            }
        }

        /// <summary>
        /// Gets or sets the secret key to retrieve the token from the registered <see cref="ISecretProvider"/>.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is blank.</exception>
        public string TokenSecretKey
        {
            get => _tokenSecretKey;
            set
            {
                Guard.NotNullOrWhitespace(value, nameof(value));
                _tokenSecretKey = value;
            }
        }

        /// <summary>
        /// Gets or sets the value which will be used when reporting the metric for finished Databricks job runs.
        /// </summary>
        public double MetricValue { get; set; }

        /// <summary>
        /// Sets the additional user options in a <see cref="SchedulerOptions"/> context.
        /// </summary>
        /// <param name="options">The additional user-options to set.</param>
        internal void SetAdditionalOptions(DatabricksJobMetricsAdditionalOptions options)
        {
            Guard.NotNull(options, nameof(options));

            MetricValue = options.MetricValue;
            CronSchedule = options.IntervalInMinutes == 1 ? "@every_minute" : $"*/{options.IntervalInMinutes} * * * *";
        }

        /// <summary>
        /// Creates an <see cref="DatabricksClient"/> instance using the predefined values.
        /// </summary>
        /// <param name="secretProvider">The provider to retrieve the token during the creation of the instance.</param>
        internal async Task<DatabricksClient> CreateDatabricksClientAsync(ISecretProvider secretProvider)
        {
            Guard.NotNull(secretProvider, nameof(secretProvider));

            if (String.IsNullOrWhiteSpace(_baseUrl))
            {
                throw new InvalidOperationException($"Databricks options are not correctly configured: requires a {nameof(BaseUrl)} that points to the Databricks location");
            }

            if (String.IsNullOrWhiteSpace(_tokenSecretKey))
            {
                throw new InvalidOperationException(
                    $"Databricks options are not correctly configured: requires a {nameof(TokenSecretKey)} to retrieve the token from the registered {nameof(ISecretProvider)} "
                    + "to authenticate to the Databricks instance");
            }

            string token = await secretProvider.GetRawSecretAsync(_tokenSecretKey);
            return DatabricksClient.CreateClient(_baseUrl, token);
        }
    }
}
