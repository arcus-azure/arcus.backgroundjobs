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

Make sure that you have registered the [Arcus secret store](https://security.arcus-azure.net/features/secret-store/) so an `ISecretProvider` is available to retrieve the connection token for the Databricks instance.
This is usually done in the `Program.cs`. See our [dedicated documentation](https://security.arcus-azure.net/features/secret-store/) for more information on the secret store.

```csharp
using Microsoft.Extensions.Hosting;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) => 
            {
                config.AddJsonFile("appsettings.json")
                      .AddJsonFile("appsettings.Development.json");
            })
            .ConfigureSecretStore((context, config, builder) =>
            {
#if DEBUG
                    builder.AddConfiguration(config);
#endif
                    var keyVaultName = config["KeyVault_Name"];
                    builder.AddEnvironmentVariables()
                           .AddAzureKeyVaultWithManagedServiceIdentity($"https://{keyVaultName}.vault.azure.net");
            })
            .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
    }
}
```

Our background job has to be configured in `ConfigureServices` method:

```csharp
using Arcus.Security.Core;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Simplest registration of the scheduler job:
        services.AddDatabricksJobMetricsJob(
            baseUrl: "https://url.to.databricks.instance/"
            // Token secret key to connect to the Databricks token.
            // Make sure that this key is available in the Arcus secret store.
            tokenSecretKey: "Databricks.Token");

        // Customized registration of the scheduler job:
        services.AddDatabricksJobMetricsJob(
            baseUrl: "https://url.to.databricks.instance/"
            // Token secret key to connect to the Databricks token.
            // Make sure that this key is available in the Arcus secret store.
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
