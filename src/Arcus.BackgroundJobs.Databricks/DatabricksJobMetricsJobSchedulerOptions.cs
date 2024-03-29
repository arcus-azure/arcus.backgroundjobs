﻿using System;
using System.Threading.Tasks;
using Arcus.Security.Core;
using CronScheduler.Extensions.Scheduler;
using GuardNet;
using Microsoft.Azure.Databricks.Client;

namespace Arcus.BackgroundJobs.Databricks
{
    /// <summary>
    /// Options to configure how the <see cref="DatabricksJobMetricsJob"/> scheduled job.
    /// </summary>
    public class DatabricksJobMetricsJobSchedulerOptions : SchedulerOptions
    {
        private string _baseUrl, _tokenSecretKey;
        private DateTimeOffset _last = DateTime.UtcNow;

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
        /// Gets or sets the secret key to retrieve the token from the registered Arcus secret store.
        /// </summary>
        /// <remarks>
        ///     Make sure that the application has the Arcus secret store configured correctly.
        ///     For on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
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
        /// Gets the additional user options which configures the <see cref="DatabricksJobMetricsJob"/> scheduled job.
        /// </summary>
        public DatabricksJobMetricsJobOptions UserOptions { get; private set; } = new DatabricksJobMetricsJobOptions();

        /// <summary>
        /// Sets the additional user options in a <see cref="SchedulerOptions"/> context.
        /// </summary>
        /// <param name="options">The additional user-options to set.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> is <c>null</c>.</exception>
        internal void SetUserOptions(DatabricksJobMetricsJobOptions options)
        {
            Guard.NotNull(options, nameof(options));

            UserOptions = options;
            CronSchedule = options.IntervalInMinutes == 1 ? "@every_minute" : $"*/{options.IntervalInMinutes} * * * *";
        }

        /// <summary>
        /// Creates an <see cref="DatabricksClient"/> instance using the predefined values.
        /// </summary>
        /// <remarks>
        ///     Make sure that the application has the Arcus secret store configured correctly.
        ///     For on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
        /// <param name="secretProvider">The provider to retrieve the token during the creation of the instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="secretProvider"/> is <c>null</c>.</exception>
        public virtual async Task<DatabricksClient> CreateDatabricksClientAsync(ISecretProvider secretProvider)
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

        /// <summary>
        /// Determining the next time window in which the job runs should be retrieved.
        /// </summary>
        public virtual (DateTimeOffset last, DateTimeOffset next) DetermineNextTimeWindow()
        {
            DateTimeOffset next = DateTimeOffset.UtcNow;

            (DateTimeOffset _last, DateTimeOffset next) result = (_last, next);
            _last = next;

            return result;
        }
    }
}
