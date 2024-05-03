using System;
using System.Net.Mime;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using GuardNet;
using Microsoft.Extensions.DependencyInjection;

namespace Arcus.BackgroundJobs.AzureActiveDirectory
{
    /// <summary>
    /// Represents the additional options that the user can configure during the <see cref="IServiceCollectionExtensions.AddClientSecretExpirationJob(IServiceCollection, Action{ClientSecretExpirationJobOptions})"/> call.
    /// </summary>
    public class ClientSecretExpirationJobOptions
    {
#pragma warning disable S1075 // Default event URI - not necessary to be defined by user.
        private Uri _eventUri = new Uri("https://azure.net/");
#pragma warning restore S1075
        
        private int _expirationThreshold = 14;
        private int _runAtHour = 0;
        private string _clientName = "Default";

        /// <summary>
        /// Gets or sets the hour which to query for client secrets.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/> is less than zero or greater than 23.</exception>
        public int RunAtHour
        {
            get => _runAtHour;
            set
            {
                Guard.NotLessThan(value, 0, nameof(value));
                Guard.NotGreaterThan(value, 23, nameof(value));

                _runAtHour = value;
            }
        }

        /// <summary>
        /// Gets or sets whether the task should run immediately, this should only be used for testing purposes.
        /// </summary>
        public bool RunImmediately { get; set; } = false;

        /// <summary>
        /// Gets or sets the source of the CloudEvent that will be published to Event Grid.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/> is blank.</exception>
        public Uri EventUri
        {
            get => _eventUri;
            set
            {
                Guard.NotNullOrWhitespace(value.OriginalString, nameof(value));

                _eventUri = value;
            }
        }

        /// <summary>
        /// Gets or sets the threshold for the expiration. If the end datetime for a secret is lower than this value a <see cref="CloudEvent"/> will be published.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/> is less than zero.</exception>
        public int ExpirationThreshold
        {
            get => _expirationThreshold;
            set
            {
                Guard.NotLessThan(value, 0, nameof(value));

                _expirationThreshold = value;
            }
        }

        /// <summary>
        /// Gets or sets the logical client name of the registered <see cref="EventGridPublisherClient"/> (default: Default).
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is blank.</exception>
        public string ClientName
        {
            get => _clientName;
            set
            {
                Guard.NotNullOrWhitespace(value, nameof(value), "Requires a non-blank logical client name of the registered EventGrid publisher client");
                _clientName = value;
            }
        }

        /// <summary>
        /// Creates a <see cref="CloudEvent"/> instance using the predefined values.
        /// </summary>
        /// <param name="application">The <see cref="AzureApplication"/> containing the information regarding the application and its expiring or about to expire secret.</param>
        /// <param name="type">The type used in the creation of the <see cref="CloudEvent"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="application"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="application"/> name is blank.</exception>
        internal CloudEvent CreateCloudEvent(AzureApplication application, ClientSecretExpirationEventType type)
        {
            Guard.NotNull(application, nameof(application));
            Guard.NotNullOrWhitespace(application.Name, nameof(application.Name));

            string eventSubject = $"/appregistrations/clientsecrets/{application.KeyId}";
            string eventId = Guid.NewGuid().ToString();

            var @event = new CloudEvent(
                _eventUri.OriginalString,
                type.ToString(),
                BinaryData.FromObjectAsJson(application),
                MediaTypeNames.Application.Json,
                CloudEventDataFormat.Json)
            {
                Id = eventId,
                Subject = eventSubject
            };
                
            return @event;
        }
    }
}
