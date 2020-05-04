---
title: "Securely Receive CloudEvents"
layout: default
---

# Securely Receive CloudEvents

![](https://img.shields.io/badge/Available%20starting-v0.1-green?link=https://github.com/arcus-azure/arcus.backgroundjobs/releases/tag/v0.1.0)

The `Arcus.BackgroundJobs.CloudEvents` library provides a collection of background jobs to securely receive [CloudEvents](https://github.com/cloudevents/spec).

This allows workloads to asynchronously process event from other components without exposing a public endpoint.

## How does it work?

An Azure Service Bus Topic resource is required to receive CloudEvents on. CloudEvent messages on this Topic will be processed by a background job.

![Automatically Invalidate Azure Key Vault Secrets](/media/CloudEvents-Job.png)

You can write your own background job(s) by deriving from `CloudEventBackgroundJob` which already takes care of topic subscription creation/deletion on start/stop of the job.

## Usage

You can easily implement your own job by implementing the `ProcessMessageAsync` method to prcocess new CloudEvents.

```csharp
public class MyBackgroundJob : CloudEventBackgroundJob
{
    public MyBackgroundJob(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<CloudEventBackgroundJob> logger) : base(configuration, serviceProvider, logger)
    {

    }

    protected override async Task ProcessMessageAsync(
        CloudEvent message,
        AzureServiceBusMessageContext messageContext,
        MessageCorrelationInfo correlationInfo,
        CancellationToken cancellationToken)
        {
            // Process the CloudEvent message...
    }
}
```

[&larr; back](/)
