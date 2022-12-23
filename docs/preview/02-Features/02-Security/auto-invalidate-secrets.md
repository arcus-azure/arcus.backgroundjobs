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

To make this automation operational, following Azure Resources has to be used:
* Azure Key Vault instance
* Azure Service Bus Topic
* Azure Event Grid subscription for `SecretNewVersionCreated` events that are sent to the Azure Service Bus Topic

## Usage

Make sure that you have registered the [Arcus secret store](https://security.arcus-azure.net/features/secret-store/) so an `ISecretProvider`/`ICachedSecretProvider` is available to auto invalidate.
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
                .ConfigureSecretStore((context, config, builder) =>
                {
#if DEBUG
                    builder.AddConfiguration(config);
#endif
                    var keyVaultName = config["KeyVault_Name"];
                    builder.AddEnvironmentVariables()
                           .AddAzureKeyVaultWithManagedServiceIdentity($"https://{keyVaultName}.vault.azure.net");
                });
    }
}
```

With an Arcus secret store configured, you can safely add the background job to your application.

🥇 [Managed Identity authentication](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview) is the recommended approach to interact with Azure Service Bus, which the background job will run against.

```csharp
using Arcus.Security.Core;
using Arcus.Security.Core.Caching;
using Microsoft.Extensions.DependencyInjection;

Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        // Register background job via managed identity authentication.
        services.AddAutoInvalidateKeyVaultSecretUsingManagedIdentityBackgroundJob(
            // Azure Service Bus topic name where the Azure Key Vault events will be placed.
            "<topic-name>",
            // Prefix of the Azure Service Bus topic subscription;
            //    this allows the background jobs to support applications that are running multiple instances, processing the same type of events, without conflicting subscription names.
            subscriptionNamePrefix: "MyPrefix",
            // Azure Service Bus namespace where the topic is located.
            "<servicebus-namespace>");

        // Register background job via connection string which is stored in the Arcus secret store.
        services.AddAutoInvalidateKeyVaultSecretBackgroundJob(
            // Prefix of the Azure Service Bus Topic subscription;
            //    this allows the background jobs to support applications that are running multiple instances, processing the same type of events, without conflicting subscription names.
            subscriptionNamePrefix: "MyPrefix",

            // Connection string secret key to a Azure Service Bus topic.
            // Make sure that this key is available in the Arcus secret store.
            serviceBusTopicConnectionStringSecretKey: "MySecretKeyToServiceBusTopicConnectionString");
    })
```