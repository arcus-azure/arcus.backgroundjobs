using System;
using System.Linq;
using Arcus.BackgroundJobs.KeyVault;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using Azure.Messaging;
using GuardNet;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="ServiceBusMessageHandlerCollection"/> to make the registration of background jobs more dev-friendly.
    /// </summary>
    public static class ServiceBusMessageHandlerCollectionExtensions
    {
        private const string SecretNewVersionCreatedEventType = "Microsoft.KeyVault.SecretNewVersionCreated";

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
        public static ServiceBusMessageHandlerCollection WithAutoRestartOnRotatedCredentials(
            this ServiceBusMessageHandlerCollection services,
            string jobId,
            string subscriptionNamePrefix,
            string serviceBusTopicConnectionStringSecretKey,
            string messagePumpConnectionStringKey)
        {
            return WithAutoRestartOnRotatedCredentials(
                services, jobId, subscriptionNamePrefix, serviceBusTopicConnectionStringSecretKey, messagePumpConnectionStringKey, configureBackgroundJob: null);
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
        public static ServiceBusMessageHandlerCollection WithAutoRestartOnRotatedCredentials(
            this ServiceBusMessageHandlerCollection services,
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

            return services.Services.AddCloudEventBackgroundJob(
                        subscriptionNamePrefix,
                        serviceBusTopicConnectionStringSecretKey,
                        configureBackgroundJob)
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
                            return new ReAuthenticateOnRotatedCredentialsMessageHandler(
                                messagePumpConnectionStringKey,
                                messagePump,
                                messageHandlerLogger);

                        });
            }
        }
}
