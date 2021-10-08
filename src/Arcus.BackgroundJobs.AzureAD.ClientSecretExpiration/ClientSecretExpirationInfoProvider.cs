using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using GuardNet;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace Arcus.BackgroundJobs.AzureAD.ClientSecretExpiration
{
    /// <summary>
    /// Provides dev-friendly access to the ClientSecretExpiration instance.
    /// </summary>
    public class ClientSecretExpirationInfoProvider
    {
        private readonly GraphServiceClient _graphServiceClient;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of hte <see cref="ClientSecretExpirationInfoProvider"/> class.
        /// </summary>
        /// <param name="graphServiceClient">The client to interact with the Microsoft Graph API.</param>
        /// <param name="logger">The instance to log metric reports of job runs.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="graphServiceClient"/> or <paramref name="logger"/> is <c>null</c>.</exception>
        public ClientSecretExpirationInfoProvider(GraphServiceClient graphServiceClient, ILogger logger)
        {
            Guard.NotNull(graphServiceClient, nameof(graphServiceClient));
            Guard.NotNull(logger, nameof(logger));

            _graphServiceClient = graphServiceClient;
            _logger = logger;
        }

        /// <summary>
        /// Returns a list of applications that have secrets that have already expired or are about to expire.
        /// </summary>
        /// <param name="expirationThreshold">The threshold for the expiration, if the end datetime for a secret is lower than this value a <see cref="CloudEvent"/> will be published.</param>
        /// <returns>A list of applications that have expired secrets or secrets that are about to expire.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="expirationThreshold"/> is less than zero.</exception>
        public async Task<List<ApplicationWithExpiredAndAboutToExpireSecrets>> GetApplicationsWithExpiredAndAboutToExpireSecrets(int expirationThreshold)
        {
            Guard.NotLessThan(expirationThreshold, 0, nameof(expirationThreshold));

            List<ApplicationWithExpiredAndAboutToExpireSecrets> applicationsList = new List<ApplicationWithExpiredAndAboutToExpireSecrets>();

            IGraphServiceApplicationsCollectionPage applications = await _graphServiceClient.Applications.Request().GetAsync();

            foreach (Application application in applications)
            {
                if (application.PasswordCredentials != null && application.PasswordCredentials.Count() > 0)
                {
                    foreach (PasswordCredential passwordCredential in application.PasswordCredentials)
                    {
                        string applicationName = application.DisplayName;
                        Guid keyId = passwordCredential.KeyId.Value;
                        double remainingValidDays = (passwordCredential.EndDateTime.Value - DateTime.UtcNow).TotalDays;

                        var telemetryContext = new Dictionary<string, object>();
                        telemetryContext.Add("keyId", keyId);
                        telemetryContext.Add("applicationName", applicationName);
                        telemetryContext.Add("remainingValidDays", remainingValidDays);

                        if (remainingValidDays <= expirationThreshold)
                        {
                            applicationsList.Add(new ApplicationWithExpiredAndAboutToExpireSecrets(applicationName, keyId, passwordCredential.EndDateTime.Value.UtcDateTime, remainingValidDays));
                        }
                    }
                }
            }

            return applicationsList;
        }
    }
}
