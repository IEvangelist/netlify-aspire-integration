---
title: Versioning
description: How MinVer derives the package version from git tags.
---

This package uses [MinVer](https://github.com/adamralph/minver) to derive its
NuGet version from git tags at build time.

## How it works

| Git state | NuGet version |
| --- | --- |
| Tag `v1.0.0` | `1.0.0` |
| Tag `v1.0.0` + 3 commits | `1.0.1-preview.0.3+abc1234` |
| Tag `v1.0.0-beta.1` | `1.0.0-beta.1` |
| Tag `v1.0.0-beta.1` + 2 commits | `1.0.0-beta.1.2+xyz5678` |
| No tags | `0.1.0-preview.0.5+def9012` |

## Tagging a release

```sh
git checkout main
git pull
git tag -a v1.0.0 -m "Release v1.0.0"
git push origin v1.0.0
```

The `publish-nuget.yml` workflow runs on every published release and pushes the
resulting package to NuGet.org via [Trusted Publishing](./ci-cd.md).

## Pre-release labels

- `v1.0.0-alpha.1` — alpha
- `v1.0.0-beta.1` — beta
- `v1.0.0-rc.1` — release candidate
- `v1.0.0-preview.1` — preview

## Local preview

```sh
dotnet tool install --global minver-cli
minver
# → e.g. 0.2.1-preview.0.7+0123abc
```

## Configuration

MinVer is configured in `src/Aspire.Hosting.Netlify/Aspire.Hosting.Netlify.csproj`:

```xml
<PropertyGroup>
  <MinVerTagPrefix>v</MinVerTagPrefix>
  <MinVerMinimumMajorMinor>0.1</MinVerMinimumMajorMinor>
  <MinVerDefaultPreReleaseIdentifiers>preview.0</MinVerDefaultPreReleaseIdentifiers>
</PropertyGroup>
```

## Troubleshooting

- **Version shows `0.0.0`.** CI must have full git history. Use
  `fetch-depth: 0` on `actions/checkout`.
- **Unexpected commit count.** Expected for commits after a tag. Cut a fresh tag
  to publish a clean version.
- **Tags not recognised.** Tags must follow `v{major}.{minor}.{patch}`.

## See also

- [CI/CD](./ci-cd.md) — the publishing workflow that consumes this version.
- [MinVer docs](https://github.com/adamralph/minver)
- [Semantic versioning](https://semver.org)
