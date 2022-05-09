# Arcus Background Jobs

[![Build Status](https://dev.azure.com/codit/Arcus/_apis/build/status/Commit%20builds/CI%20-%20Arcus.BackgroundJobs?branchName=master)](https://dev.azure.com/codit/Arcus/_build/latest?definitionId=794&branchName=master)
[![NuGet Badge](https://buildstats.info/nuget/Arcus.BackgroundJobs.CloudEvents?includePreReleases=true)](https://www.nuget.org/packages/Arcus.BackgroundJobs.CloudEvents/)

Background job development in a breeze.

![Arcus](https://raw.githubusercontent.com/arcus-azure/arcus/master/media/arcus.png)

# Installation
The background jobs packages are easy to install via NuGet.

## General
  - [CloudEvents background jobs](https://background-jobs.arcus-azure.net/Features/General/receive-cloudevents-job): securely receive CloudEvents which allows workloads to asynchronously process events from other components without exposing a public endpoint.
    ```shell
    PM > Install-Package Arcus.BackgroundJobs.CloudEvents
    ```
## Security
  - Azure Active Directory background jobs notifies event subscriptions on expired client secrets in an Azure Active Directory.
    ```shell
    PM > Install-Package Arcus.BackgroundJobs.AzureActiveDirectory
    ```
  - Azure Key Vault background jobs allows [automatic invalidation of Azure Key Vault secrets](https://background-jobs.arcus-azure.net/Features/Security/auto-invalidate-secrets) and [automatic message pump restart upon rotated Azure Key Vault credentials](https://background-jobs.arcus-azure.net/Features/Security/auto-restart-servicebus-messagepump-on-rotated-credentials)
    ```shell
    PM > Install-Package Arcus.BackgroundJobs.KeyVault
    ```
## Other
  - [Databricks background jobs](https://background-jobs.arcus-azure.net/Features/Databricks/gain-insights) allows interaction with run Databricks jobs to gain insights at the resutls.
    ```shell
    PM > Install-Package Arcus.BackgroundJobs.Databricks
    ```

For a more thorough overview, we recommend reading our [documentation](#documentation).

# Documentation
All documentation can be found on [here](https://background-jobs.arcus-azure.net/).

# Customers
Are you an Arcus user? Let us know and [get listed](https://bit.ly/become-a-listed-arcus-user)!

# License Information
This is licensed under The MIT License (MIT). Which means that you can use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the web application. But you always need to state that Codit is the original author of this web application.

Read the full license [here](https://github.com/arcus-azure/arcus.backgroundjobs/blob/master/LICENSE).
