# AllFrameworks.AppHost

A single Aspire AppHost that boots and deploys six frontend frameworks side by side:
Angular, Astro, Next.js, React, Svelte, and Vue. Each is wired up with
[`PublishAsNetlifySite`](../../src/Aspire.Hosting.Netlify/Extensions/JavaScriptHostingExtensions.cs)
using the full `NetlifyDeployOptions` form so the corresponding Netlify `Site` IDs
stay explicit.

## Run locally

```sh
aspire run --apphost samples/AllFrameworks.AppHost/AllFrameworks.AppHost.csproj
```

This launches all six dev servers alongside the Aspire dashboard. Each frontend's
README in [`samples/`](../) documents the underlying `npm run dev` / build commands
if you want to run them in isolation.

## Deploy to Netlify

```sh
aspire deploy --apphost samples/AllFrameworks.AppHost/AllFrameworks.AppHost.csproj
```

The pipeline builds each site, runs the Netlify CLI's `deploy` command for it, and
records state under `~/.aspire/deployments/<sha>/<environment>.json`.

## Auth precedence

Each `PublishAsNetlifySite(...)` call accepts an explicit `authToken` parameter. The
pipeline resolves the auth token in this order (highest first):

1. `authToken` parameter passed to `PublishAsNetlifySite`.
2. `NETLIFY_AUTH_TOKEN` environment variable.
3. Interactive `ntl login`.

> If you have both an `authToken` parameter **and** `NETLIFY_AUTH_TOKEN` set, the
> parameter wins and the pipeline emits an INFO/WARN log naming the deployment so
> the override is visible at runtime.
