using System;
using Arcus.BackgroundJobs.DataBricks;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/> to add Databricks background jobs.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the <see cref="QueryRepeatedlyDatabricksReportJob"/> background job as hosted service
        /// which will query on a fixed <paramref name="interval"/> for finished Databricks job runs and report them as metrics.
        /// </summary>
        /// <param name="services">The services to add the background job to.</param>
        /// <param name="baseUrl">The URL where the Databricks instance is located on Azure.</param>
        /// <param name="tokenSecretKey">The secret key that points to the token to authenticate with the Databricks instance.</param>
        /// <param name="interval">The interval in which to query for Databricks finished job runs.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="baseUrl"/> or <paramref name="tokenSecretKey"/> is blank or <paramref name="interval"/> is less then zero.</exception>
        public static IServiceCollection AddQueryRepeatedlyDataBricksReportJob(this IServiceCollection services, string baseUrl, string tokenSecretKey, TimeSpan interval)
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(baseUrl, nameof(baseUrl));
            Guard.NotNullOrWhitespace(tokenSecretKey, nameof(tokenSecretKey));
            Guard.NotLessThan(interval, TimeSpan.Zero, nameof(interval));

            return services.AddHostedService(serviceProvider =>
            {
                var options = new QueryRepeatedlyDatabricksReportJobOptions(baseUrl, tokenSecretKey, interval);
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var logger = serviceProvider.GetRequiredService<ILogger<QueryRepeatedlyDatabricksReportJob>>();

                return new QueryRepeatedlyDatabricksReportJob(options, configuration, serviceProvider, logger);
            });
        }
    }
}
