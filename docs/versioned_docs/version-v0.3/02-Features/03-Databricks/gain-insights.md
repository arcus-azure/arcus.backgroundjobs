---
title: "Interact with Databricks to gain insights"
layout: default
---

# Interact with Databricks to gain insights

## Installation

To use these features, you have to install the following package:

```shell
PM > Install-Package Arcus.BackgroundJobs.Databricks -Version 0.3.0
```

> :bulb: With using our [Arcus.Observability.Telemetry.Serilog.Sinks.ApplicationInsights](https://www.nuget.org/packages/Arcus.Observability.Telemetry.Serilog.Sinks.ApplicationInsights/), you can report these Databricks reports as metrics in Application Insights.

## Usage
We provide a  `DatabricksInfoProvider` which allows you to interact with Databricks clusters to gain insights on your workloads, such as measuring job run outcomes.

It can be easily setup and used anywhere such as .NET Core workers, Azure Functions and more. We are using this ourselves for our [job metrics](./job-metrics).

```csharp
using Arcus.BackgroundJobs.Databricks;
using Microsoft.Azure.Databricks.Client;
using Microsoft.Extensions.Logging;

ILogger logger = ...
using (var client = DatabricksClient.CreateClient("https://databricks.base.url", "security.token"))
using (var provider = new DatabricksInfoProvider(client, logger))
{
}
```

### Getting finished job run information
Gets all the finished job runs within a given time window.
```csharp
using Arcus.BackgroundJobs.Databricks;

DatabricksInfoProvider provider = ...
var startOfWindow = DateTimeOffset.UtcNow.AddDays(-1);
var endOfWindow = DateTimeOffset.UtcNow;
IEnumerable<JobRun> finishedJobRuns = await provider.GetFinishedJobRunsAsync(startOfWindow, endOfWindow);
```

It provides following information about the job that was executated, such as name & id, along with [details about a single job run](https://github.com/Azure/azure-databricks-client/blob/master/csharp/Microsoft.Azure.Databricks.Client/Run.cs).

### Measure finished job outcomes
Measures the finished job runs by reporting the results as (multi-dimensional) metrics.

This method is an combination of the previously defined method (**Getting finished jobs**) and calling an `ILogger` extension provided in this package (`ILogger.LogMetricFinishedJobOutcome`) which will write the finished job runs `JobRun` instances as metrics.

```csharp
using Arcus.BackgroundJobs.Databricks;

DatabricksInfoProvider provider = ...
var metricName = "Databricks Job Completed";
var startOfWindow = DateTimeOffset.UtcNow.AddDays(-1);
var endOfWindow = DateTimeOffset.UtcNow;
await provider.MeasureJobOutcomesAsync(metricName, startOfWindow, endOfWindow);
// Logs > Metric Databricks Job Completed: 1 {UtcNow} (Context: {[Run Id] = my.run.id, [Job Id] = my.job.id, [Job Name] = my.job.name, [Outcome] = Success})
```

> Note: you can always call **Getting finished jobs** yourself and pass along the finished jobs to the available `ILogger.LogMetricFinishedJobOutcome` extension.
> That way, you can pass along additional contextual properties

[&larr; back](/)
