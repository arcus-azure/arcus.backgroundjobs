---
title: "Check Applications in Azure Active Directory for potential expired client secrets"
layout: default
---

# Check Applications in Azure Active Directory for potential expired client secrets

Applications within Azure Active Directory can have multiple client secrets, unfortunately there is no out-of-the-box way of getting notified when a secret is about to expire. 
The `Arcus.BackgroundJobs.AzureActiveDirectory` library provides a background job to periodically check applications in Azure Active Directory for client secrets that have expired or will expire in the near future and will send an either a `ClientSecretAboutToExpire` or `ClientSecretExpired` CloudEvent to EventGrid.

## Installation

To use this feature, you have to install the following package:

```shell
PM > Install-Package Arcus.BackgroundJobs.AzureActiveDirectory
```

## How does it work?

This automation works by periodically querying the Microsoft Graph API using the `GraphServiceClient` for applications. If the application contains any client secrets the `EndDateTime` of the secret is validated against the current date and a configurable threshold to determine if the secret has expired or will soon expire.
If this is the case either a `ClientSecretAboutToExpire` or `ClientSecretExpired` CloudEvent is sent to EventGrid.

## Usage

The background job can easily be added to any .NET hosted application. It is recommended though to create a dedicated background application to run background jobs.

```csharp
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

public class Program
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Make sure that the application has a Microsoft EventGrid publisher client configured to where the CloudEvents are sent to.
        // For more information on the Arcus EventGrid publisher: https://eventgrid.arcus-azure.net/Features/publishing-events.
        services.AddAzureClients(clients =>
        {
            // Arcus provides an additional extension overload that lets us pass-in a secret name instead of the authentication key directly.
            // This secret name will be used to contact the Arcus secret store to retrieve the authentication key. (more info: https://security.arcus-azure.net/features/secret-store)
            // Note that this Arcus extension needs an correlation system to configure service-to-service correlation. This is by default available in Arcus HTTP middleware and Messaging components (more info: https://observability.arcus-azure.net/Features/correlation).
            clients.AddEventGridPublisherClient("https://az-eventgrid-topic-endpoint", "Authentication.Key.Secret.Name");
        });

        services.AddClientSecretExpirationJob(options => 
        {
            // The expiration threshold for the client secrets. 
            // If a client secret has an EndDateTime within the `ExpirationThreshold` a `ClientSecretAboutToExpire` CloudEvent is used.
            // If a client secret has an EndDateTime that is in the past a `ClientSecretExpired` event is used.
            options.ExpirationThreshold = 14;

            // The hour at which the job will during the day, the value can range from 0 to 23
            options.RunAtHour = 0;

            // The RunImmediately option can be used to indicate that the job should run immediately
            options.RunImmediately = false;
            
            // The uri to use in the CloudEvent
            options.EventUri = new Uri("https://github.com/arcus-azure/arcus.backgroundjobs");
        });
    }
}
```