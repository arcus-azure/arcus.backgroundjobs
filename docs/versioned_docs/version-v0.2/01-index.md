---
title: "Arcus Background Jobs"
layout: default
permalink: /
slug: /
sidebar_label: Welcome
---

# Installation

The Arcus BackgroundJobs can be installed via NuGet, for example:

```shell
PM > Install-Package Arcus.BackgroundJobs.CloudEvents
```

# Features

- **General**
    - [Securely Receive CloudEvents](./02-Features/01-General/receive-cloudevents-job.md)
- **Security**
    - [Automatically invalidate cached secrets from Azure Key Vault](./02-Features/02-Security/auto-invalidate-secrets.md)
- **Databricks**
    - [Measure Databricks job run outcomes as metric](./02-Features/03-Databricks/job-metrics.md)
    - [Interact with Databricks to gain insights](./02-Features/03-Databricks/gain-insights.md)

# License
This is licensed under The MIT License (MIT). Which means that you can use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the web application. But you always need to state that Codit is the original author of this web application.

*[Full license here](https://github.com/arcus-azure/arcus.backgroundjobs/blob/master/LICENSE)*
