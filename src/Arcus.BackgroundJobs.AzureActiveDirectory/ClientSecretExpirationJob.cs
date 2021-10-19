using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.Security.Core;
using Azure.Identity;
using CloudNative.CloudEvents;
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
        private readonly IEventGridPublisher _eventGridPublisher;
        private readonly ILogger<ClientSecretExpirationJob> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientSecretExpirationJob"/> class.
        /// </summary>
        /// <param name="options">The options to configure the job to query Azure Active Directory.</param>
        /// <param name="eventGridPublisher">The Event Grid Publisher which will be used to send the events to Azure Event Grid.</param>
        /// <param name="logger">The logger instance to to write telemetry to.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="options"/>, <paramref name="eventGridPublisher"/>, <paramref name="logger"/> is <c>null</c>
        ///     or the <see cref="IOptionsMonitor{TOptions}.Get"/> on the  <paramref name="options"/> returns <c>null</c>.
        /// </exception>
        public ClientSecretExpirationJob(
            IOptionsMonitor<ClientSecretExpirationJobSchedulerOptions> options,
            IEventGridPublisher eventGridPublisher,
            ILogger<ClientSecretExpirationJob> logger)
        {
            Guard.NotNull(options, nameof(options));
            Guard.NotNull(eventGridPublisher, nameof(eventGridPublisher));
            Guard.NotNull(logger, nameof(logger));

            ClientSecretExpirationJobSchedulerOptions value = options.Get(Name);
            Guard.NotNull(options, nameof(options), "Requires a registered options instance for this background job");

            _options = value;
            _eventGridPublisher = eventGridPublisher;
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
        /// <param name="stoppingToken">
        ///     Triggered when <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StopAsync(System.Threading.CancellationToken)" /> is called.
        /// </param>
        /// <returns>
        ///     A <see cref="T:System.Threading.Tasks.Task" /> that represents the long running operations.
        /// </returns>
        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogTrace("Executing ClientSecretExpiration job.");
            string token = await GetToken();
            GraphServiceClient graphServiceClient = new GraphServiceClient(
                new DelegateAuthenticationProvider((requestMessage) =>
                {
                    requestMessage
                    .Headers
                    .Authorization = new AuthenticationHeaderValue("bearer", token);

                    return Task.CompletedTask;
                })
            );

            _logger.LogTrace("Token retrieved, getting a list of applications with expired or about to expire secrets.");

            ClientSecretExpirationInfoProvider clientSecretExpirationInfoProvider = new ClientSecretExpirationInfoProvider(graphServiceClient, _logger);
            List<ApplicationWithExpiredAndAboutToExpireSecrets> applicationsWithExpiredAndAboutToExpireSecrets = await clientSecretExpirationInfoProvider.GetApplicationsWithExpiredAndAboutToExpireSecrets(_options.UserOptions.ExpirationThreshold);

            foreach (var applicationWithExpiredAndAboutToExpireSecrets in applicationsWithExpiredAndAboutToExpireSecrets)
            {
                var telemetryContext = new Dictionary<string, object>();
                telemetryContext.Add("KeyId", applicationWithExpiredAndAboutToExpireSecrets.KeyId);
                telemetryContext.Add("ApplicationName", applicationWithExpiredAndAboutToExpireSecrets.Name);
                telemetryContext.Add("RemainingValidDays", applicationWithExpiredAndAboutToExpireSecrets.RemainingValidDays);

                ClientSecretExpirationEventType eventType = ClientSecretExpirationEventType.ClientSecretAboutToExpire;
                if (applicationWithExpiredAndAboutToExpireSecrets.RemainingValidDays < 0)
                {
                    eventType = ClientSecretExpirationEventType.ClientSecretExpired;
                    _logger.LogEvent($"The secret {applicationWithExpiredAndAboutToExpireSecrets.KeyId} for Azure Active Directory application {applicationWithExpiredAndAboutToExpireSecrets.Name} has expired.", telemetryContext);
                }
                else
                {
                    _logger.LogEvent($"The secret {applicationWithExpiredAndAboutToExpireSecrets.KeyId} for Azure Active Directory application {applicationWithExpiredAndAboutToExpireSecrets.Name} will expire within {applicationWithExpiredAndAboutToExpireSecrets.RemainingValidDays} days.", telemetryContext);
                }

                CloudEvent @event = _options.UserOptions.CreateEvent(applicationWithExpiredAndAboutToExpireSecrets, eventType, _options.UserOptions.EventUri);                
                await _eventGridPublisher.PublishAsync(@event);
            }
            _logger.LogTrace("Executing ClientSecretExpiration job finished.");
        }

        private async Task<string> GetToken()
        {
            var credential = new DefaultAzureCredential();
            var token = await credential.GetTokenAsync(
                new Azure.Core.TokenRequestContext(
                    new[] { "https://graph.microsoft.com/.default" }));

            return token.Token;
        }
    }
}
