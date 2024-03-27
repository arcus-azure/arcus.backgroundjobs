using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Messaging.EventGrid; 
using CronScheduler.Extensions.Scheduler;
using GuardNet;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;

namespace Arcus.BackgroundJobs.AzureActiveDirectory
{
    /// <summary>
    /// Representing a background job that repeatedly queries Azure Active Directory for client secrets that are about to expire or have already expired.
    /// </summary>
    public class ClientSecretExpirationJob : IScheduledJob
    {
        private readonly ClientSecretExpirationJobSchedulerOptions _options;
        private readonly EventGridPublisherClient _publisherClient;
        private readonly ILogger<ClientSecretExpirationJob> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientSecretExpirationJob" /> class.
        /// </summary>
        public ClientSecretExpirationJob(
            IOptionsMonitor<ClientSecretExpirationJobSchedulerOptions> options,
            EventGridPublisherClient publisherClient,
            ILogger<ClientSecretExpirationJob> logger)
        {
            Guard.NotNull(options, nameof(options));
            Guard.NotNull(publisherClient, nameof(publisherClient));
            Guard.NotNull(logger, nameof(logger));

            ClientSecretExpirationJobSchedulerOptions value = options.Get(Name);
            Guard.NotNull(options, nameof(options), "Requires a registered options instance for this background job");

            _options = value;
            _publisherClient = publisherClient;
            _logger = logger;
        }

        /// <summary>
        /// The name of the executing job.
        /// In order for the <see cref="T:CronScheduler.Extensions.Scheduler.SchedulerOptions" /> options to work correctly make sure that the name is matched
        /// between the job and the named job options.
        /// </summary>
        public string Name { get; } = nameof(ClientSecretExpirationJob);

        /// <summary>
        /// This method is called when the <see cref="T:Microsoft.Extensions.Hosting.IHostedService" /> starts. The implementation should return a task that represents
        /// the lifetime of the long running operation(s) being performed.
        /// </summary>
        /// <param name="cancellationToken">
        ///     Triggered when <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StopAsync(System.Threading.CancellationToken)" /> is called.
        /// </param>
        /// <returns>
        ///     A <see cref="T:System.Threading.Tasks.Task" /> that represents the long running operations.
        /// </returns>
        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogTrace("Executing {Name}", Name);
                IEnumerable<AzureApplication> applications = await GetAzureApplicationsWithPotentialExpiredSecretsAsync();

                foreach (AzureApplication application in applications)
                {
                    ClientSecretExpirationEventType eventType = DetermineExpirationEventType(application);
                    
                    LogPotentialSecretEvent(application, eventType);

                    await PublishPotentialSecretEventAsync(application, eventType);
                }

                _logger.LogTrace("Executing {Name} finished", Name);
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Could not correctly publish Azure EventGrid events for potential expired client secrets in the Azure Active Directory due to an exception");
            }
        }

        private async Task<IEnumerable<AzureApplication>> GetAzureApplicationsWithPotentialExpiredSecretsAsync()
        {
            var graphServiceClient = new GraphServiceClient(new DefaultAzureCredential());
            _logger.LogTrace("Token retrieved, getting a list of applications with expired or about to expire secrets");

            var clientSecretExpirationInfoProvider = new ClientSecretExpirationInfoProvider(graphServiceClient, _logger);
            
            IEnumerable<AzureApplication> applications =
                await clientSecretExpirationInfoProvider.GetApplicationsWithPotentialExpiredSecrets(_options.UserOptions.ExpirationThreshold);
            
            return applications;
        }

        private static ClientSecretExpirationEventType DetermineExpirationEventType(AzureApplication application)
        {
            if (application.RemainingValidDays < 0)
            {
                return ClientSecretExpirationEventType.ClientSecretExpired;
            }

            return ClientSecretExpirationEventType.ClientSecretAboutToExpire;
        }

        private void LogPotentialSecretEvent(AzureApplication application, ClientSecretExpirationEventType eventType)
        {
            var telemetryContext = new Dictionary<string, object>
            {
                { "KeyId", application.KeyId },
                { "ApplicationName", application.Name },
                { "RemainingValidDays", application.RemainingValidDays }
            };

            switch (eventType)
            {
                case ClientSecretExpirationEventType.ClientSecretExpired:
                    telemetryContext["Description"] = $"The secret {application.KeyId} for Azure Active Directory application {application.Name} has expired.";
                    _logger.LogSecurityEvent("Expired Azure Active Directory application secret", telemetryContext);
                    break;
                
                case ClientSecretExpirationEventType.ClientSecretAboutToExpire:
                    telemetryContext["Description"] = $"The secret {application.KeyId} for Azure Active Directory application {application.Name} will expire within {application.RemainingValidDays} days.";
                    _logger.LogSecurityEvent("Soon expired Azure Active Directory application secret", telemetryContext);
                    break;
                
                default: 
                    throw new ArgumentOutOfRangeException(nameof(eventType), eventType, "Could not determine event type for potential expired Azure Application secret");
            }
        }

        private async Task PublishPotentialSecretEventAsync(AzureApplication application, ClientSecretExpirationEventType eventType)
        {
            Azure.Messaging.CloudEvent @event = _options.UserOptions.CreateCloudEvent(application, eventType);
            await _publisherClient.SendEventAsync(@event);
        }
    }
}
