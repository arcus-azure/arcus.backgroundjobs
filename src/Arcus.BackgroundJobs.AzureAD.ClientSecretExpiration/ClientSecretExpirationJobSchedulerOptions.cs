using System;
using System.Threading.Tasks;
using Arcus.EventGrid.Publishing;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.Security.Core;
using CronScheduler.Extensions.Scheduler;
using GuardNet;

namespace Arcus.BackgroundJobs.AzureAD.ClientSecretExpiration
{
    /// <summary>
    /// Options to configure how the <see cref="ClientSecretExpirationJob"/> scheduled job.
    /// </summary>
    public class ClientSecretExpirationJobSchedulerOptions : SchedulerOptions
    {
        private string _topicEndpoint, _topicEndpointSecretKey;

        /// <summary>
        /// Gets or sets the endpoint of the Event Grid Topic.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is blank.</exception>
        public string TopicEndpoint
        {
            get => _topicEndpoint;
            set
            {
                Guard.NotNullOrWhitespace(value, nameof(value));
                _topicEndpoint = value;
            }
        }

        /// <summary>
        /// Gets or sets the secret key to retrieve the topic endpoint key from the registered <see cref="ISecretProvider"/>.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is blank.</exception>
        public string TopicEndpointSecretKey
        {
            get => _topicEndpointSecretKey;
            set
            {
                Guard.NotNullOrWhitespace(value, nameof(value));
                _topicEndpointSecretKey = value;
            }
        }

        /// <summary>
        /// Gets the additional user options which configures the <see cref="ClientSecretExpirationJob"/> scheduled job.
        /// </summary>
        public ClientSecretExpirationJobOptions UserOptions { get; private set; } = new ClientSecretExpirationJobOptions();

        /// <summary>
        /// Sets the additional user options in a <see cref="SchedulerOptions"/> context.
        /// </summary>
        /// <param name="options">The additional user-options to set.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> is <c>null</c>.</exception>
        internal void SetUserOptions(ClientSecretExpirationJobOptions options)
        {
            Guard.NotNull(options, nameof(options));

            UserOptions = options;
            CronSchedule = $"* {options.RunAtHour} * * *";
            RunImmediately = options.RunImmediately;
        }

        /// <summary>
        /// Creates an <see cref="IEventGridPublisher"/> instance using the predefined values.
        /// </summary>
        /// <param name="secretProvider">The provider to retrieve the token during the creation of the instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="secretProvider"/> is <c>null</c>.</exception>
        public virtual async Task<IEventGridPublisher> CreateEventGridPublisherBuilderAsync(ISecretProvider secretProvider)
        {
            Guard.NotNull(secretProvider, nameof(secretProvider));

            if (String.IsNullOrWhiteSpace(_topicEndpoint))
            {
                throw new InvalidOperationException($"ClientSecretExpiration options are not correctly configured: requires a {nameof(TopicEndpoint)} that points to the Event Grid Topic");
            }

            if (String.IsNullOrWhiteSpace(_topicEndpointSecretKey))
            {
                throw new InvalidOperationException(
                    $"ClientSecretExpiration options are not correctly configured: requires a {nameof(TopicEndpointSecretKey)} to retrieve the topic endpoint key from the registered {nameof(ISecretProvider)} "
                    + "to authenticate to the Topic Endpoint.");
            }

            string topicEndpointKey = await secretProvider.GetRawSecretAsync(_topicEndpointSecretKey);
            IEventGridPublisher eventGridPublisher = EventGridPublisherBuilder
                .ForTopic(_topicEndpoint)
                .UsingAuthenticationKey(topicEndpointKey)
                .Build();
            return eventGridPublisher;
        }
    }
}
