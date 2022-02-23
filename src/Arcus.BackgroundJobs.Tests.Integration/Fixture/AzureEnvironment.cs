using System;
using GuardNet;

namespace Arcus.BackgroundJobs.Tests.Integration.Fixture
{
    /// <summary>
    /// Represents a location in Azure where the test resources are located.
    /// </summary>
    public class AzureEnvironment
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureEnvironment"/> class.
        /// </summary>
        /// <param name="subscriptionId">The unique identifier for the global subscription that is linked to the test Azure resources.</param>
        /// <param name="tenantId">The tenant identifier within the Azure subscription where the test Azure resources are located.</param>
        /// <param name="resourceGroupName">The name of the Azure resource group inside the Azure tenant where the test Azure resources are located.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="subscriptionId"/>, <paramref name="tenantId"/>, or the <paramref name="resourceGroupName"/> is blank.
        /// </exception>
        public AzureEnvironment(string subscriptionId, string tenantId, string resourceGroupName)
        {
            Guard.NotNullOrWhitespace(subscriptionId, nameof(subscriptionId), "Requires an Azure subscription ID to create an Azure environment instance");
            Guard.NotNullOrWhitespace(tenantId, nameof(tenantId), "Requires an Azure tenant ID to create an Azure environment instance");
            Guard.NotNullOrWhitespace(resourceGroupName, nameof(resourceGroupName), "Requires an Azure resource group name to create an Azure environment instance");

            SubscriptionId = subscriptionId;
            TenantId = tenantId;
            ResourceGroupName = resourceGroupName;
        }

        /// <summary>
        /// Gets the unique identifier for the global subscription that is linked to the test Azure resources.
        /// </summary>
        public string SubscriptionId { get; }

        /// <summary>
        /// Gets the tenant identifier within the Azure subscription where the test Azure resources are located.
        /// </summary>
        public string TenantId { get; }

        /// <summary>
        /// Gets the name of the Azure resource group inside the Azure tenant where the test Azure resources are located.
        /// </summary>
        public string ResourceGroupName { get; }
    }
}
