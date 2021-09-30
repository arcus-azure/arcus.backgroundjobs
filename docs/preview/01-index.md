---
title: "Arcus Background Jobs"
layout: default
permalink: /
slug: /
sidebar_label: Welcome
---

[![NuGet Badge](https://buildstats.info/nuget/Arcus.BackgroundJobs.CloudEvents?includePreReleases=true)](https://www.nuget.org/packages/Arcus.BackgroundJobs.CloudEvents/)

# Installation

The Arcus BackgroundJobs can be installed via NuGet:

```shell
PM > Install-Package Arcus.BackgroundJobs.CloudEvents
```

For more granular packages we recommend reading the documentation.

# Features

- **General**
    - [Securely Receive CloudEvents](features/general/receive-cloudevents-job)
- **Azure App Configuration**
    - [Automatically refresh configuration values from Azure App Configuration](features/appconfiguration/auto-refresh-app-configuration)
- **Databricks**
    - [Measure Databricks job run outcomes as metric](features/databricks/job-metrics)
    - [Interact with Databricks to gain insights](features/databricks/gain-insights)
- **Security**
    - [Automatically invalidate cached secrets from Azure Key Vault](features/security/auto-invalidate-secrets)

# License
This is licensed under The MIT License (MIT). Which means that you can use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the web application. But you always need to state that Codit is the original author of this web application.

*[Full license here](https://github.com/arcus-azure/arcus.backgroundjobs/blob/master/LICENSE)*
