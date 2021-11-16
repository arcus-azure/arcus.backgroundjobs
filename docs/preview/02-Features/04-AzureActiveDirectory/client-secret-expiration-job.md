---
title: "Check Applications in Azure Active Directory for client secrets that have expired or will expire in the near future"
layout: default
---

# Check Applications in Azure Active Directory for client secrets that have expired or will expire in the near future

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

Our background job has to be configured in `ConfigureServices` method:

```csharp
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // An `IEventGridPublisher` implementation where the CloudEvents are sent to
        services.AddSingleton<IEventGridPublisher>(eventGridPublisher);
    
        services.AddClientSecretExpirationJob(options => 
        {
            // The expiration threshold for the client secrets. 
            // If a client secret has an EndDateTime within the `ExpirationThreshold` a `ClientSecretAboutToExpire` CloudEvent is used.
            // If a client secret has an EndDateTime that is in the past a `ClientSecretExpired` event is used.
            options.ExpirationThreshold = 14;
            
            // The uri to use in the CloudEvent
            options.EventUri = new Uri("https://github.com/arcus-azure/arcus.backgroundjobs");
        });
    }
}
```

[&larr; back](/)
