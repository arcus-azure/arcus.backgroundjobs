using System;
using GuardNet;

namespace Arcus.BackgroundJobs.Tests.Integration.AppConfiguration.Fixture
{
    /// <summary>
    /// Represents the test configuration to interact with the Azure App Configuration resource.
    /// </summary>
    public class AppTestConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppTestConfiguration" /> class.
        /// </summary>
        /// <param name="connectionString">The connection string to connect to the Azure App Configuration resource.</param>
        /// <param name="serviceBusTopicConnectionString">The connection string to connect to the Azure Service Bus Topic which receives Azure App Configuration events.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="connectionString"/> or <paramref name="serviceBusTopicConnectionString"/> is blank.</exception>
        public AppTestConfiguration(string connectionString, string serviceBusTopicConnectionString)
        {
            Guard.NotNullOrWhitespace(connectionString, nameof(connectionString), "Requires a non-blank connection string to connect to the Azure App Configuration resource");
            Guard.NotNullOrWhitespace(serviceBusTopicConnectionString, nameof(serviceBusTopicConnectionString), 
                "Requires a non-blank connection string to connect to the Azure Service Bus Topic which receives Azure App Configuration events");

            ConnectionString = connectionString;
            ServiceBusTopicConnectionString = serviceBusTopicConnectionString;
        }
        
        /// <summary>
        /// Gets the connection string to connect to the Azure App Configuration resource.
        /// </summary>
        public string ConnectionString { get; }
        
        /// <summary>
        /// Gets the connection string to connect to the Azure Service Bus Topic resource which receives Azure App Configuration events.
        /// </summary>
        public string ServiceBusTopicConnectionString { get; }
    }
}
