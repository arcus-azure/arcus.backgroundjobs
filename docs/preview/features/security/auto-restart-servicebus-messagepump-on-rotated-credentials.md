---
title: "Automatically restart Azure Service Bus message pump on rotated credentials"
layout: default
---

# Automatic Azure Key Vault credentials rotation

The library `Arcus.BackgroundJobs.KeyVault` provides an extension on the message pump to restart the pump automatically when the credentials of the pump stored in Azure Key Vault are changed.
This feature allows more reliable restarting instead of relying on authentication exceptions that may be throwed during the lifetime of the message pump.

## How does this work?

A background job is polling for `SecretNewVersionCreated` events on an Azure Service Bus Topic for the secret that stores the connection string.

That way, when the background job receives a new Key Vault event, it will get the latest connection string, restart the message pump and authenticate with the latest credentials.

### Installation

This features requires to install our NuGet package:

```shell
PM > Install-Package Arcus.BackgroundJobs.KeyVault
```

### Usage

When the package is installed, you'll be able to use the extension in your application:

```csharp
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // You should have a unique Job ID to identity the message pump so the automatic process knows which pump to restart.
        string jobId = Guid.NewGuid().ToString();
    
        string secretName = hostContext.Configuration["ARCUS_KEYVAULT_CONNECTIONSTRINGSECRETNAME"];
        services.AddServiceBusQueueMessagePump(secretName, options => options.JobId =   
                .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();

         // This extension will be available to you once you installed the package.
         services.AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob(
             jobId: jobId, 
             subscriptionNamePrefix: "TestSub", 
             
             // The secret key where the Azure Service Bus Topic connection string is located that the background job will use to receive the Azure Key vault events.
             serviceBusTopicConnectionStringSecretKey: "ARCUS_KEYVAULT_SECRETNEWVERSIONCREATED_CONNECTIONSTRING",
             
             // The secret key where the Azure Service Bus connection string is located that your target message pump uses.
             // This secret key name will be used to check if the received Azure Key Vault event is from this secret or not.
             messagePumpConnectionStringKey: secretName,
    
             // The maximum amount of thrown unauthorized exceptions that your message pump should allow before it should restart either way.
             // This amount can be used to either wait for an Azure Key Vault event or rely on the thrown unauthorized exceptions.
             maximumUnauthorizedExceptionsBeforeRestart: 5)
    }
}
```