using System;
using System.Net.Mime;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using CloudNative.CloudEvents;
using GuardNet;
using Microsoft.Extensions.DependencyInjection;
using CloudEvent = CloudNative.CloudEvents.CloudEvent;

namespace Arcus.BackgroundJobs.AzureActiveDirectory
{
    /// <summary>
    /// Represents the additional options that the user can configure during the <see cref="IServiceCollectionExtensions.AddClientSecretExpirationJob(IServiceCollection, Action{ClientSecretExpirationJobOptions})"/> call.
    /// </summary>
    public class ClientSecretExpirationJobOptions
    {
        private int _runAtHour = 0;
        private bool _runImmediately = false;
        private Uri _eventUri = new Uri("https://azure.net/");
        private int _expirationThreshold = 14;
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
        public bool RunImmediately
        {
            get => _runImmediately;
            set
            {
                _runImmediately = value;
            }
        }

        /// <summary>
        /// Gets or sets the uri of the CloudEvent that will be published to Event Grid.
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
        /// Gets or sets the threshold for the expiration, if the end datetime for a secret is lower than this value a <see cref="CloudNative.CloudEvents.CloudEvent"/> will be published.
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
        /// Creates a <see cref="CloudNative.CloudEvents.CloudEvent"/> instance using the predefined values.
        /// </summary>
        /// <param name="application">The <see cref="AzureApplication"/> containing the information regarding the application and its expiring or about to expire secret.</param>
        /// <param name="type">The type used in the creation of the <see cref="CloudNative.CloudEvents.CloudEvent"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="application"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="application"/> name is blank.</exception>
        internal CloudEvent CreateEvent(AzureApplication application, ClientSecretExpirationEventType type)
        {
            Guard.NotNull(application, nameof(application));
            Guard.NotNullOrWhitespace(application.Name, nameof(application.Name));

            string eventSubject = $"/appregistrations/clientsecrets/{application.KeyId}";
            string eventId = Guid.NewGuid().ToString();

            CloudEvent @event = new CloudEvent(
                                     CloudEventsSpecVersion.V1_0,
                                     type.ToString(),
                                     _eventUri,
                                     eventSubject,
                                     eventId)
            {
                Data = application,
                DataContentType = new ContentType("application/json")
            };

            return @event;
        }

        /// <summary>
        /// Creates a <see cref="CloudEvent"/> instance using the predefined values.
        /// </summary>
        /// <param name="application">The <see cref="AzureApplication"/> containing the information regarding the application and its expiring or about to expire secret.</param>
        /// <param name="type">The type used in the creation of the <see cref="CloudEvent"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="application"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="application"/> name is blank.</exception>
        internal Azure.Messaging.CloudEvent CreateCloudEvent(AzureApplication application, ClientSecretExpirationEventType type)
        {
            Guard.NotNull(application, nameof(application));
            Guard.NotNullOrWhitespace(application.Name, nameof(application.Name));

            string eventSubject = $"/appregistrations/clientsecrets/{application.KeyId}";
            string eventId = Guid.NewGuid().ToString();

            var @event = new Azure.Messaging.CloudEvent(
                _eventUri.OriginalString,
                type.ToString(),
                BinaryData.FromObjectAsJson(application),
                "application/json",
                CloudEventDataFormat.Json)
            {
                Id = eventId,
                Subject = eventSubject
            };
                
            return @event;
        }
    }
}
