using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Arcus.BackgroundJobs.Tests.Integration.Fixture.ServiceBus
{
    /// <summary>
    /// Represents an <see cref="AzureServiceBusMessagePump"/> implementation that spies on whether the message pump was triggered for restart.
    /// </summary>
    internal class TestSpyRestartServiceBusMessagePump : AzureServiceBusMessagePump
    {
        private int _startedCount, _stoppedCount;

        public TestSpyRestartServiceBusMessagePump(
            string jobId,
            ILogger<AzureServiceBusMessagePump> logger) 
            : base(new AzureServiceBusMessagePumpSettings("<test-entity>", subscriptionName: null, ServiceBusEntityType.Queue, getConnectionStringFromConfigurationFunc: null, getConnectionStringFromSecretFunc: _ => Task.FromResult("<secret>"), new AzureServiceBusMessagePumpOptions(), new ServiceCollection().BuildServiceProvider()),
                new ConfigurationManager(), new ServiceCollection().BuildServiceProvider(), Mock.Of<IAzureServiceBusMessageRouter>(), logger)
        {
            JobId = jobId;
        }

        /// <summary>
        /// Gets the flag that determines whether this message pump was triggered for restart.
        /// </summary>
        public bool IsRestarted => _startedCount >= 1 && _stoppedCount >= 1;

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous Start operation.</returns>
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Started test Azure Service Bus message pump");
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override Task StartProcessingMessagesAsync(CancellationToken cancellationToken)
        {
            _startedCount++;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override Task StopProcessingMessagesAsync(CancellationToken cancellationToken)
        {
            _stoppedCount++;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous Stop operation.</returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Stopped test Azure Service Bus message pump");
            return Task.CompletedTask;
        }
    }
}
