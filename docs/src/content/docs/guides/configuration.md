---
title: Configuration
description: Every option on NetlifyDeployOptions and how it maps to the Netlify CLI.
---

Every property on `NetlifyDeployOptions` maps to a single Netlify CLI flag. The
DTO is annotated with `[AspireDto]` so TypeScript AppHosts get an equivalent
shape automatically.

## `NetlifyDeployOptions` properties

| Property | CLI flag | Notes |
| --- | --- | --- |
| `Dir` | `--dir <path>` | The folder to deploy. Required for `NoBuild = true`. |
| `Site` | `--site <id-or-name>` | Netlify Site ID (preferred — stable across renames) or site name. Redacted in logs. |
| `Auth` | `--auth <token>` | Netlify personal access token. **Set via the `authToken` parameter or `NETLIFY_AUTH_TOKEN` env var; do not hard-code in source.** |
| `Production` | `--prod` | Deploy as a production deploy. Default: `false` (preview deploy). |
| `NoBuild` | `--no-build` | Skip the framework build step. Set automatically by the directory-only overload. |
| `BuildTimeoutSeconds` | `--timeout <seconds>` | Override the default build timeout. |
| `Message` | `--message <text>` | Free-form deploy message. Defaults to a timestamped message generated at deploy time. |
| `Filter` | `--filter <pattern>` | Limit which files are uploaded. |
| `CreateSite` | `--create-site <name>` | Create a new site with the given name and use it for this deploy. |
| `BuildContext` | `--context <name>` | Custom build context (e.g. `staging`, `qa`). |
| `Functions` | `--functions <path>` | Path to a folder of Netlify Functions. |
| `EnvironmentFiles` | `--env-file <path>` | One or more `.env` files to load before deploying. |
| `Open` | `--open` | Open the deploy preview in a browser when finished. Use only for interactive runs. |

## Authentication precedence

When the pipeline resolves the Netlify auth token it considers, in order:

1. The `authToken` `IResourceBuilder<ParameterResource>?` you passed to
   `PublishAsNetlifySite`.
2. The `NETLIFY_AUTH_TOKEN` environment variable.
3. An interactive `ntl login` flow.

If both #1 and #2 are set with different values the pipeline emits a warning
naming the deployment so the override is visible.

## Logging redaction

`Site` and `Auth` are redacted in any log output the pipeline emits. The
underlying CLI invocation may still print them in raw form when you set
`Aspire.Hosting:LogLevel` to `Trace`, so avoid `Trace` for production
runs.

## See also

- [Authentication](./auth.md)
- [Deploy pipeline](./pipeline.md)
