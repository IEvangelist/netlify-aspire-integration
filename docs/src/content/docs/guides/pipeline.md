---
title: Deploy pipeline
description: The pipeline steps that AddNetlifyDeployPipeline registers, and how to compose with them.
---

`AddNetlifyDeployPipeline` registers a sequence of pipeline steps that turn an
`aspire deploy` invocation into a working Netlify deploy. The steps run in order,
once per deploy run:

1. **Install Netlify CLI** — runs `npm install -g netlify-cli` if `ntl` cannot be
   resolved on `PATH`. After install, the npm-global bin directory is prepended
   to the current process's `PATH` so subsequent steps find the binary.
2. **Authenticate with Netlify** — resolves the auth token using the documented
   [precedence](/guides/auth/). If the token is empty, runs `ntl login` once.
3. **Resolve / create site** — looks up the Netlify site ID via `--site` or
   creates a new one with `--create-site <name>` when `CreateSite` is set.
4. **Deploy** — invokes `netlify deploy` with the configured arguments.

## Customising the pipeline

`AddNetlifyDeployPipeline` is an extension on `IDistributedApplicationPipeline`.
You can compose extra steps before or after it using the standard pipeline APIs.

```csharp
builder.Pipeline
    .AddStep(MyCustomPreflight)
    .AddNetlifyDeployPipeline()
    .AddStep(MyCustomNotification);
```

## Diagnostics

- Each step shows a top-level entry in the Aspire dashboard's pipeline view with
  its arguments redacted.
- Failed steps include the underlying Netlify CLI output. To see the full
  `netlify deploy --json` payload, raise the `Aspire.Hosting:LogLevel` to
  `Trace` for local debugging only.
- `~/.aspire/deployments/<sha>/<environment>.json` records the successful deploy
  state. CI can cache and restore this folder to track deploys across runs.

## See also

- [Configuration](/guides/configuration/)
- [Authentication](/guides/auth/)
- [Troubleshooting](/guides/troubleshooting/)
