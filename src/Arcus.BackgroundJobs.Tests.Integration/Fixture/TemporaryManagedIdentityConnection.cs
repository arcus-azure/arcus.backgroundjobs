using System;
using Arcus.BackgroundJobs.Tests.Integration.AzureActiveDirectory.Fixture;
using Arcus.BackgroundJobs.Tests.Integration.Hosting;
using GuardNet;
using Microsoft.Graph.Models;

namespace Arcus.BackgroundJobs.Tests.Integration.Fixture
{
    /// <summary>
    /// Represents a temporary Managed Identity authentication connection to Azure.
    /// </summary>
    public class TemporaryManagedIdentityConnection : IDisposable
    {
        private readonly TemporaryEnvironmentVariable[] _temporaryEnvironmentVariables;

        private TemporaryManagedIdentityConnection(params TemporaryEnvironmentVariable[] temporaryEnvironmentVariables)
        {
            _temporaryEnvironmentVariables = temporaryEnvironmentVariables;
        }

        /// <summary>
        /// Creates a temporary connection to Azure that mimics a Managed Identity.
        /// </summary>
        /// <param name="config">The configuration values to retrieve the secrets to mimic the Managed Identity connection.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="config"/> is <c>null</c>.</exception>
        public static TemporaryManagedIdentityConnection Create(TestConfig config)
        {
            Guard.NotNull(config, nameof(config), "Requires a test configuration instance to retrieve integration test values");

            ServicePrincipal servicePrincipal = config.GetServicePrincipal();
            AzureEnvironment environment = config.GetAzureEnvironment();

            return new TemporaryManagedIdentityConnection(
                TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureTenantId, environment.TenantId),
                TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureServicePrincipalClientId, servicePrincipal.ClientId),
                TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureServicePrincipalClientSecret, servicePrincipal.ClientSecret));
        }

        /// <summary>
        /// Creates a temporary connection to Azure that mimics a Managed Identity.
        /// </summary>
        /// <param name="config">The configuration values to retrieve the secrets to mimic the Managed Identity connection.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="config"/> is <c>null</c>.</exception>
        public static TemporaryManagedIdentityConnection Create(AzureActiveDirectoryConfig config)
        {
            ServicePrincipal servicePrincipal = config.ServicePrincipal;

            return new TemporaryManagedIdentityConnection(
                TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureTenantId, config.TenantId),
                TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureServicePrincipalClientId, servicePrincipal.ClientId),
                TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureServicePrincipalClientSecret, servicePrincipal.ClientSecret));
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            foreach (TemporaryEnvironmentVariable temporaryVariable in _temporaryEnvironmentVariables)
            {
                temporaryVariable.Dispose();
            }
        }
    }
}
