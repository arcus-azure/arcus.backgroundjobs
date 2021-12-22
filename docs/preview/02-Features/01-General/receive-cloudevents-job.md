---
title: "Securely Receive CloudEvents"
layout: default
---

# Securely Receive CloudEvents

The `Arcus.BackgroundJobs.CloudEvents` library provides a collection of background jobs to securely receive [CloudEvents](https://github.com/cloudevents/spec).

This allows workloads to asynchronously process event from other components without exposing a public endpoint.

## How does it work?

An Azure Service Bus Topic resource is required to receive CloudEvents on. CloudEvent messages on this Topic will be processed by a background job.

![Automatically Invalidate Azure Key Vault Secrets](/media/CloudEvents-Job.png)

## Usage

The CloudEvents background job uses the [Arcus Messaging](https://github.com/arcus-azure/arcus.messaging) functionality to receive messages. 
Make sure you take a look at the [documentation on message handlers](https://messaging.arcus-azure.net/features/message-pumps/service-bus) to fully grasp the possibilities.

The CloudEvent background job itself can be easily registered:

```csharp
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Add CloudEvents background job with an namespace-scoped connection string.
        services.AddCloudEventBackgroundJob(
            topicName: "<your-topic>",
            subscriptionNamePrefix: "Sub-",
            serviceBusNamespaceConnectionStringSecretKey: "<secret-key-name-for-servicebus-namespace-connection-string>");

        // Add CloudEvent background job with an entity-scoped connection string.
        services.AddCloudEventBackgroundJob(
            subscriptionNamePrefix: "Sub-",
            serviceBusTopicConnectionStringSecretKey: "<secret-key-name-for-servicebus-topic-connection-string>");

        // Add CloudEvent background job via Managed Identity
        services.AddCloudEventBackgroundJobUsingManagedIdentity(
            serviceBusNamespace: "<your-namespace>.servicebus.windows.net",
            topicName: "<your-topic>",
            subscriptionNamePrefix: "Sub-",
            // The optional client id to authenticate for a user assigned managed identity. More information on user assigned managed identities cam be found here:
            // https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview#how-a-user-assigned-managed-identity-works-with-an-azure-vm
            clientId: "<your-client-id>"
        );
    }
}
```

To handle the incoming CloudEvents messages, you can register an custom `IAzureServiceBusMessageHandler<CloudEvent>` message handler instance:

```csharp
using CloudNative.CloudEvents;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddCloudEventEventBackgroundJob(...)
                .WithServiceBusMessageHandler<MyCloudEventMessageHandler, CloudEvent>();
    }
}
```

Such an custom implementation could look like this:

```csharp
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using CloudNative.CloudEvents;
using Microsoft.Extensions.Logging;

public class MyCloudEventMessageHandler : IAzureServiceBusMessageHandler<CloudEvent>
{
    private readonly ILogger _logger;

    public MyCloudEventMessageHandler(ILogger<MyCloudEventMessageHandler> logger)
    {
        _logger = logger;
    }

    public async Task ProcessMessageAsync(
        CloudEvent message,
        AzureServiceBusMessageContext messageContext,
        MessageCorrelationInfo correlationInfo,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing CloudEvent message");
    }
}
```

[&larr; back](/)
