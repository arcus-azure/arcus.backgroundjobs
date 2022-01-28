using System;
using System.Threading.Tasks;
using Arcus.EventGrid.Publishing;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.Security.Core;
using CronScheduler.Extensions.Scheduler;
using GuardNet;

namespace Arcus.BackgroundJobs.AzureActiveDirectory
{
    /// <summary>
    /// Options to configure how the <see cref="ClientSecretExpirationJob"/> scheduled job.
    /// </summary>
    public class ClientSecretExpirationJobSchedulerOptions : SchedulerOptions
    {
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
            Guard.NotNull(options.EventUri, nameof(options.EventUri));

            UserOptions = options;
            CronSchedule = $"* {options.RunAtHour} * * *";
            RunImmediately = options.RunImmediately;
        }
    }
}
