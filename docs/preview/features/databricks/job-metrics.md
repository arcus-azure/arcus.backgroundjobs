---
title: "Repeatedly query Databricks report"
layout: default
---

# Repeatedly query Databricks job runs and report as metric

The `Arcus.BackgroundJobs.Databricks` library provides a background job to repeatedly query for Databricks **finished** job runs, and reports them as metrics.

> :bulb: With using our [Arcus.Observability.Telemetry.Serilog.Sinks.ApplicationInsights](https://www.nuget.org/packages/Arcus.Observability.Telemetry.Serilog.Sinks.ApplicationInsights/), you can report these Databricks reports as metrics in Application Insights.

## Usage

Our background job has to be configured in `ConfigureServices` method:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // An 'ISecretProvider' implementation (see: https://security.arcus-azure.net/) to access the Azure Service Bus Topic resource;
    //     this will get the 'tokenSecretKey' string (configured below) and has to retrieve the connection token for the Databricks instance.
    services.AddSingleton<ISecretProvider>(serviceProvider => ...);

    services.AddDatabricksJobMetricsJob(
        baseUrl: "https://url.to.databricks.instance/" 
        // Token secret key to connect to the Databricks token.
        tokenSecretKey: "Databricks.Token");
}
```

[&larr; back](/)