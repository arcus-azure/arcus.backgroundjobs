using System;
using Arcus.BackgroundJobs.CloudEvents;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using CloudNative.CloudEvents;
using GuardNet;
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
        /// <remarks>
        ///     Make sure that the application has the Arcus secret store configured correctly.
        ///     For on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
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

            return AddCloudEventBackgroundJob(services, topicName, subscriptionNamePrefix, serviceBusNamespaceConnectionStringSecretKey, configureBackgroundJob: null);
        }

        /// <summary>
        /// Adds a background job to the <see cref="IServiceCollection"/> to receive <see cref="CloudEvent"/>'s.
        /// </summary>
        /// <remarks>
        ///     Make sure that the application has the Arcus secret store configured correctly.
        ///     For on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
        /// <param name="services">The services collection to add the job to.</param>
        /// <param name="topicName"></param>
        /// <param name="subscriptionNamePrefix">The name of the Azure Service Bus subscription that will be created to receive <see cref="CloudEvent"/>'s.</param>
        /// <param name="serviceBusNamespaceConnectionStringSecretKey">The secret key that points to the Azure Service Bus namespace-scoped connection string.</param>
        /// <param name="configureBackgroundJob">The capability to configure additional options on how the CloudEvents background job should behave.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="topicName"/>, the <paramref name="subscriptionNamePrefix"/>, or the <paramref name="serviceBusNamespaceConnectionStringSecretKey"/> is blank.
        /// </exception>
        public static ServiceBusMessageHandlerCollection AddCloudEventBackgroundJob(
            this IServiceCollection services,
            string topicName,
            string subscriptionNamePrefix,
            string serviceBusNamespaceConnectionStringSecretKey,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureBackgroundJob)
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the CloudEvents background job to");
            Guard.NotNullOrWhitespace(topicName, nameof(topicName), "Requires a non-blank Azure Service Bus Topic name to identity the Azure Service Bus entity");
            Guard.NotNullOrWhitespace(subscriptionNamePrefix, nameof(subscriptionNamePrefix), "Requires a non-blank subscription name of the Azure Service Bus Topic subscription");
            Guard.NotNullOrWhitespace(serviceBusNamespaceConnectionStringSecretKey, nameof(serviceBusNamespaceConnectionStringSecretKey), "Requires a non-blank secret key that points to a Azure Service Bus namespace-scoped connection string");

            services.UseServiceBusMessageRouter(configureBackgroundJob);

            return services.AddServiceBusTopicMessagePumpWithPrefix(topicName, subscriptionNamePrefix, serviceBusNamespaceConnectionStringSecretKey, configureBackgroundJob);
        }

        /// <summary>
        /// Adds a background job to the <see cref="IServiceCollection"/> to receive <see cref="CloudEvent"/>'s.
        /// </summary>
        /// <remarks>
        ///     Make sure that the application has the Arcus secret store configured correctly.
        ///     For on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
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

            return AddCloudEventBackgroundJob(services, subscriptionNamePrefix, serviceBusTopicConnectionStringSecretKey, configureBackgroundJob: null);
        }

        /// <summary>
        /// Adds a background job to the <see cref="IServiceCollection"/> to receive <see cref="CloudEvent"/>'s.
        /// </summary>
        /// <remarks>
        ///     Make sure that the application has the Arcus secret store configured correctly.
        ///     For on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
        /// <param name="services">The services collection to add the job to.</param>
        /// <param name="subscriptionNamePrefix">The name of the Azure Service Bus subscription that will be created to receive <see cref="CloudEvent"/>'s.</param>
        /// <param name="serviceBusTopicConnectionStringSecretKey">The secret key that points to the Azure Service Bus Topic-scoped connection string.</param>
        /// <param name="configureBackgroundJob">The capability to configure additional options on how the CloudEvents background job should behave.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="subscriptionNamePrefix"/>, or the <paramref name="serviceBusTopicConnectionStringSecretKey"/> is blank.
        /// </exception>
        public static ServiceBusMessageHandlerCollection AddCloudEventBackgroundJob(
            this IServiceCollection services,
            string subscriptionNamePrefix,
            string serviceBusTopicConnectionStringSecretKey,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureBackgroundJob)
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the CloudEvents background job to");
            Guard.NotNullOrWhitespace(subscriptionNamePrefix, nameof(subscriptionNamePrefix), "Requires a non-blank subscription name of the Azure Service Bus Topic subscription, to receive Key Vault events");
            Guard.NotNullOrWhitespace(serviceBusTopicConnectionStringSecretKey, nameof(serviceBusTopicConnectionStringSecretKey), "Requires a non-blank secret key that points to a Azure Service Bus Topic-scoped connection string");

            services.UseServiceBusMessageRouter(configureBackgroundJob);

            return services.AddServiceBusTopicMessagePumpWithPrefix(subscriptionNamePrefix, serviceBusTopicConnectionStringSecretKey, configureBackgroundJob);
        }

        /// <summary>
        /// Adds a background job to the <see cref="IServiceCollection"/> to receive <see cref="CloudEvent"/>'s.
        /// </summary>
        /// <param name="services">The services collection to add the job to.</param>
        /// <param name="serviceBusNamespace">The Service Bus namespace to connect to. This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.</param>
        /// <param name="topicName">The name of the Azure Service Bus Topic to process.</param>
        /// <param name="subscriptionNamePrefix">The name of the Azure Service Bus subscription that will be created to receive <see cref="CloudEvent"/>'s.</param>
        /// <param name="clientId">
        ///     The client ID to authenticate for a user assigned managed identity. More information on user assigned managed identities cam be found here:
        ///     <see href="https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview#how-a-user-assigned-managed-identity-works-with-an-azure-vm" />.
        /// </param>
        /// <param name="configureBackgroundJob">The capability to configure additional options on how the CloudEvents background job should behave.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="serviceBusNamespace"/>, the <paramref name="topicName"/>, or the <paramref name="subscriptionNamePrefix"/> is blank.
        /// </exception>
        public static ServiceBusMessageHandlerCollection AddCloudEventBackgroundJobUsingManagedIdentity(
            this IServiceCollection services,
            string topicName,
            string subscriptionNamePrefix,
            string serviceBusNamespace,
            string clientId = null,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureBackgroundJob = null)
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the CloudEvents background job to");
            Guard.NotNullOrWhitespace(topicName, nameof(topicName), "Requires a non-blank Azure Service Bus Topic name to identity the Azure Service Bus entity");
            Guard.NotNullOrWhitespace(subscriptionNamePrefix, nameof(subscriptionNamePrefix), "Requires a non-blank subscription name of the Azure Service Bus Topic subscription, to receive Key Vault events");
            Guard.NotNullOrWhitespace(serviceBusNamespace, nameof(serviceBusNamespace), "Requires a non-blank fully qualified namespace for the Azure Service Bus Topic");

            services.UseServiceBusMessageRouter(configureBackgroundJob);

            return services.AddServiceBusTopicMessagePumpUsingManagedIdentityWithPrefix(topicName, subscriptionNamePrefix, serviceBusNamespace, clientId, configureBackgroundJob);
        }

        private static void UseServiceBusMessageRouter(this IServiceCollection services, Action<IAzureServiceBusTopicMessagePumpOptions> configureBackgroundJob = null)
        {
            services.AddServiceBusMessageRouting(serviceProvider =>
            {
                var options = new AzureServiceBusMessagePumpOptions();
                configureBackgroundJob?.Invoke(options);

                var logger = serviceProvider.GetRequiredService<ILogger<CloudEventMessageRouter>>();
                return new CloudEventMessageRouter(serviceProvider, options.Routing, logger);
            });
        }
    }
}
