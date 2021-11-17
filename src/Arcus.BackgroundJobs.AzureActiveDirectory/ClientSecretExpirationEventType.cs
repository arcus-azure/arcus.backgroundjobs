namespace Arcus.BackgroundJobs.AzureActiveDirectory
{
    /// <summary>
    /// Represents the available event types.
    /// </summary>
    public enum ClientSecretExpirationEventType
    {
        /// <summary>
        /// The event type for when the client secret is about to expire.
        /// </summary>
        ClientSecretAboutToExpire,

        /// <summary>
        /// The event type for when the client secret has already expired
        /// </summary>
        ClientSecretExpired
    }
}