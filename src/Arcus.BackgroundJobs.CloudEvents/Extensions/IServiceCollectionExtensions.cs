using System;
using Arcus.BackgroundJobs.CloudEvents;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using CloudNative.CloudEvents;
using GuardNet;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/> to add CloudEvent-related background jobs.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a background job to the <see cref="IServiceCollection"/> to receive <see cref="CloudEvent"/>'s.
        /// </summary>
        /// <param name="services">The services collection to add the job to.</param>
        /// <param name="topicName"></param>
        /// <param name="subscriptionNamePrefix">The name of the Azure Service Bus subscription that will be created to receive <see cref="CloudEvent"/>'s.</param>
        /// <param name="serviceBusNamespaceConnectionStringSecretKey">The secret key that points to the Azure Service Bus namespace-scoped connection string.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="topicName"/>, the <paramref name="subscriptionNamePrefix"/>, or the <paramref name="serviceBusNamespaceConnectionStringSecretKey"/> is blank.
        /// </exception>
        public static ServiceBusMessageHandlerCollection AddCloudEventBackgroundJob(
            this IServiceCollection services,
            string topicName,
            string subscriptionNamePrefix,
            string serviceBusNamespaceConnectionStringSecretKey)
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the CloudEvents background job to");
            Guard.NotNullOrWhitespace(topicName, nameof(topicName), "Requires a non-blank Azure Service Bus Topic name to identity the Azure Service Bus entity");
            Guard.NotNullOrWhitespace(subscriptionNamePrefix, nameof(subscriptionNamePrefix), "Requires a non-blank subscription name of the Azure Service Bus Topic subscription");
            Guard.NotNullOrWhitespace(serviceBusNamespaceConnectionStringSecretKey, nameof(serviceBusNamespaceConnectionStringSecretKey), "Requires a non-blank secret key that points to a Azure Service Bus namespace-scoped connection string");

            services.TryAddSingleton<IAzureServiceBusMessageRouter>(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<CloudEventMessageRouter>>();
                return new CloudEventMessageRouter(serviceProvider, new AzureServiceBusMessageRouterOptions(), logger);
            });

            return services.AddServiceBusTopicMessagePumpWithPrefix(topicName, subscriptionNamePrefix, serviceBusNamespaceConnectionStringSecretKey);
        }

        /// <summary>
        /// Adds a background job to the <see cref="IServiceCollection"/> to receive <see cref="CloudEvent"/>'s.
        /// </summary>
        /// <param name="services">The services collection to add the job to.</param>
        /// <param name="subscriptionNamePrefix">The name of the Azure Service Bus subscription that will be created to receive <see cref="CloudEvent"/>'s.</param>
        /// <param name="serviceBusTopicConnectionStringSecretKey">The secret key that points to the Azure Service Bus Topic-scoped connection string.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="subscriptionNamePrefix"/>, or the <paramref name="serviceBusTopicConnectionStringSecretKey"/> is blank.
        /// </exception>
        public static ServiceBusMessageHandlerCollection AddCloudEventBackgroundJob(
            this IServiceCollection services,
            string subscriptionNamePrefix,
            string serviceBusTopicConnectionStringSecretKey)
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the CloudEvents background job to");
            Guard.NotNullOrWhitespace(subscriptionNamePrefix, nameof(subscriptionNamePrefix), "Requires a non-blank subscription name of the Azure Service Bus Topic subscription, to receive Key Vault events");
            Guard.NotNullOrWhitespace(serviceBusTopicConnectionStringSecretKey, nameof(serviceBusTopicConnectionStringSecretKey), "Requires a non-blank secret key that points to a Azure Service Bus Topic-scoped connection string");

            services.AddServiceBusMessageRouting(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<CloudEventMessageRouter>>();
                return new CloudEventMessageRouter(serviceProvider, new AzureServiceBusMessageRouterOptions(), logger);
            });

            return services.AddServiceBusTopicMessagePumpWithPrefix(subscriptionNamePrefix, serviceBusTopicConnectionStringSecretKey);
        }
    }
}
