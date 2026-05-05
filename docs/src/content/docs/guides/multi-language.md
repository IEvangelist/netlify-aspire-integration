---
title: Multi-language (Aspire Type System)
description: How the integration's [AspireExport] / [AspireDto] attributes flow through to TypeScript AppHosts.
---

`Aspire.Hosting.Netlify` is annotated with the [Aspire Type System](https://aspire.dev/architecture/multi-language-architecture/)
attributes so a TypeScript AppHost can consume it directly.

## What gets exported

| C# member | TS surface | Notes |
| --- | --- | --- |
| `PublishAsNetlifySite(builder, NetlifyDeployOptions, authToken)` | `publishAsNetlifySite(jsApp, options, authToken)` | Canonical export — full options shape. |
| `PublishAsNetlifySite(builder, string dir, authToken)` | _(C# only)_ | Marked `[AspireExportIgnore]`; TS users pass `{ dir, noBuild: true }` to the canonical overload. |
| `WithNpmCommand(builder, string args)` | `withNpmCommand(npm, args)` | The no-callback variant. |
| `WithNpmRunCommand(builder, string scriptName)` | `withNpmRunCommand(npm, scriptName)` | The no-callback variant. |
| `AddNetlifyDeployPipeline(pipeline)` | `addNetlifyDeployPipeline(pipeline)` | Registers the deploy steps on a pipeline. |
| `NetlifyDeployOptions` | `NetlifyDeployOptions` (DTO) | Marked `[AspireDto]`. |
| `NetlifyDeploymentResource` | `NetlifyDeploymentResource` | Properties: `name`, `workingDirectory`, `options`, `buildDirectory`, `siteName`, `deploymentEnvironment`. |
| `NpmCommandResource` | `NpmCommandResource` | Single property: `scriptName`. |

## What's deliberately not exported

- **Callback-form overloads** of `WithNpmCommand` / `WithNpmRunCommand` — inline
  C# `Action<...>` callbacks aren't ATS-friendly.
- **`Annotations` collections** on resources — they're attached via separate
  builder methods, not exposed as exported properties.

## Iterating locally

```sh
# In your TypeScript AppHost folder.
aspire restore     # regenerates .modules/ from referenced integrations
aspire run         # boots the AppHost
```

If `.modules/` is missing or stale, `aspire restore` is the way back to a
known-good state. Inspect `.modules/aspire.ts` to see the current exported
surface.

## See also

- [Quickstart](/netlify-aspire-integration/guides/quickstart/) — C# and TypeScript walkthrough.
- [Configuration](/netlify-aspire-integration/guides/configuration/)
- [Aspire.dev — multi-language architecture](https://aspire.dev/architecture/multi-language-architecture/)
- [Aspire.dev — multi-language integration authoring](https://aspire.dev/extensibility/multi-language-integration-authoring/)
