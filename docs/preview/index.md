---
title: "Home"
layout: default
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
    - [Securely Receive CloudEvents](features/cloudevent/receive-cloudevents-job)
- **Security**
    - [Automatically invalidate cached secrets from Azure Key Vault](features/security/auto-invalidate-secrets)
- **Telemetry**
    - [Repeatedly report finished Databricks job runs](features/databricks/repeatedly-query-report)

# License
This is licensed under The MIT License (MIT). Which means that you can use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the web application. But you always need to state that Codit is the original author of this web application.

*[Full license here](https://github.com/arcus-azure/arcus.backgroundjobs/blob/master/LICENSE)*
