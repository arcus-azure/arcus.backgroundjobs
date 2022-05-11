using System;
using Arcus.BackgroundJobs.AzureActiveDirectory;
using Arcus.EventGrid.Publishing.Interfaces;
using GuardNet;

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
        /// <remarks>
        ///     Make sure that you register an <see cref="IEventGridPublisher"/> instance that the background job can use to publish events for potential expired Azure Application secrets.
        ///     For more information on Azure EventGrid, see: <a href="https://eventgrid.arcus-azure.net/Features/publishing-events" />.
        /// </remarks>
        /// <param name="services">The services to add the background job to.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <c>null</c>.</exception>
        public static IServiceCollection AddClientSecretExpirationJob(this IServiceCollection services)
        {
            Guard.NotNull(services, nameof(services));
            return AddClientSecretExpirationJob(services, configureOptions: null);
        }

        /// <summary>
        /// Adds the <see cref="ClientSecretExpirationJob"/> scheduled job
        /// which will query Azure Active Directory for applications that have expired or soon to be expired secrets and send a CloudEvent to an Event Grid Topic.
        /// </summary>
        /// <remarks>
        ///     Make sure that you register an <see cref="IEventGridPublisher"/> instance that the background job can use to publish events for potential expired Azure Application secrets.
        ///     For more information on Azure EventGrid, see: <a href="https://eventgrid.arcus-azure.net/Features/publishing-events" />.
        /// </remarks>
        /// <param name="services">The services to add the background job to.</param>
        /// <param name="configureOptions">The optional additional customized user configuration of options for this background job.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <c>null</c>.</exception>
        public static IServiceCollection AddClientSecretExpirationJob(
            this IServiceCollection services, 
            Action<ClientSecretExpirationJobOptions> configureOptions)
        {
            Guard.NotNull(services, nameof(services));

            return services.AddScheduler(builder =>
            {
                builder.AddJob<ClientSecretExpirationJob, ClientSecretExpirationJobSchedulerOptions>(options =>
                {
                    var additionalOptions = new ClientSecretExpirationJobOptions();
                    configureOptions?.Invoke(additionalOptions);

                    options.SetUserOptions(additionalOptions);
                });
            });
        }
    }
}
