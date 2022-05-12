using System;
using Arcus.BackgroundJobs.AzureAppConfiguration;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using CloudNative.CloudEvents;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/> to add Azure App Configuration-related background jobs.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a background job to automatically refresh Azure App Configuration resources based on send-out App Configuration key-value events.
        /// </summary>
        /// <remarks>
        ///     Make sure that the application is configuring the Azure App Configuration when building the <see cref="IConfiguration"/> model
        ///     and that the Arcus secret store is configured correctly. For more information on Azure App Configuration: <a href="https://docs.microsoft.com/en-us/azure/azure-app-configuration/overview" />
        ///     and on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
        /// <param name="services">The services collection to add the job to.</param>
        /// <param name="subscriptionNamePrefix">The name of the Azure Service Bus subscription that will be created to receive App Configuration events.</param>
        /// <param name="serviceBusTopicConnectionStringSecretKey">The configuration key that points to the Azure Service Bus Topic connection string.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="subscriptionNamePrefix"/> or <paramref name="serviceBusTopicConnectionStringSecretKey"/> is blank.
        /// </exception>
        public static IServiceCollection AddAutoRefreshAppConfigurationBackgroundJob(
            this IServiceCollection services,
            string subscriptionNamePrefix,
            string serviceBusTopicConnectionStringSecretKey)
        {
            Guard.NotNull(services, nameof(services), "Requires a collection of services to add the re-authentication background job");
            Guard.NotNullOrWhitespace(subscriptionNamePrefix, nameof(subscriptionNamePrefix), "Requires a non-blank subscription name of the Azure Service Bus Topic subscription, to receive Azure App Configuration events");
            Guard.NotNullOrWhitespace(serviceBusTopicConnectionStringSecretKey, nameof(serviceBusTopicConnectionStringSecretKey), "Requires a non-blank secret key that points to a Azure Service Bus Topic");

            return AddAutoRefreshAppConfigurationBackgroundJob(
                services, 
                subscriptionNamePrefix, 
                serviceBusTopicConnectionStringSecretKey, 
                configureBackgroundJob: null);
        }

        /// <summary>
        /// Adds a background job to automatically refresh Azure App Configuration resources based on send-out App Configuration key-value events.
        /// </summary>
        /// <remarks>
        ///     Make sure that the application is configuring the Azure App Configuration when building the <see cref="IConfiguration"/> model
        ///     and that the Arcus secret store is configured correctly. For more information on Azure App Configuration: <a href="https://docs.microsoft.com/en-us/azure/azure-app-configuration/overview" />
        ///     and on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
        /// <param name="services">The services collection to add the job to.</param>
        /// <param name="subscriptionNamePrefix">The name of the Azure Service Bus subscription that will be created to receive App Configuration events.</param>
        /// <param name="serviceBusTopicConnectionStringSecretKey">The configuration key that points to the Azure Service Bus Topic connection string.</param>
        /// <param name="configureBackgroundJob">The capability to configure additional options on how the background job should behave.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="subscriptionNamePrefix"/> or <paramref name="serviceBusTopicConnectionStringSecretKey"/> is blank.
        /// </exception>
        public static IServiceCollection AddAutoRefreshAppConfigurationBackgroundJob(
            this IServiceCollection services,
            string subscriptionNamePrefix,
            string serviceBusTopicConnectionStringSecretKey,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureBackgroundJob)
        {
            Guard.NotNull(services, nameof(services), "Requires a collection of services to add the re-authentication background job");
            Guard.NotNullOrWhitespace(subscriptionNamePrefix, nameof(subscriptionNamePrefix), "Requires a non-blank subscription name of the Azure Service Bus Topic subscription, to receive Azure App Configuration events");
            Guard.NotNullOrWhitespace(serviceBusTopicConnectionStringSecretKey, nameof(serviceBusTopicConnectionStringSecretKey), "Requires a non-blank secret key that points to a Azure Service Bus Topic");

            services.AddAzureAppConfiguration()
                    .AddCloudEventBackgroundJob(subscriptionNamePrefix, serviceBusTopicConnectionStringSecretKey, configureBackgroundJob)
                    .WithServiceBusMessageHandler<RefreshAppConfigurationMessageHandler, CloudEvent>(
                        messageBodyFilter: message => message?.Type is "Microsoft.AppConfiguration.KeyValueModified" 
                                                      || message?.Type is "Microsoft.AppConfiguration.KeyValueDeleted",
                        serviceProvider =>
                        {
                            var refresher = serviceProvider.GetService<IConfigurationRefresherProvider>();
                            if (refresher is null)
                            {
                                throw new InvalidOperationException(
                                    "Cannot handle CloudEvents that notifies changes in the Azure App Configuration because no Azure App Configuration was registered with any refresh configuration registration. " 
                                    + $"Please use the '{nameof(AzureAppConfigurationExtensions.AddAzureAppConfiguration)}' extension when building the {nameof(IConfiguration)} " 
                                    + $"and configure a Azure App Configuration refresh '{nameof(IConfigurationRefresher)}' registration with the '{nameof(AzureAppConfigurationOptions.ConfigureRefresh)}' extension." 
                                    + "For more information on Azure App Configuration: https://docs.microsoft.com/en-us/azure/azure-app-configuration/");
                            }

                            var logger = 
                                serviceProvider.GetService<ILogger<RefreshAppConfigurationMessageHandler>>() 
                                ?? NullLogger<RefreshAppConfigurationMessageHandler>.Instance;

                            return new RefreshAppConfigurationMessageHandler(refresher, logger);
                        });

            return services;
        }
    }
}
