---
title: Quickstart (C#)
description: Get a Netlify deploy running through Aspire in under a minute.
---

## 1. Scaffold an AppHost

```sh
mkdir MyAppHost && cd MyAppHost
aspire init
dotnet add package Aspire.Hosting.Netlify
dotnet add package Aspire.Hosting.JavaScript
dotnet add package CommunityToolkit.Aspire.Hosting.JavaScript.Extensions
```

## 2. Wire up your frontend

Drop your built static frontend (e.g. an Astro `dist/` folder) next to the
AppHost, then point at it:

```csharp
using Aspire.Hosting;
using Aspire.Hosting.JavaScript;

var builder = DistributedApplication.CreateBuilder(args);

// Optional: bind an auth token from configuration / user secrets.
var authToken = builder.AddParameterFromConfiguration(
    "netlify-token", "NETLIFY_AUTH_TOKEN", secret: true);

builder.Pipeline.AddNetlifyDeployPipeline();

builder.AddJavaScriptApp("astro", "../astro")
    .PublishAsNetlifySite("dist", authToken);

builder.Build().Run();
```

The single-argument overload of `PublishAsNetlifySite` is a shortcut for
`new NetlifyDeployOptions { Dir = "dist", NoBuild = true }`. Use the full
`NetlifyDeployOptions` form when you need to specify a `Site`, `Message`,
`Production` flag, etc.

## 3. Run locally

```sh
aspire run
```

Aspire starts your frontend's dev server alongside the dashboard. Browse to the
dashboard URL printed in the terminal to inspect the resource tree.

## 4. Deploy

```sh
aspire deploy
```

The pipeline:

1. Resolves the Netlify CLI (auto-installs if missing).
2. Resolves your auth token (parameter > env var > `ntl login`).
3. Resolves the Netlify site ID (or creates a new site).
4. Runs `netlify deploy ...` and records the result.

See [Configuration](./configuration.md) for the full list of options, and
[Authentication](./auth.md) for the precedence rules.

## Working sample

[`samples/Quickstart.AppHost`](https://github.com/IEvangelist/netlify-aspire-integration/tree/main/samples/Quickstart.AppHost)
is the runnable version of this guide. Clone the repo and run:

```sh
aspire run --apphost samples/Quickstart.AppHost/Quickstart.AppHost.csproj
```
