---
title: "Measure Databricks job run outcomes as metric"
layout: default
---

# Measure Databricks job run outcomes as metric

The `Arcus.BackgroundJobs.Databricks` library provides a background job to repeatedly query for Databricks **finished** job runs, and reports them as metrics.

> :bulb: With using our [Arcus.Observability.Telemetry.Serilog.Sinks.ApplicationInsights](https://www.nuget.org/packages/Arcus.Observability.Telemetry.Serilog.Sinks.ApplicationInsights/), you can report these Databricks reports as metrics in Application Insights.

## Installation

To use these features, you have to install the following package:

```shell
PM > Install-Package Arcus.BackgroundJobs.Databricks
```

## Usage

Our background job has to be configured in `ConfigureServices` method:

```objectivec
using Arcus.Security.Core;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // An 'ISecretProvider' implementation (see: https://security.arcus-azure.net/) to access the Azure Service Bus Topic resource;
        //     this will get the 'tokenSecretKey' string (configured below) and has to retrieve the connection token for the Databricks instance.
        services.AddSingleton<ISecretProvider>(serviceProvider => ...);

        // Simplest registration of the scheduler job:
        services.AddDatabricksJobMetricsJob(
            baseUrl: "https://url.to.databricks.instance/"
            // Token secret key to connect to the Databricks token.
            tokenSecretKey: "Databricks.Token");

        // Customized registration of the scheduler job:
        services.AddDatabricksJobMetricsJob(
            baseUrl: "https://url.to.databricks.instance/"
            // Token secret key to connect to the Databricks token.
            tokenSecretKey: "Databricks.Token",
            options =>
            {
                // Setting the name which will be used when reporting the metric for finished Databricks job runs (default: "Databricks Job Completed").
                options.MetricName = "MyDatabricksJobMetric";

                // Settings the time interval (in minutes) in which the scheduler job should run (default: 5 minutes).
                options.IntervalInMinutes = 6;
            });
    }
}
```

[&larr; back](/)
