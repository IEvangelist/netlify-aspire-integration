---
title: 13.2 upgrade notes
description: What changed when Aspire.Hosting.Netlify moved from Aspire 13.1 to 13.2.4.
---

`Aspire.Hosting.Netlify` is now built against **Aspire 13.2.4** (the latest stable
release in the 13.x line).

## What changed

- All Aspire packages are pinned to **13.2.4** in
  [`Directory.Packages.props`](https://github.com/IEvangelist/netlify-aspire-integration/blob/main/Directory.Packages.props).
- The library and AppHosts use the new
  [Aspire Type System](https://aspire.dev/architecture/multi-language-architecture/)
  attributes so a TypeScript AppHost gets a generated `publishAsNetlifySite(...)`
  surface. See [Multi-language](/netlify-aspire-integration/guides/multi-language/).
- A directory-only `PublishAsNetlifySite("dist", authToken)` overload is now
  available alongside the canonical `NetlifyDeployOptions` overload.
- The repo's framework demos moved from `src/<framework>/` to
  `samples/<framework>/`. The AppHost was renamed from `Netlify.AppHost` to
  `AllFrameworks.AppHost` to reflect what it actually demonstrates. A new
  `Quickstart.AppHost` showcases the directory-only overload.
- NuGet.org publishing now uses [OIDC Trusted Publishing](/netlify-aspire-integration/guides/ci-cd/);
  no `NUGET_API_KEY` secret is required.

## Behaviour changes (intentional)

| Before | After | Why |
| --- | --- | --- |
| `NETLIFY_AUTH_TOKEN` env var clobbered any explicit `authToken` parameter. | The parameter wins. The pipeline emits a warning when both are set with different values. | The previous behaviour silently overrode user intent. |
| `NetlifyDeployOptions.CreateSite = "name"` emitted `--create-site` only — without the supplied name. | `--create-site <name>` is emitted. | The previous behaviour silently dropped the user-supplied site name. |
| If `ntl` was not initially on `PATH`, the install step succeeded but the deploy step still failed because the inherited `PATH` was stale. | After install, the npm-global bin directory is prepended to the current process's `PATH`. The deploy step re-resolves the `ntl` binary. | Restores the documented "auto-install" workflow. |

## Package changes

- `Aspire.Hosting.AppHost` is **obsolete** in 13.2 and is no longer referenced by
  AppHost projects. AppHosts only need the `Aspire.AppHost.Sdk` SDK and an
  `Aspire.Hosting.JavaScript` package reference.

## Migrating your AppHost

If you upgraded from an earlier 13.x release of this integration, no code
changes are required. Run:

```sh
aspire update
dotnet restore
```

If you want to take advantage of the directory-only overload:

```diff
- .PublishAsNetlifySite(new NetlifyDeployOptions { Dir = "dist", NoBuild = true })
+ .PublishAsNetlifySite("dist")
```

## See also

- [What's new in Aspire 13.2](https://aspire.dev/whats-new/aspire-13-2/)
- [Aspire Type System](https://aspire.dev/architecture/multi-language-architecture/)
- [Configuration](/netlify-aspire-integration/guides/configuration/)
- [Authentication](/netlify-aspire-integration/guides/auth/)
