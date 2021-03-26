---
title: "Automatically Invalidate Azure Key Vault Secrets"
layout: default
---

# Automatically Invalidate Azure Key Vault Secrets

The `Arcus.BackgroundJobs.KeyVault` library provides a background job to automatically invalidate cached Azure Key Vault secrets from an `ICachedSecretProvider` instance of your choice.

## How does it work?

<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Farcus-azure%2Farcus.backgroundjobs%2Fmaster%2Fdeploy%2Farm%2Fazure-key-vault-job.json" target="_blank">
    <img src="https://azuredeploy.net/deploybutton.png"/>
</a>


This automation works by subscribing on the `SecretNewVersionCreated` event of an Azure Key Vault resource and placing those events on a Azure Service Bus Topic; which we process in our background job.

![Automatically Invalidate Azure Key Vault Secrets](/media/Azure-Key-Vault-Job.png)

To make this automation opperational, following Azure Resources has to be used:
* Azure Key Vault instance
* Azure Service Bus Topic
* Azure Event Grid subscription for `SecretNewVersionCreated` events that are sent to the Azure Service Bus Topic

## Usage

Our background job has to be configured in `ConfigureServices` method:

```csharp
using Arcus.Security.Core;
using Arcus.Security.Core.Caching;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // An 'ISecretProvider' implementation (see: https://security.arcus-azure.net/) to access the Azure Service Bus Topic resource;
        //     this will get the 'serviceBusTopicConnectionStringSecretKey' string (configured below) and has to retrieve the connection string for the topic.
        services.AddSingleton<ISecretProvider>(serviceProvider => ...);
    
        // An `ICachedSecretProvider` implementation which secret keys will automatically be invalidated.
        services.AddSingleton<ICachedSecretProvider>(serviceProvider => new CachedSecretProvider(mySecretProvider));
    
        services.AddAutoInvalidateKeyVaultSecretBackgroundJob(
            // Prefix of the Azure Service Bus Topic subscription;
            //    this allows the background jobs to support applications that are running multiple instances, processing the same type of events, without conflicting subscription names.
            subscriptionNamePrefix: "MyPrefix"
    
            // Connection string secret key to a Azure Service Bus Topic.
            serviceBusTopicConnectionStringSecretKey: "MySecretKeyToServiceBusTopicConnectionString");
    }
}
```

[&larr; back](/)
