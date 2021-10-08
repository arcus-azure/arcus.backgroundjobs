using System;
using System.Net.Mime;
using CloudNative.CloudEvents;
using GuardNet;
using Microsoft.Extensions.DependencyInjection;

namespace Arcus.BackgroundJobs.AzureAD.ClientSecretExpiration
{
    /// <summary>
    /// Represents the additional options that the user can configure during the <see cref="IServiceCollectionExtensions.AddClientSecretExpirationJob"/> call.
    /// </summary>
    public class ClientSecretExpirationJobOptions
    {
        private int _runAtHour = 0;
        private bool _runImmediately = false;
        private Uri _eventUri = new Uri("https://github.com/arcus-azure/arcus.backgroundjobs");
        private int _expirationThreshold = 14;

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
        /// Gets or sets the threshold for the expiration, if the end datetime for a secret is lower than this value a <see cref="CloudEvent"/> will be published.
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
        /// Creates a <see cref="CloudEvent"/> instance using the predefined values.
        /// </summary>
        /// <param name="application">The <see cref="ApplicationWithExpiredAndAboutToExpireSecrets"/> containing the information regarding the application and its expiring or about to expire secret.</param>
        /// <param name="type">The type used in the creation of the <see cref="CloudEvent"/>.</param>
        /// <param name="eventUri">The uri used in the creation of the <see cref="CloudEvent"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="application"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="application"/> name is blank.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="eventUri"/> is blank.</exception>
        public virtual CloudEvent CreateEvent(ApplicationWithExpiredAndAboutToExpireSecrets application, EventType type, Uri eventUri)
        {
            Guard.NotNull(application, nameof(application));
            Guard.NotNullOrWhitespace(application.Name, nameof(application.Name));
            Guard.NotNullOrWhitespace(eventUri.OriginalString, nameof(eventUri));

            string eventSubject = $"/appregistrations/clientsecrets/{application.KeyId}";
            string eventId = Guid.NewGuid().ToString();

            CloudEvent @event = new CloudEvent(
                                    CloudEventsSpecVersion.V1_0,
                                    type.ToString(),
                                    eventUri,
                                    eventSubject,
                                    eventId)
            {
                Data = application,
                DataContentType = new ContentType("application/json")
            };

            return @event;
        }
    }
}
