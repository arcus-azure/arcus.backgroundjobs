---
title: "Arcus Background Jobs"
layout: default
permalink: /
slug: /
sidebar_label: Welcome
---

# Introduction
Arcus BackgroundJobs provides common repeated tasks background tasks that strengthens an application. This goes from automatic secret invalidation upon Azure Key Vault secrets to publish an event on Azure EventGrid upon a potential expired client secret in Azure. This collection of background tasks has grown from real-life application builder that wanted to re-use existing functionality in a safe and trusted manner.

# Installation
The Arcus BackgroundJobs can be installed via NuGet, for example:

```shell
PM > Install-Package Arcus.BackgroundJobs.CloudEvents
```

# License
This is licensed under The MIT License (MIT). Which means that you can use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the web application. But you always need to state that Codit is the original author of this web application.

*[Full license here](https://github.com/arcus-azure/arcus.backgroundjobs/blob/master/LICENSE)*
