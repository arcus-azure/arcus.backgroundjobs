using System;
using Arcus.BackgroundJobs.Tests.Integration.Fixture;
using GuardNet;

namespace Arcus.BackgroundJobs.Tests.Integration.AzureActiveDirectory.Fixture
{
    /// <summary>
    /// Represents the configuration model to connect to the test Azure Active Directory used during the integration tests.
    /// </summary>
    public class AzureActiveDirectoryConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureActiveDirectoryConfig" /> class.
        /// </summary>
        /// <param name="tenantId">The ID of the tenant of the test Azure Active Directory used during the integration tests.</param>
        /// <param name="clientId">The ID of the service principal to connect to the Azure Active Directory.</param>
        /// <param name="clientSecret">The secret of the service principal to connect to the Azure Active Directory.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="tenantId"/>, <paramref name="clientId"/>, or <paramref name="clientSecret"/> is blank.
        /// </exception>
        public AzureActiveDirectoryConfig(
            string tenantId,
            string clientId,
            string clientSecret)
        {
            Guard.NotNullOrWhitespace(tenantId, nameof(tenantId), "Requires a non-blank tenant ID to run Azure Active Directory integration tests");

            TenantId = tenantId;
            ServicePrincipal = new ServicePrincipal(clientId, clientSecret);
        }
        
        /// <summary>
        /// Gets the ID of the tenant of the test Azure Active Directory used during the integration tests.
        /// </summary>
        public string TenantId { get; }
        
        /// <summary>
        /// Gets the service principal credentials to connect to the test Azure Active Directory used during the integration tests.
        /// </summary>
        public ServicePrincipal ServicePrincipal { get; }
    }
}
