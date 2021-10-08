using System;
using GuardNet;

namespace Arcus.BackgroundJobs.AzureAD.ClientSecretExpiration
{
    /// <summary>
    /// Represents an <see cref="ApplicationWithExpiredAndAboutToExpireSecrets"/> from Azure Active Directory.
    /// </summary>
    public class ApplicationWithExpiredAndAboutToExpireSecrets
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationWithExpiredAndAboutToExpireSecrets"/> class.
        /// </summary>
        /// <param name="name">The name of the application.</param>
        /// <param name="keyId">The id of the secret.</param>
        /// <param name="endDateTime">The datetime the secret will expire.</param>
        /// <param name="remainingValidDays">The remaining days before the secret will expire.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="name"/> is blank.</exception>
        public ApplicationWithExpiredAndAboutToExpireSecrets(string name, Guid keyId, DateTime endDateTime, double remainingValidDays)
        {
            Guard.NotNullOrWhitespace(name, nameof(name));

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
        /// Gets the id of the clientsecret.
        /// </summary>
        public Guid KeyId { get; }

        /// <summary>
        /// Gets the end datetime of the clientsecret.
        /// </summary>
        public DateTime EndDateTime { get; }

        /// <summary>
        /// Gets the number of days the secret is still valid.
        /// </summary>
        public double RemainingValidDays { get; }
    }
}
