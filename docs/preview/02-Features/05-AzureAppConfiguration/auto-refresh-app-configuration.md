---
title: "Automatically refresh Azure App Configuration"
layout: default
---

# Automatically refresh Azure App Configuration
The `Arcus.BackgroundJobs.AppConfiguration` library provides a background job to automatically refresh [configuration values](https://docs.microsoft.com/en-us/dotnet/core/extensions/configuration) in your application that comes from the Azure App Configuration.

## Installation
To use these features, you have to install the following package:

```shell
PM > Install-Package Arcus.BackgroundJobs.AzureAppConfiguration
```

## Setup
To automatically notify your `IConfiguration` instance in your application for any change in the remote Azure App Configuration, you need some setup.
We use the events from Azure App Configuration which will be send towards an Azure Service Bus Topic. The background job will look for those events on the topic, and will in turn refresh the `IConfiguration`.

1. Create an Azure App Configuration resource
2. Create an Azure Service Bus Topic resource
3. Create an event subscription on the App Configuration resource to send events to the Service Bus Topic


Both the connection string of the Azure App Configuration and the Azure Service Bus Topic will be needed in the next section, so make sure you have those.

## Usage
Our background job requries both the configuration of Azure App Configuration while setting up the `IConfiguration`, 
as well as the [Arcus secret store](https://security.arcus-azure.net/features/secret-store) which will retrieve the connection string of the Azure Service Bus Topic.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class Program
{
    public static void Main(string[] args)
    {
        IHostBuilder hostBuilder = CreateHostBuilder(args);
        hostBuilder.Build().Run();
    }

    private static IHostBuilder CreatHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
                    // Make sure that you register the Arcus secret store so the background job has access to the Azure Service Bus Topic connection string,
                    // where the Azure App Configuration events are pushed.
                    .ConfigureSecretStore((stores => stores.AddAzureKeyVaultWithManagedIdentity("<your-key-vault-uri>"))
                    .ConfigureAppConfiguration(configBuilder => ConfigureAppConfiguration(configBuilder))
                    .ConfigureServices(services => ConfigureServices(services));
    }

    private static void ConfigureAppConfiguration(IConfigurationBuilder configBuilder)
    {
        configBuilder.AddAzureAppConfiguration(appConfigOptions =>
        {
            appConfigOptions.Connect("<your-azure-app-configuration-connection-string>")
                            // Specifies which Azure App Configuration key you want to automatically updated.
                            .Register("<your-azure-app-configuration-key>");
        });
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // Adds the automatic Azure App Configuration refresh background job
        services.AddAutoRefreshAppConfigurationBackgroundJob(
            // Prefix of the Azure Service Bus Topic subscription;
            //    this allows the background jobs to support applications that are running multiple instances, processing the same type of events, without conflicting subscription names.
            subscriptionPrefix: "TestSub",
            // Connection string secret key to a Azure Service Bus Topic.
            serviceBusTopicConnectionStringSecretKey: "MySecretKeyToServiceBusTopicConnectionString");
    }
}
```

[&larr; back](/)
