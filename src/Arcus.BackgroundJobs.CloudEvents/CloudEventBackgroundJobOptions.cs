using System;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;

namespace Arcus.BackgroundJobs.CloudEvents
{
    /// <summary>
    /// Represents the options to configure the <see cref="CloudEventBackgroundJob"/>.
    /// </summary>
    [Obsolete("Configuring the CloudEvents background job now happens with a dedicated Azure Service Bus topic set of options '" + nameof(IAzureServiceBusTopicMessagePumpOptions) + "' when registering the job")]
    public class CloudEventBackgroundJobOptions
    {
        /// <summary>
        /// Gets or sets the job ID to distinguish background job instances in a multi-deployment.
        /// </summary>
        public string JobId { get; set; }
    }
}