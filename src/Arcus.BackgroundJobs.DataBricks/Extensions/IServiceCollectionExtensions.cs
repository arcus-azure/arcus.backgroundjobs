using System;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.Databricks;
using GuardNet;

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
        /// Adds the <see cref="DatabricksJobMetrics"/> background job as hosted service
        /// which will query on a fixed interval for finished Databricks job runs and report them as metrics.
        /// </summary>
        /// <param name="services">The services to add the background job to.</param>
        /// <param name="baseUrl">The URL where the Databricks instance is located on Azure.</param>
        /// <param name="tokenSecretKey">The secret key that points to the token to authenticate with the Databricks instance.</param>
        /// <param name="configureOptions">The optional additional customized user configuration of options for this background job.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="baseUrl"/> or <paramref name="tokenSecretKey"/> is blank.</exception>
        public static IServiceCollection AddDatabricksJobMetrics(
            this IServiceCollection services, 
            string baseUrl, 
            string tokenSecretKey, 
            Action<DatabricksJobMetricsAdditionalOptions> configureOptions = null)
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(baseUrl, nameof(baseUrl));
            Guard.NotNullOrWhitespace(tokenSecretKey, nameof(tokenSecretKey));

            return services.AddScheduler(builder =>
            {
                builder.AddJob<DatabricksJobMetrics, DatabricksJobMetricsSchedulerOptions>(options =>
                {
                    var additionalOptions = new DatabricksJobMetricsAdditionalOptions();
                    configureOptions?.Invoke(additionalOptions);

                    options.BaseUrl = baseUrl;
                    options.TokenSecretKey = tokenSecretKey;
                    options.SetAdditionalOptions(additionalOptions);
                });
                builder.UnobservedTaskExceptionHandler = UnobservedExceptionHandler;
            });
        }

        private static void UnobservedExceptionHandler(object sender, UnobservedTaskExceptionEventArgs eventArgs)
        {
            Console.WriteLine(eventArgs.Exception?.GetBaseException());
            eventArgs.SetObserved();
        }
    }
}
