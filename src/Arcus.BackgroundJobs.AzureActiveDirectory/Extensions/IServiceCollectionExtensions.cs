using System;
using Arcus.BackgroundJobs.AzureActiveDirectory;
using Azure.Messaging.EventGrid; 
using Microsoft.Extensions.Azure;
using Arcus.EventGrid.Publishing.Interfaces;
using GuardNet;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

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
        ///     Make sure that you register either an <see cref="EventGridPublisherClient"/> or <see cref="IEventGridPublisher"/> instance
        ///     that the background job can use to publish events for potential expired Azure Application secrets.
        ///     For more information on Azure EventGrid, see: <a href="https://eventgrid.arcus-azure.net/Features/publishing-events" />.
        /// </remarks>
        /// <param name="services">The services to add the background job to.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when neither an <see cref="EventGridPublisherClient"/> or <see cref="IEventGridPublisher"/> is registered in the application <paramref name="services"/>.
        /// </exception>
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
        ///     Make sure that you register either an <see cref="EventGridPublisherClient"/> or <see cref="IEventGridPublisher"/> instance
        ///     that the background job can use to publish events for potential expired Azure Application secrets.
        ///     For more information on Azure EventGrid, see: <a href="https://eventgrid.arcus-azure.net/Features/publishing-events" />.
        /// </remarks>
        /// <param name="services">The services to add the background job to.</param>
        /// <param name="configureOptions">The optional additional customized user configuration of options for this background job.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when neither an <see cref="EventGridPublisherClient"/> or <see cref="IEventGridPublisher"/> is registered in the application <paramref name="services"/>.
        /// </exception>
        public static IServiceCollection AddClientSecretExpirationJob(
            this IServiceCollection services, 
            Action<ClientSecretExpirationJobOptions> configureOptions)
        {
            Guard.NotNull(services, nameof(services));

            return services.AddScheduler(builder =>
            {
                builder.AddJob<ClientSecretExpirationJob, ClientSecretExpirationJobSchedulerOptions>(
                    serviceProvider =>
                    {
                        var options = serviceProvider.GetRequiredService<IOptionsMonitor<ClientSecretExpirationJobSchedulerOptions>>();
                        var logger =
                            serviceProvider.GetService<ILogger<ClientSecretExpirationJob>>()
                            ?? NullLogger<ClientSecretExpirationJob>.Instance;

                        var factory = serviceProvider.GetService<IAzureClientFactory<EventGridPublisherClient>>();
                        if (factory != null)
                        {
                            ClientSecretExpirationJobOptions userOptions = options.Get(nameof(ClientSecretExpirationJob)).UserOptions;
                            EventGridPublisherClient client = factory.CreateClient(userOptions.ClientName);
                            return new ClientSecretExpirationJob(options, client, logger);
                        }
                        
#pragma warning disable CS0618 // Making sure this functionality is backwards compatible, despite it being deprecated.
                        var deprecatedClient = serviceProvider.GetService<IEventGridPublisher>();
                        if (deprecatedClient != null)
                        {
                            return new ClientSecretExpirationJob(options, deprecatedClient, logger);
#pragma warning restore CS0618
                        }

                        throw new InvalidOperationException(
                            "Could not create a client secret expiration background job because no Microsoft or Arcus EventGrid publisher was registered in the application services, "
                            + $"please add either an '{nameof(EventGridPublisherClient)}' instance via '{nameof(AzureClientFactoryBuilderExtensions.AddEventGridPublisherClient)}'"
                            + $"or an '{nameof(IEventGridPublisher)}' instance via 'services.AddEventGridPublisher' to the registered services."
                            + "For more information, see: https://eventgrid.arcus-azure.net/Features/publishing-events");
                    },
                    options =>
                    {
                        var additionalOptions = new ClientSecretExpirationJobOptions();
                        configureOptions?.Invoke(additionalOptions);

                        options.SetUserOptions(additionalOptions);
                    });
            });
        }
    }
}
