---
title: Install
description: Install Aspire.Hosting.Netlify and its prerequisites.
---

`Aspire.Hosting.Netlify` is published to NuGet.org and targets `net10.0` with a
floating dependency on `Aspire.Hosting` 13.2.x.

## Prerequisites

- **.NET 10 SDK** — confirm with `dotnet --info`. The integration ships with the
  `Aspire.AppHost.Sdk` 13.2.4 SDK and requires .NET 10.
- **Aspire CLI** — install via `winget install Microsoft.Aspire` (Windows),
  `brew install aspire` (macOS), or follow the
  [`install.sh` script](https://aspire.dev/install.sh) on Linux. Confirm with
  `aspire --version`.
- **Node.js 20+** — needed by your frontend apps and by the Netlify CLI.
- **Netlify CLI** — the integration auto-installs `netlify-cli` globally if it
  isn't already on `PATH`. You can pre-install it yourself with
  `npm install -g netlify-cli`.

:::caution
The legacy "Aspire workload" is **obsolete** in Aspire 13.x. Do not install it.
:::

## Add the package

```sh
dotnet add package Aspire.Hosting.Netlify
```

If you're starting from scratch, scaffold an Aspire AppHost first:

```sh
mkdir MyAppHost && cd MyAppHost
aspire init
dotnet add package Aspire.Hosting.Netlify
dotnet add package Aspire.Hosting.JavaScript
dotnet add package CommunityToolkit.Aspire.Hosting.JavaScript.Extensions
```

## Verify

Run any AppHost that references the package and you should see no missing
references when you call `PublishAsNetlifySite(...)`:

```csharp
using Aspire.Hosting;
using Aspire.Hosting.JavaScript;

var builder = DistributedApplication.CreateBuilder(args);

builder.Pipeline.AddNetlifyDeployPipeline();

builder.AddJavaScriptApp("astro", "../astro")
    .PublishAsNetlifySite("dist");

builder.Build().Run();
```

## Next steps

- [C# Quickstart](./quickstart-csharp.md)
- [TypeScript Quickstart](./quickstart-typescript.md)
