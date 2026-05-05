---
title: CI/CD
description: Run aspire deploy from CI and publish releases to NuGet.org via OIDC trusted publishing.
---

How to run `aspire deploy` from CI and how to publish releases of this
integration to NuGet.org without a long-lived API key.

## Aspire Deploy in GitHub Actions

The repo's [`aspire-deploy.yml`](https://github.com/IEvangelist/netlify-aspire-integration/blob/main/.github/workflows/aspire-deploy.yml)
runs `aspire deploy` against the [`AllFrameworks.AppHost`](https://github.com/IEvangelist/netlify-aspire-integration/tree/main/samples/AllFrameworks.AppHost)
on demand. The deploy state file is cached between runs at
`~/.aspire/deployments/<sha>/<environment>.json` so subsequent deploys reuse the
created sites.

```yaml
env:
  NETLIFY_AUTH_TOKEN: ${{ secrets.NETLIFY_AUTH_TOKEN }}
```

The workflow expects:

- `secrets.NETLIFY_AUTH_TOKEN` — a personal access token from
  [`app.netlify.com/user/applications`](https://app.netlify.com/user/applications#personal-access-tokens).
- A Node.js 20+ runtime and the .NET 10 SDK on the runner.
- `npm install -g netlify-cli` (the workflow does this for you, but you can do
  it in advance to speed up runs).

## Publishing this package to NuGet.org (Trusted Publishing / OIDC)

NuGet.org now supports
[Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing)
via GitHub Actions OIDC. The repo's
[`publish-nuget.yml`](https://github.com/IEvangelist/netlify-aspire-integration/blob/main/.github/workflows/publish-nuget.yml)
uses `NuGet/login@v1` to swap a short-lived OIDC token for a temporary API key —
no `NUGET_API_KEY` secret required.

### One-time setup (per repo)

1. Sign in to [nuget.org](https://www.nuget.org).
2. Click your avatar → **Trusted Publishers** → **Add**.
3. Configure:
   - **Repository owner**: `IEvangelist`
   - **Repository**: `netlify-aspire-integration`
   - **Workflow filename**: `publish-nuget.yml`
   - **Environment**: _(blank, unless you use one)_.
4. Save.
5. In your GitHub repo, set a **repository variable** (not a secret) named
   `NUGET_USER` to your NuGet.org username.

### Workflow shape

```yaml
permissions:
  id-token: write
  contents: read

jobs:
  publish:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with: { fetch-depth: 0 }
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
          dotnet-quality: 'preview'
      - run: dotnet pack src/IEvangelist.Aspire.Hosting.Netlify/IEvangelist.Aspire.Hosting.Netlify.csproj -c Release -o ./artifacts

      - name: NuGet login (OIDC → short-lived API key)
        id: nuget-login
        uses: NuGet/login@v1
        with:
          user: ${{ vars.NUGET_USER }}

      - name: Push
        env:
          NUGET_API_KEY: ${{ steps.nuget-login.outputs.NUGET_API_KEY }}
        run: |
          dotnet nuget push ./artifacts/*.nupkg \
            --api-key "$NUGET_API_KEY" \
            --source https://api.nuget.org/v3/index.json \
            --skip-duplicate
```

### Why OIDC?

- **No long-lived secret to rotate** — the short-lived API key the action prints
  expires automatically.
- **Repo + workflow scoped** — the policy on NuGet.org binds publishes to a
  specific repo and workflow filename. A leak from another repo can't be used.
- **Audit trail** — every publish records the originating GitHub run.

## See also

- [NuGet Trusted Publishing docs](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing)
- [Versioning](/netlify-aspire-integration/guides/versioning/) — how MinVer drives the published version
  from git tags.
