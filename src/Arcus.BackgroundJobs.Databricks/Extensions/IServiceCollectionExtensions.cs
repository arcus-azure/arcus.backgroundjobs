using System;
using Arcus.BackgroundJobs.Databricks;
using Arcus.Security.Core;
using GuardNet;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

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
        /// <remarks>
        ///     Make sure that the application has the Arcus secret store configured correctly.
        ///     For on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
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
                builder.AddJob<DatabricksJobMetricsJob, DatabricksJobMetricsJobSchedulerOptions>(
                    serviceProvider =>
                    {
                        var options = serviceProvider.GetRequiredService<IOptionsMonitor<DatabricksJobMetricsJobSchedulerOptions>>();
                        var secretProvider = serviceProvider.GetService<ISecretProvider>();
                        if (secretProvider is null)
                        {
                            throw new InvalidOperationException(
                                "Could not register the Databricks background job to measure finished job runs because no Arcus secret store was registered to retrieve the access token to interact with the Databricks instance,"
                                + $"please configure the Arcus secret store with '{nameof(IHostBuilderExtensions.ConfigureSecretStore)}' on the application '{nameof(IHost)}' "
                                + $"or during the service collection registration by calling 'AddSecretStore' on the application '{nameof(IServiceCollection)}'."
                                + "For more information on the Arcus secret store, see: https://security.arcus-azure.net/features/secret-store");
                        }

                        var logger =
                            serviceProvider.GetService<ILogger<DatabricksJobMetricsJob>>()
                            ?? NullLogger<DatabricksJobMetricsJob>.Instance;

                        return new DatabricksJobMetricsJob(options, secretProvider, logger);
                    },
                    options =>
                    {
                        var additionalOptions = new DatabricksJobMetricsJobOptions();
                        configureOptions?.Invoke(additionalOptions);

                        options.BaseUrl = baseUrl;
                        options.TokenSecretKey = tokenSecretKey;
                        options.SetUserOptions(additionalOptions);
                    });
            });
        }
    }
}
