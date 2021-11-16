using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using GuardNet;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace Arcus.BackgroundJobs.AzureActiveDirectory
{
    /// <summary>
    /// Provides dev-friendly access to the <see cref="ClientSecretExpirationInfoProvider" /> instance.
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
        /// <param name="expirationThresholdInDays">The threshold for the expiration, if the end datetime for a secret is lower than this value a <see cref="CloudEvent"/> will be published.</param>
        /// <returns>A list of applications that have expired secrets or secrets that are about to expire.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="expirationThresholdInDays"/> is less than zero.</exception>
        public async Task<IEnumerable<AzureApplication>> GetApplicationsWithPotentialExpiredSecrets(int expirationThresholdInDays)
        {
            Guard.NotLessThan(expirationThresholdInDays, 0, nameof(expirationThresholdInDays), "Requires an expiration threshold in maximum remaining days the secrets are allowed to stay active");

            var applicationsList = new List<AzureApplication>();
            IGraphServiceApplicationsCollectionPage applications = await _graphServiceClient.Applications.Request().GetAsync();
            Application[] applicationsWithSecrets = applications.Where(app => app.PasswordCredentials?.Any() is true).ToArray();

            foreach (Application application in applicationsWithSecrets)
            {
                foreach (PasswordCredential passwordCredential in application.PasswordCredentials)
                {
                    string applicationName = application.DisplayName;
                    Guid? keyId = passwordCredential.KeyId;

                    if (passwordCredential.EndDateTime.HasValue)
                    {
                        double remainingValidDays = DetermineRemainingDaysBeforeExpiration(passwordCredential.EndDateTime.Value);
                        if (remainingValidDays <= expirationThresholdInDays)
                        {
                            applicationsList.Add(new AzureApplication(applicationName, keyId, passwordCredential.EndDateTime, remainingValidDays));
                            _logger.LogInformation("The secret {KeyId} for application {ApplicationName} has an expired secret or a secret that will expire within {ExpirationThresholdInDays} days", keyId, applicationName, expirationThresholdInDays);
                        }
                        else
                        {
                            _logger.LogTrace("The secret {KeyId} for application {ApplicationName} is still valid", keyId, applicationName);
                        }
                    }
                    else
                    {
                        _logger.LogTrace("The secret {KeyId} for application {ApplicationName} has no expiration date", keyId, applicationName);
                    }
                }
            }

            return applicationsList.ToArray();
        }

        private static double DetermineRemainingDaysBeforeExpiration(DateTimeOffset expirationDate)
        {
            TimeSpan remainingTime = expirationDate - DateTimeOffset.UtcNow;
            return remainingTime.TotalDays;
        }
    }
}
