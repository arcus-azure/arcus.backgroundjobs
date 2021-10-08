using System;
using System.Linq;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.AzureAD.ClientSecretExpiration;
using GuardNet;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/> to add ClientSecretExpiration background jobs.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the <see cref="ClientSecretExpirationJob"/> scheduled job
        /// which will query Azure Active Directory for applications that have expired or soon to be expired secrets and send a CloudEvent to an Event Grid Topic.
        /// </summary>
        /// <param name="services">The services to add the background job to.</param>
        /// <param name="topicEndpoint">The endpoint of the Event Grid Topic where the events will be sent to.</param>
        /// <param name="topicEndpointSecretKey">The secret key that points to the token to authenticate to the Event Grid Topic.</param>
        /// <param name="configureOptions">The optional additional customized user configuration of options for this background job.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="topicEndpoint"/> or <paramref name="topicEndpointSecretKey"/> is blank.</exception>
        public static IServiceCollection AddClientSecretExpirationJob(
            this IServiceCollection services, 
            string topicEndpoint, 
            string topicEndpointSecretKey, 
            Action<ClientSecretExpirationJobOptions> configureOptions = null)
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(topicEndpoint, nameof(topicEndpoint));
            Guard.NotNullOrWhitespace(topicEndpointSecretKey, nameof(topicEndpointSecretKey));

            return services.AddScheduler(builder =>
            {
                builder.AddJob<ClientSecretExpirationJob, ClientSecretExpirationJobSchedulerOptions>(options =>
                {
                    var additionalOptions = new ClientSecretExpirationJobOptions();
                    configureOptions?.Invoke(additionalOptions);

                    options.TopicEndpoint = topicEndpoint;
                    options.TopicEndpointSecretKey = topicEndpointSecretKey;
                    options.SetUserOptions(additionalOptions);
                });
                builder.UnobservedTaskExceptionHandler = (sender, args) =>  UnobservedExceptionHandler(args, services);
            });
        }

        private static void UnobservedExceptionHandler(UnobservedTaskExceptionEventArgs eventArgs, IServiceCollection services)
        {
            ServiceDescriptor logger = services.FirstOrDefault(service => service.ServiceType == typeof(ILogger));
            var loggerInstance = (ILogger) logger?.ImplementationInstance;

            loggerInstance?.LogCritical(eventArgs.Exception, "Unhandled exception in job {JobName}", nameof(ClientSecretExpirationJob));
            eventArgs.SetObserved();
        }
    }
}
