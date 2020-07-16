using System;
using System.Threading.Tasks;
using Arcus.Security.Core;
using GuardNet;
using Microsoft.Azure.Databricks.Client;

namespace Arcus.BackgroundJobs.DataBricks
{
    /// <summary>
    /// Options to configure how the <see cref="QueryRepeatedlyDatabricksReportJob"/> background job.
    /// </summary>
    public class QueryRepeatedlyDatabricksReportJobOptions
    {
        private readonly Func<ISecretProvider, Task<DatabricksClient>> _createDataBricksClientAsync;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryRepeatedlyDatabricksReportJobOptions"/> class.
        /// </summary>
        /// <param name="baseUrl">The URL of the Databricks location.</param>
        /// <param name="tokenSecretKey">The secret key to retrieve the token from the registered <see cref="ISecretProvider"/>.</param>
        /// <param name="interval">The interval in which the background job should query the Databricks instance.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="baseUrl"/> or <paramref name="tokenSecretKey"/> is blank or <paramref name="interval"/> is less then zero.</exception>
        public QueryRepeatedlyDatabricksReportJobOptions(string baseUrl, string tokenSecretKey, TimeSpan interval)
        {
            Guard.NotNullOrWhitespace(baseUrl, nameof(baseUrl));
            Guard.NotNullOrWhitespace(tokenSecretKey, nameof(tokenSecretKey));
            Guard.NotLessThan(interval, TimeSpan.Zero, nameof(interval));

            _createDataBricksClientAsync = async secretProvider =>
            {
                string token = await secretProvider.GetRawSecretAsync(tokenSecretKey);
                return DatabricksClient.CreateClient(baseUrl, token);
            };

            Interval = interval;
        }

        /// <summary>
        /// Gets the interval in which the background job should query the Databricks instance.
        /// </summary>
        public TimeSpan Interval { get; }

        /// <summary>
        /// Creates an <see cref="DatabricksClient"/> instance using the predefined values.
        /// </summary>
        /// <param name="secretProvider">The provider to retrieve the token during the creation of the instance.</param>
        internal async Task<DatabricksClient> CreateDatabricksClientAsync(ISecretProvider secretProvider)
        {
            return await _createDataBricksClientAsync(secretProvider);
        }
    }
}
