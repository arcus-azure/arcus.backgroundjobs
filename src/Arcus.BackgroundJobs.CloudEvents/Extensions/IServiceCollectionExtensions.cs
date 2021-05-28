using System;
using Arcus.BackgroundJobs.CloudEvents;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using CloudNative.CloudEvents;
using GuardNet;
using Microsoft.Extensions.Configuration;
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
        /// <param name="subscriptionNamePrefix">The name of the Azure Service Bus subscription that will be created to receive <see cref="CloudEvent"/>'s.</param>
        /// <param name="serviceBusTopicConnectionStringSecretKey">The configuration key that points to the Azure Service Bus Topic connection string.</param>
        public static IServiceCollection AddCloudEventBackgroundJob(
            this IServiceCollection services,
            string subscriptionNamePrefix,
            string serviceBusTopicConnectionStringSecretKey)
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionNamePrefix, nameof(subscriptionNamePrefix), "Requires a non-blank subscription name of the Azure Service Bus Topic subscription, to receive Key Vault events");
            Guard.NotNullOrWhitespace(serviceBusTopicConnectionStringSecretKey, nameof(serviceBusTopicConnectionStringSecretKey), "Requires a non-blank configuration key that points to a Azure Service Bus Topic");

            services.AddHostedService(serviceProvider =>
            {
                var settings = new AzureServiceBusMessagePumpSettings(
                    entityName: null,
                    subscriptionName: $"{subscriptionNamePrefix}.{Guid.NewGuid().ToString()}",
                    ServiceBusEntity.Topic,
                    getConnectionStringFromConfigurationFunc: null,
                    getConnectionStringFromSecretFunc: secretProvider => secretProvider.GetRawSecretAsync(serviceBusTopicConnectionStringSecretKey),
                    options: new AzureServiceBusMessagePumpConfiguration(AzureServiceBusTopicMessagePumpOptions.Default), 
                    serviceProvider: serviceProvider);

                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var logger = serviceProvider.GetRequiredService<ILogger<AzureServiceBusMessagePump>>();

                return new CloudEventBackgroundJob(settings, configuration, serviceProvider, logger);
            });

            return services;
        }
    }
}
