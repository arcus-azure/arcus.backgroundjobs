using System;
using System.Linq;
using Arcus.BackgroundJobs.KeyVault;
using Arcus.BackgroundJobs.KeyVault.Events;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using Arcus.Security.Core.Caching;
using Azure.Messaging;
using GuardNet;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
#if NET6_0
using CloudEvent = Azure.Messaging.CloudEvent;
#else
using CloudEvent = CloudNative.CloudEvents.CloudEvent;
#endif

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/> to make the registration of jobs more dev-friendly.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IServiceCollectionExtensions
    {
        private const string SecretNewVersionCreatedEventType = "Microsoft.KeyVault.SecretNewVersionCreated";

        /// <summary>
        /// Adds a background job to the <see cref="IServiceCollection"/> to automatically invalidate cached Azure Key Vault secrets.
        /// </summary>
        /// <remarks>
        ///     Make sure that the application has the Arcus secret store configured correctly.
        ///     For on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
        /// <param name="services">The services collection to add the job to.</param>
        /// <param name="topicName">The name of the Azure Service Bus Topic to process the received event.</param>
        /// <param name="subscriptionNamePrefix">The name of the Azure Service Bus subscription that will be created to receive Key Vault events.</param>
        /// <param name="serviceBusNamespace">The Service Bus namespace to connect to. This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="serviceBusNamespace"/>, the <paramref name="topicName"/>, or the <paramref name="subscriptionNamePrefix"/> is blank.
        /// </exception>
        public static IServiceCollection AddAutoInvalidateKeyVaultSecretUsingManagedIdentityBackgroundJob(
            this IServiceCollection services,
            string topicName,
            string subscriptionNamePrefix,
            string serviceBusNamespace)
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the CloudEvents background job to");
            Guard.NotNullOrWhitespace(topicName, nameof(topicName), "Requires a non-blank Azure Service Bus Topic name to identity the Azure Service Bus entity");
            Guard.NotNullOrWhitespace(subscriptionNamePrefix, nameof(subscriptionNamePrefix), "Requires a non-blank subscription name of the Azure Service Bus Topic subscription, to receive Key Vault events");
            Guard.NotNullOrWhitespace(serviceBusNamespace, nameof(serviceBusNamespace), "Requires a non-blank fully qualified namespace for the Azure Service Bus Topic");

            return AddAutoInvalidateKeyVaultSecretUsingManagedIdentityBackgroundJob(services, topicName, subscriptionNamePrefix, serviceBusNamespace, clientId: null);
        }

        /// <summary>
        /// Adds a background job to the <see cref="IServiceCollection"/> to automatically invalidate cached Azure Key Vault secrets.
        /// </summary>
        /// <remarks>
        ///     Make sure that the application has the Arcus secret store configured correctly.
        ///     For on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
        /// <param name="services">The services collection to add the job to.</param>
        /// <param name="topicName">The name of the Azure Service Bus Topic to process the received event.</param>
        /// <param name="subscriptionNamePrefix">The name of the Azure Service Bus subscription that will be created to receive Key Vault events.</param>
        /// <param name="serviceBusNamespace">The Service Bus namespace to connect to. This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.</param>
        /// <param name="clientId">
        ///     The client ID to authenticate for a user assigned managed identity. More information on user assigned managed identities cam be found here:
        ///     <see href="https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview#how-a-user-assigned-managed-identity-works-with-an-azure-vm" />.
        /// </param>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="serviceBusNamespace"/>, the <paramref name="topicName"/>, or the <paramref name="subscriptionNamePrefix"/> is blank.
        /// </exception>
        public static IServiceCollection AddAutoInvalidateKeyVaultSecretUsingManagedIdentityBackgroundJob(
            this IServiceCollection services,
            string topicName,
            string subscriptionNamePrefix,
            string serviceBusNamespace,
            string clientId)
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the CloudEvents background job to");
            Guard.NotNullOrWhitespace(topicName, nameof(topicName), "Requires a non-blank Azure Service Bus Topic name to identity the Azure Service Bus entity");
            Guard.NotNullOrWhitespace(subscriptionNamePrefix, nameof(subscriptionNamePrefix), "Requires a non-blank subscription name of the Azure Service Bus Topic subscription, to receive Key Vault events");
            Guard.NotNullOrWhitespace(serviceBusNamespace, nameof(serviceBusNamespace), "Requires a non-blank fully qualified namespace for the Azure Service Bus Topic");

            return AddAutoInvalidateKeyVaultSecretUsingManagedIdentityBackgroundJob(services, topicName, subscriptionNamePrefix, serviceBusNamespace, clientId, configureBackgroundJob: null);
        }

        /// <summary>
        /// Adds a background job to the <see cref="IServiceCollection"/> to automatically invalidate cached Azure Key Vault secrets.
        /// </summary>
        /// <remarks>
        ///     Make sure that the application has the Arcus secret store configured correctly.
        ///     For on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
        /// <param name="services">The services collection to add the job to.</param>
        /// <param name="topicName">The name of the Azure Service Bus Topic to process the received event.</param>
        /// <param name="subscriptionNamePrefix">The name of the Azure Service Bus subscription that will be created to receive Key Vault events.</param>
        /// <param name="serviceBusNamespace">The Service Bus namespace to connect to. This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.</param>
        /// <param name="configureBackgroundJob">The capability to configure additional options on how the CloudEvents background job should behave.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="serviceBusNamespace"/>, the <paramref name="topicName"/>, or the <paramref name="subscriptionNamePrefix"/> is blank.
        /// </exception>
        public static IServiceCollection AddAutoInvalidateKeyVaultSecretUsingManagedIdentityBackgroundJob(
            this IServiceCollection services,
            string topicName,
            string subscriptionNamePrefix,
            string serviceBusNamespace,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureBackgroundJob)
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the CloudEvents background job to");
            Guard.NotNullOrWhitespace(topicName, nameof(topicName), "Requires a non-blank Azure Service Bus Topic name to identity the Azure Service Bus entity");
            Guard.NotNullOrWhitespace(subscriptionNamePrefix, nameof(subscriptionNamePrefix), "Requires a non-blank subscription name of the Azure Service Bus Topic subscription, to receive Key Vault events");
            Guard.NotNullOrWhitespace(serviceBusNamespace, nameof(serviceBusNamespace), "Requires a non-blank fully qualified namespace for the Azure Service Bus Topic");

            return AddAutoInvalidateKeyVaultSecretUsingManagedIdentityBackgroundJob(services, topicName, subscriptionNamePrefix, serviceBusNamespace, clientId: null, configureBackgroundJob);
        }

        /// <summary>
        /// Adds a background job to the <see cref="IServiceCollection"/> to automatically invalidate cached Azure Key Vault secrets.
        /// </summary>
        /// <remarks>
        ///     Make sure that the application has the Arcus secret store configured correctly.
        ///     For on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
        /// <param name="services">The services collection to add the job to.</param>
        /// <param name="topicName">The name of the Azure Service Bus Topic to process the received event.</param>
        /// <param name="subscriptionNamePrefix">The name of the Azure Service Bus subscription that will be created to receive Key Vault events.</param>
        /// <param name="serviceBusNamespace">The Service Bus namespace to connect to. This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.</param>
        /// <param name="clientId">
        ///     The client ID to authenticate for a user assigned managed identity. More information on user assigned managed identities cam be found here:
        ///     <see href="https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview#how-a-user-assigned-managed-identity-works-with-an-azure-vm" />.
        /// </param>
        /// <param name="configureBackgroundJob">The capability to configure additional options on how the CloudEvents background job should behave.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="serviceBusNamespace"/>, the <paramref name="topicName"/>, or the <paramref name="subscriptionNamePrefix"/> is blank.
        /// </exception>
        public static IServiceCollection AddAutoInvalidateKeyVaultSecretUsingManagedIdentityBackgroundJob(
            this IServiceCollection services,
            string topicName,
            string subscriptionNamePrefix,
            string serviceBusNamespace,
            string clientId,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureBackgroundJob)
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the CloudEvents background job to");
            Guard.NotNullOrWhitespace(topicName, nameof(topicName), "Requires a non-blank Azure Service Bus Topic name to identity the Azure Service Bus entity");
            Guard.NotNullOrWhitespace(subscriptionNamePrefix, nameof(subscriptionNamePrefix), "Requires a non-blank subscription name of the Azure Service Bus Topic subscription, to receive Key Vault events");
            Guard.NotNullOrWhitespace(serviceBusNamespace, nameof(serviceBusNamespace), "Requires a non-blank fully qualified namespace for the Azure Service Bus Topic");

            services.AddCloudEventBackgroundJobUsingManagedIdentity(topicName, subscriptionNamePrefix, serviceBusNamespace, clientId, configureBackgroundJob)
                    .WithInvalidateKeyVaultSecretHandler();

            return services;
        }

        /// <summary>
        /// Adds a background job to the <see cref="IServiceCollection"/> to automatically invalidate cached Azure Key Vault secrets.
        /// </summary>
        /// <remarks>
        ///     Make sure that the application has the Arcus secret store configured correctly.
        ///     For on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
        /// <param name="services">The services collection to add the job to.</param>
        /// <param name="subscriptionNamePrefix">The name of the Azure Service Bus subscription that will be created to receive Key Vault events.</param>
        /// <param name="serviceBusTopicConnectionStringSecretKey">The configuration key that points to the Azure Service Bus Topic connection string.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="subscriptionNamePrefix"/> or <paramref name="serviceBusTopicConnectionStringSecretKey"/> is blank.
        /// </exception>
        public static IServiceCollection AddAutoInvalidateKeyVaultSecretBackgroundJob(
            this IServiceCollection services,
            string subscriptionNamePrefix,
            string serviceBusTopicConnectionStringSecretKey)
        {
            Guard.NotNullOrWhitespace(subscriptionNamePrefix, nameof(subscriptionNamePrefix), "Requires a non-blank subscription name of the Azure Service Bus Topic subscription, to receive Key Vault events");
            Guard.NotNullOrWhitespace(serviceBusTopicConnectionStringSecretKey, nameof(serviceBusTopicConnectionStringSecretKey), "Requires a non-blank configuration key that points to a Azure Service Bus Topic");

            return AddAutoInvalidateKeyVaultSecretBackgroundJob(services, subscriptionNamePrefix, serviceBusTopicConnectionStringSecretKey, configureBackgroundJob: null);
        }

        /// <summary>
        /// Adds a background job to the <see cref="IServiceCollection"/> to automatically invalidate cached Azure Key Vault secrets.
        /// </summary>
        /// <remarks>
        ///     Make sure that the application has the Arcus secret store configured correctly.
        ///     For on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
        /// <param name="services">The services collection to add the job to.</param>
        /// <param name="subscriptionNamePrefix">The name of the Azure Service Bus subscription that will be created to receive Key Vault events.</param>
        /// <param name="serviceBusTopicConnectionStringSecretKey">The configuration key that points to the Azure Service Bus Topic connection string.</param>
        /// <param name="configureBackgroundJob">The capability to configure additional options on how the auto-invalidate Azure Key Vault background job should behave.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="subscriptionNamePrefix"/> or <paramref name="serviceBusTopicConnectionStringSecretKey"/> is blank.
        /// </exception>
        public static IServiceCollection AddAutoInvalidateKeyVaultSecretBackgroundJob(
            this IServiceCollection services,
            string subscriptionNamePrefix,
            string serviceBusTopicConnectionStringSecretKey,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureBackgroundJob)
        {
            Guard.NotNullOrWhitespace(subscriptionNamePrefix, nameof(subscriptionNamePrefix), "Requires a non-blank subscription name of the Azure Service Bus Topic subscription, to receive Key Vault events");
            Guard.NotNullOrWhitespace(serviceBusTopicConnectionStringSecretKey, nameof(serviceBusTopicConnectionStringSecretKey), "Requires a non-blank configuration key that points to a Azure Service Bus Topic");

            services.AddCloudEventBackgroundJob(subscriptionNamePrefix, serviceBusTopicConnectionStringSecretKey, configureBackgroundJob)
                    .WithInvalidateKeyVaultSecretHandler();

            return services;
        }

        private static void WithInvalidateKeyVaultSecretHandler(this ServiceBusMessageHandlerCollection services)
        {
            services.WithServiceBusMessageHandler<InvalidateKeyVaultSecretHandler, CloudEvent>(
                messageBodyFilter: cloudEvent => cloudEvent?.Type == SecretNewVersionCreatedEventType,
                implementationFactory: serviceProvider =>
                {
                    var cachedSecretProvider = serviceProvider.GetService<ICachedSecretProvider>();
                    if (cachedSecretProvider is null)
                    {
                        throw new InvalidOperationException(
                            "Could not handle CloudEvents that notifies on new Azure Key Vault secret versions because no Arcus secret store was registered to invalidate the cached secrets,"
                            + $"please configure the Arcus secret store with '{nameof(IHostBuilderExtensions.ConfigureSecretStore)}' on the application '{nameof(IHost)}' "
                            + $"or during the service collection registration 'AddSecretStore' on the application '{nameof(IServiceCollection)}'."
                            + "For more information on the Arcus secret store, see: https://security.arcus-azure.net/features/secret-store");
                    }

                    var logger =
                        serviceProvider.GetService<ILogger<InvalidateKeyVaultSecretHandler>>()
                        ?? NullLogger<InvalidateKeyVaultSecretHandler>.Instance;

                    return new InvalidateKeyVaultSecretHandler(cachedSecretProvider, logger);
                });
        }

        /// <summary>
        /// Adds a background job to the <see cref="IServiceCollection"/> to automatically restart a <see cref="AzureServiceBusMessagePump"/> with a specific <paramref name="jobId"/>
        /// when the Azure Key Vault secret that holds the Azure Service Bus connection string was updated.
        /// </summary>
        /// <remarks>
        ///     Make sure that the application has the Arcus secret store configured correctly.
        ///     For on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
        /// <param name="services">The collection of services to add the job to.</param>
        /// <param name="jobId">The unique background job ID to identify which message pump to restart.</param>
        /// <param name="subscriptionNamePrefix">The name of the Azure Service Bus subscription that will be created to receive <see cref="CloudEvent"/>'s.</param>
        /// <param name="serviceBusTopicConnectionStringSecretKey">The secret key that points to the Azure Service Bus Topic connection string.</param>
        /// <param name="messagePumpConnectionStringKey">
        ///     The secret key where the connection string credentials are located for the target message pump that needs to be auto-restarted.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="services"/> or the searched for <see cref="AzureServiceBusMessagePump"/> based on the given <paramref name="jobId"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="subscriptionNamePrefix"/> or <paramref name="serviceBusTopicConnectionStringSecretKey"/> is blank.
        /// </exception>
#pragma warning disable CS0436 // Type conflicts with imported type
        [Obsolete("Consider using the " + nameof(ServiceBusMessageHandlerCollectionExtensions.WithAutoRestartOnRotatedCredentials) + " extension instead when configuring the message pump/router/handlers")]
#pragma warning restore CS0436 // Type conflicts with imported type
        public static IServiceCollection AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob(
            this IServiceCollection services,
            string jobId,
            string subscriptionNamePrefix,
            string serviceBusTopicConnectionStringSecretKey,
            string messagePumpConnectionStringKey)
        {
            Guard.NotNull(services, nameof(services), "Requires a collection of services to add the re-authentication background job");
            Guard.NotNullOrWhitespace(jobId, nameof(jobId), "Requires a non-blank job ID to identify the Azure Service Bus message pump which needs to restart");
            Guard.NotNullOrWhitespace(subscriptionNamePrefix, nameof(subscriptionNamePrefix), "Requires a non-blank subscription name of the Azure Service Bus Topic subscription, to receive Azure Key Vault events");
            Guard.NotNullOrWhitespace(serviceBusTopicConnectionStringSecretKey, nameof(serviceBusTopicConnectionStringSecretKey), "Requires a non-blank secret key that points to a Azure Service Bus Topic");
            Guard.NotNullOrWhitespace(messagePumpConnectionStringKey, nameof(messagePumpConnectionStringKey), "Requires a non-blank secret key that points to the credentials that holds the connection string of the target message pump");

            return AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob(
                services,
                jobId,
                subscriptionNamePrefix,
                serviceBusTopicConnectionStringSecretKey,
                messagePumpConnectionStringKey,
                configureBackgroundJob: null);
        }

        /// <summary>
        /// Adds a background job to the <see cref="IServiceCollection"/> to automatically restart a <see cref="AzureServiceBusMessagePump"/> with a specific <paramref name="jobId"/>
        /// when the Azure Key Vault secret that holds the Azure Service Bus connection string was updated.
        /// </summary>
        /// <remarks>
        ///     Make sure that the application has the Arcus secret store configured correctly.
        ///     For on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
        /// <param name="services">The collection of services to add the job to.</param>
        /// <param name="jobId">The unique background job ID to identify which message pump to restart.</param>
        /// <param name="subscriptionNamePrefix">The name of the Azure Service Bus subscription that will be created to receive <see cref="CloudEvent"/>'s.</param>
        /// <param name="serviceBusTopicConnectionStringSecretKey">The secret key that points to the Azure Service Bus Topic connection string.</param>
        /// <param name="messagePumpConnectionStringKey">
        ///     The secret key where the connection string credentials are located for the target message pump that needs to be auto-restarted.
        /// </param>
        /// <param name="configureBackgroundJob">
        ///     The capability to configure additional options on how the auto-restart Azure Service Bus message pump
        ///     on rotated Azure Key Vault credentials background job should behave.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="services"/> or the searched for <see cref="AzureServiceBusMessagePump"/> based on the given <paramref name="jobId"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="subscriptionNamePrefix"/> or <paramref name="serviceBusTopicConnectionStringSecretKey"/> is blank.
        /// </exception>
#pragma warning disable CS0436 // Type conflicts with imported type
        [Obsolete("Consider using the " + nameof(Microsoft.Extensions.DependencyInjection.ServiceBusMessageHandlerCollectionExtensions.WithAutoRestartOnRotatedCredentials) + " extension instead when configuring the message pump/router/handlers")]
#pragma warning restore CS0436 // Type conflicts with imported type
        public static IServiceCollection AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob(
            this IServiceCollection services,
            string jobId,
            string subscriptionNamePrefix,
            string serviceBusTopicConnectionStringSecretKey,
            string messagePumpConnectionStringKey,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureBackgroundJob)
        {
            Guard.NotNull(services, nameof(services), "Requires a collection of services to add the re-authentication background job");
            Guard.NotNullOrWhitespace(jobId, nameof(jobId), "Requires a non-blank job ID to identify the Azure Service Bus message pump which needs to restart");
            Guard.NotNullOrWhitespace(subscriptionNamePrefix, nameof(subscriptionNamePrefix), "Requires a non-blank subscription name of the Azure Service Bus Topic subscription, to receive Azure Key Vault events");
            Guard.NotNullOrWhitespace(serviceBusTopicConnectionStringSecretKey, nameof(serviceBusTopicConnectionStringSecretKey), "Requires a non-blank secret key that points to a Azure Service Bus Topic");
            Guard.NotNullOrWhitespace(messagePumpConnectionStringKey, nameof(messagePumpConnectionStringKey), "Requires a non-blank secret key that points to the credentials that holds the connection string of the target message pump");

            services.AddCloudEventBackgroundJob(subscriptionNamePrefix, serviceBusTopicConnectionStringSecretKey, configureBackgroundJob)
                    .WithServiceBusMessageHandler<ReAuthenticateOnRotatedCredentialsMessageHandler, CloudEvent>(
                        messageBodyFilter: cloudEvent => cloudEvent?.Type == SecretNewVersionCreatedEventType,
                        implementationFactory: serviceProvider =>
                        {
                            AzureServiceBusMessagePump messagePump =
                                serviceProvider.GetServices<IHostedService>()
                                               .OfType<AzureServiceBusMessagePump>()
                                               .FirstOrDefault(pump => pump.JobId == jobId);
                        
                            if (messagePump is null)
                            {
                                throw new InvalidOperationException(
                                    $"Cannot register re-authentication without a '{nameof(AzureServiceBusMessagePump)}' with job id {jobId}");
                            }
                        
                            var messageHandlerLogger = serviceProvider.GetRequiredService<ILogger<ReAuthenticateOnRotatedCredentialsMessageHandler>>();
                            return new ReAuthenticateOnRotatedCredentialsMessageHandler(messagePumpConnectionStringKey, messagePump, messageHandlerLogger);
                        });

            return services;
        }
    }
}
