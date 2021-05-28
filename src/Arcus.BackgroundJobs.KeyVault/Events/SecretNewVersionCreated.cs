using Newtonsoft.Json;

namespace Arcus.BackgroundJobs.KeyVault.Events 
{
    /// <summary>
    /// Azure Key Vault secret event, when a new version is created for the secret.
    /// </summary>
    public class SecretNewVersionCreated
    {
        /// <summary>
        /// Gets or sets the unique identifier of the event.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }
 
        /// <summary>
        /// Gets or sets the Azure Key Vault name of the object that triggered this event.
        /// </summary>
        [JsonProperty("vaultName")]
        public string VaultName { get; set; }
        
        /// <summary>
        /// Gets or sets the type of the object that triggered this event.
        /// </summary>
        [JsonProperty("objectType")]
        public string ObjectType { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the object that triggered this event.
        /// </summary>
        [JsonProperty("objectName")]
        public string ObjectName { get; set; }
    }
}