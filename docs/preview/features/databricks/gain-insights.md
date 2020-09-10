---
title: "Interact with Databricks to gain insights"
layout: default
---

# Interact with Databricks to gain insights

## Installation

To use these features, you have to install the following package:

```shell
PM > Install-Package Arcus.BackgroundJobs.Databricks
```

## Usage
The background job makes use of the available `DatabricksInfoProvider` to measure and report the finished job outcomes.
This provider can be used outside the background job environment (ex. Azure Functions).

```csharp
ILogger logger = ...
using (var client = DatabricksClient.CreateClient("https://databricks.base.url", "security.token"))
using (var provider = new DatabricksInfoProvider(client, logger))
{
}
```
The provider provides two methods.
**Getting finished jobs**
Gets all the finished job runs within a given time window.
```csharp
DatabricksInfoProvider provider = ...
var startOfWindow = DateTimeOffset.UtcNow.AddDays(-1);
var endOfWindow = DateTimeOffset.UtcNow;
IEnumerable<JobRun> finishedJobRuns = await provider.GetFinishedJobRunsAsync(startOfWindow, endOfWindow);
// Custom 'JobRun' model.
JobRun jobRun = finishedJobRuns.First();
string runId = jobRun.Run.RunId;
string jobId = jobRun.Run.JobId;
string jobName = jobRun.JobName;
RunResultState? resultState = jobRun.Run.State.ResultState;
```

**Measure finished job outcomes**
Measures the finished job runs by reporting the results as logging metrics.

This method is an combination of the previously defined method (**Getting finished jobs**) and calling an `ILogger` extension provided in this package (`ILogger.LogMetricFinishedJobOutcome`) which will write the finished job runs `JobRun` instances as metrics.

> :bulb: With using our [Arcus.Observability.Telemetry.Serilog.Sinks.ApplicationInsights](https://www.nuget.org/packages/Arcus.Observability.Telemetry.Serilog.Sinks.ApplicationInsights/), you can report these Databricks reports as metrics in Application Insights.
```csharp
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
