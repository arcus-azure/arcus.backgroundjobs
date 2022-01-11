using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.Tests.Integration.Hosting;
using Arcus.BackgroundJobs.Tests.Integration.KeyVault.Fixture;
using GuardNet;
using Microsoft.Azure.Management.ServiceBus;
using Microsoft.Azure.Management.ServiceBus.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;

namespace Arcus.BackgroundJobs.Tests.Integration.Fixture.ServiceBus
{
    /// <summary>
    /// Represents the test client to interact with a Azure Service Bus resource.
    /// </summary>
    public class ServiceBusConfiguration
    {
        private readonly AzureEnvironment _environment;
        private readonly ServicePrincipal _servicePrincipal;
        private readonly KeyRotationConfig _rotationConfig;
        private readonly ILogger _logger;

        private ServiceBusConfiguration(
            AzureEnvironment environment,
            ServicePrincipal servicePrincipal,
            KeyRotationConfig rotationConfig, 
            ILogger logger)
        {
            Guard.NotNull(rotationConfig, nameof(rotationConfig));

            _environment = environment;
            _servicePrincipal = servicePrincipal;
            _rotationConfig = rotationConfig;
            _logger = logger ?? NullLogger.Instance;
        }

        /// <summary>
        /// Creates an <see cref="ServiceBusConfiguration"/> instance based on the test configuration values of the current test run.
        /// </summary>
        /// <param name="config">The current integration test configuration.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the interaction with Azure Service Bus.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="config"/> is <c>null</c>.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when one or more configuration values cannot be found in the given test <paramref name="config"/> instance.</exception>
        public static ServiceBusConfiguration CreateFrom(TestConfig config, ILogger logger)
        {
            Guard.NotNull(config, nameof(config), "Requires a test configuration instance to retrieve the details to interact with Azure Service Bus");

            AzureEnvironment environment = config.GetAzureEnvironment();
            ServicePrincipal servicePrincipal = config.GetServicePrincipal();
            KeyRotationConfig rotationConfig = config.GetKeyRotationConfig();

            return new ServiceBusConfiguration(environment, servicePrincipal, rotationConfig, logger);
        }

        /// <summary>
        /// Rotates the connection string key of the Azure Service Bus Queue, returning the new connection string as result.
        /// </summary>
        /// <param name="keyType">The type of key to rotate.</param>
        /// <returns>
        ///     The new connection string according to the <paramref name="keyType"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="keyType"/> is not within the bounds of the enumeration.</exception>
        public async Task<string> RotateConnectionStringKeysForQueueAsync(KeyType keyType)
        {
            Guard.For<ArgumentOutOfRangeException>(
                () => !Enum.IsDefined(typeof(KeyType), keyType),
                $"Requires a KeyType that is either '{nameof(KeyType.PrimaryKey)}' or '{nameof(KeyType.SecondaryKey)}'");

            var parameters = new RegenerateAccessKeyParameters(keyType);
            string queueName = _rotationConfig.ServiceBusNamespace.QueueName;
            const ServiceBusEntityType entity = ServiceBusEntityType.Queue;

            try
            {
                using (IServiceBusManagementClient client = await CreateServiceManagementClientAsync())
                {
                    _logger.LogTrace("Start rotating {KeyType} connection string of Azure Service Bus {EntityType} '{EntityName}'...", keyType, entity, queueName);

                    AccessKeys accessKeys = await client.Queues.RegenerateKeysAsync(
                        _environment.ResourceGroupName,
                        _rotationConfig.ServiceBusNamespace.NamespaceName,
                        queueName,
                        _rotationConfig.ServiceBusNamespace.AuthorizationRuleName,
                        parameters);

                    _logger.LogInformation("Rotated {KeyType} connection string of Azure Service Bus {EntityType} '{EntityName}'", keyType, entity, queueName);

                    switch (keyType)
                    {
                        case KeyType.PrimaryKey: return accessKeys.PrimaryConnectionString;
                        case KeyType.SecondaryKey: return accessKeys.SecondaryConnectionString;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(keyType), keyType, "Unknown key type");
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to rotate the {KeyType} connection string of the Azure Service Bus {EntityType} '{EntityName}'", keyType, entity, queueName);
                
                throw;
            }
        }

        private async Task<IServiceBusManagementClient> CreateServiceManagementClientAsync()
        {
            string tenantId = _environment.TenantId;
            var context = new AuthenticationContext($"https://login.microsoftonline.com/{tenantId}");

            ClientCredential clientCredentials = _servicePrincipal.CreateCredentials();
            AuthenticationResult result =
                await context.AcquireTokenAsync(
                    "https://management.azure.com/",
                    clientCredentials);

            var tokenCredentials = new TokenCredentials(result.AccessToken);
            string subscriptionId = _environment.SubscriptionId;

            var client = new ServiceBusManagementClient(tokenCredentials) { SubscriptionId = subscriptionId };
            return client;
        }
    }
}
