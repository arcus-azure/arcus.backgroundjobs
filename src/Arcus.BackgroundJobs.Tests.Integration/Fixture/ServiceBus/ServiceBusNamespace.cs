using GuardNet;

namespace Arcus.BackgroundJobs.Tests.Integration.Fixture.ServiceBus
{
    /// <summary>
    /// Represents the configuration values related to an Azure Service Bus instance.
    /// </summary>
    public class ServiceBusNamespace
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusNamespace" /> class.
        /// </summary>
        /// <param name="namespace">The namespace in which the Azure Service Bus is categorized.</param>
        /// <param name="queueName">The name of the Queue in the Azure Service Bus instance.</param>
        /// <param name="authorizationRuleName">The name of the authorization rule that describes the available permissions.</param>
        public ServiceBusNamespace(
            string @namespace,
            string queueName,
            string authorizationRuleName)
        {
            Guard.NotNullOrWhitespace(@namespace, nameof(@namespace));
            Guard.NotNullOrWhitespace(queueName, nameof(queueName));
            Guard.NotNullOrWhitespace(authorizationRuleName, nameof(authorizationRuleName));

            NamespaceName = @namespace;
            QueueName = queueName;
            AuthorizationRuleName = authorizationRuleName;
        }

        /// <summary>
        /// Gets the namespace in which the Azure Service Bus is categorized.
        /// </summary>
        public string NamespaceName { get; }

        /// <summary>
        /// Gets the name of the Queue in the Azure Service Bus namespace.
        /// </summary>
        public string QueueName { get; }

        /// <summary>
        /// Gets the name of the authorization rule that describes the available permissions.
        /// </summary>
        public string AuthorizationRuleName { get; }
    }
}
