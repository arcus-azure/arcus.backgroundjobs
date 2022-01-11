using System;
using Arcus.EventGrid.Publishing;
using Arcus.EventGrid.Publishing.Interfaces;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Arcus.BackgroundJobs.Tests.Integration.Hosting
{
    /// <summary>
    /// Extensions on the <see cref="WorkerOptions"/> related to test setup.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds an <see cref="IEventGridPublisher"/> instance to the current <paramref name="services"/>' services.
        /// </summary>
        /// <param name="services">The available application services to configure a test <see cref="Worker"/>.</param>
        /// <param name="config">The current configuration for the integration test.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or the <paramref name="config"/> is <c>null</c>.</exception>
        public static IServiceCollection AddEventGridPublisher(this IServiceCollection services, TestConfig config)
        {
            Guard.NotNull(services, nameof(services), "Requires a set of worker options to add the Azure EventGrid publisher");
            Guard.NotNull(config, nameof(config), "Requires a test configuration instance to configure the Azure EventGrid publisher");

            services.AddTransient(serviceProvider =>
            {
                var topicEndpoint = config.GetRequiredValue<string>("Arcus:Infra:EventGrid:TopicUri");
                var topicEndpointSecret = config.GetRequiredValue<string>("Arcus:Infra:EventGrid:AuthKey");

                IEventGridPublisher publisher =
                    EventGridPublisherBuilder
                        .ForTopic(topicEndpoint)
                        .UsingAuthenticationKey(topicEndpointSecret)
                        .Build();

                return publisher;
            });

            return services;
        }
    }
}
