using System;
using System.Linq;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.Databricks;
using GuardNet;
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
        /// Adds the <see cref="DatabricksJobMetricsJob"/> scheduled job
        /// which will query for finished Databricks job runs on a specified interval and report them as metrics.
        /// </summary>
        /// <param name="services">The services to add the background job to.</param>
        /// <param name="baseUrl">The URL where the Databricks instance is located on Azure.</param>
        /// <param name="tokenSecretKey">The secret key that points to the token to authenticate with the Databricks instance.</param>
        /// <param name="configureOptions">The optional additional customized user configuration of options for this background job.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="baseUrl"/> or <paramref name="tokenSecretKey"/> is blank.</exception>
        public static IServiceCollection AddDatabricksJobMetricsJob(
            this IServiceCollection services, 
            string baseUrl, 
            string tokenSecretKey, 
            Action<DatabricksJobMetricsJobOptions> configureOptions = null)
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(baseUrl, nameof(baseUrl));
            Guard.NotNullOrWhitespace(tokenSecretKey, nameof(tokenSecretKey));

            return services.AddScheduler(builder =>
            {
                builder.AddJob<DatabricksJobMetricsJob, DatabricksJobMetricsJobSchedulerOptions>(options =>
                {
                    var additionalOptions = new DatabricksJobMetricsJobOptions();
                    configureOptions?.Invoke(additionalOptions);

                    options.BaseUrl = baseUrl;
                    options.TokenSecretKey = tokenSecretKey;
                    options.SetUserOptions(additionalOptions);
                });
                builder.UnobservedTaskExceptionHandler = (sender, args) =>  UnobservedExceptionHandler(args, services);
            });
        }

        private static void UnobservedExceptionHandler(UnobservedTaskExceptionEventArgs eventArgs, IServiceCollection services)
        {
            ServiceDescriptor logger = services.FirstOrDefault(service => service.ServiceType == typeof(ILogger));
            var loggerInstance = (ILogger) logger?.ImplementationInstance;

            loggerInstance?.LogCritical(eventArgs.Exception, "Unhandled exception in job {JobName}", nameof(DatabricksJobMetricsJob));
            eventArgs.SetObserved();
        }
    }
}
