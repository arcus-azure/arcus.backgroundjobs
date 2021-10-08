namespace Arcus.BackgroundJobs.AzureAD.ClientSecretExpiration
{
    /// <summary>
    /// Represents the available event types/>.
    /// </summary>
    public enum EventType
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