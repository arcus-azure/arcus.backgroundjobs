using System;
using GuardNet;

namespace Arcus.BackgroundJobs.AzureActiveDirectory
{
    /// <summary>
    /// Represents an application running on Azure, in an Azure Active Directory.
    /// <remarks>
    ///     This model is used to publicly represent the client applications and is used during event tracking.
    ///     Therefore, new values added here should be carefully considered as all data represented here is made public in the configured logging system.
    /// </remarks>
    /// </summary>
    public class AzureApplication
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureApplication"/> class.
        /// </summary>
        /// <param name="name">The name of the application.</param>
        /// <param name="keyId">The id of the secret.</param>
        /// <param name="endDateTime">The datetime the secret will expire.</param>
        /// <param name="remainingValidDays">The remaining days before the secret will expire.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="name"/> is blank.</exception>
        public AzureApplication(string name, Guid? keyId, DateTimeOffset? endDateTime, double remainingValidDays)
        {
            Guard.NotNullOrWhitespace(name, nameof(name), "Requires a name to describe the application running on Azure");

            Name = name;
            KeyId = keyId;
            EndDateTime = endDateTime;
            RemainingValidDays = remainingValidDays;
        }

        /// <summary>
        /// Gets the name of the application.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the id of the client secret.
        /// </summary>
        public Guid? KeyId { get; }

        /// <summary>
        /// Gets the end datetime of the client secret.
        /// </summary>
        public DateTimeOffset? EndDateTime { get; }

        /// <summary>
        /// Gets the number of days the secret is still valid.
        /// </summary>
        public double RemainingValidDays { get; }
    }
}
