---
title: Troubleshooting
description: Common failure modes and fixes when using Aspire.Hosting.Netlify.
---

Common failure modes and fixes.

## "ntl: command not found" after install step succeeded

The integration installs `netlify-cli` globally and prepends the npm-global bin
directory to the current process's `PATH`. If your runner mounts a custom npm
prefix, set `PATH` explicitly before running `aspire deploy`:

```sh
export PATH="$(npm config get prefix)/bin:$PATH"
aspire deploy
```

## "401 Unauthorized" during deploy

The auth token resolved at deploy time is invalid or expired.

- Inspect which source the pipeline used — it logs an INFO line if the token came
  from the explicit parameter, or a WARN line if both the parameter and
  `NETLIFY_AUTH_TOKEN` were set with different values.
- Regenerate a new token at
  [`app.netlify.com/user/applications`](https://app.netlify.com/user/applications#personal-access-tokens).

## "site not found" after enabling `CreateSite`

`CreateSite` must be a non-empty, whitespace-free string. The pipeline now skips
the create-site path when `CreateSite` is empty or whitespace, so an unset value
falls back to `Site` resolution.

## Deploy succeeds but the site still serves the old build

`PublishAsNetlifySite("dist", ...)` deploys the directory as-is (`NoBuild = true`).
Make sure your build step (`npm run build`) ran *before* `aspire deploy` — the
`AllFrameworks.AppHost` workflow shows the typical pattern in CI.

## TypeScript AppHost: `publishAsNetlifySite` is missing from `.modules/aspire.ts`

The integration must be referenced in `aspire.config.json` and the TypeScript
project must be restored. Run:

```sh
aspire restore
```

If exports still don't appear, make sure the version pinned in your
`aspire.config.json` is at least 13.2.x (the first release with ATS support).

## Aspire dashboard shows the deploy step as orange but no error

The pipeline emits a warning when the auth resolution detects two sources of
truth. Check the dashboard's pipeline log for a line like
`"Both an authToken parameter and NETLIFY_AUTH_TOKEN are set..."` — that's a
heads-up, not a failure. The parameter wins per [precedence](/netlify-aspire-integration/guides/auth/).

## Where state lives

Per-deploy state files are written to:

```text
~/.aspire/deployments/<sha256-of-AppHost-csproj-name>/<environment>.json
```

CI workflows can cache and restore this directory to keep created site IDs
across runs.
