# Quickstart.AppHost.TypeScript

A minimal TypeScript AppHost that boots a single Astro site (the one in
[`samples/astro`](../astro)) and demonstrates calling
`Aspire.Hosting.Netlify` from a multi-language AppHost via the
[Aspire Type System](https://aspire.dev/architecture/multi-language-architecture/).

## Prereqs

The first time you clone the repo, regenerate the `.modules/` folder for this
AppHost so the TypeScript compiler can resolve
`./.modules/aspire`:

```sh
cd samples/Quickstart.AppHost.TypeScript
aspire restore
```

This pulls down the `Aspire.Hosting.Netlify` integration referenced from
`aspire.config.json` and writes its TypeScript bindings into `.modules/`.

> **Local development note.** While iterating on the integration locally,
> change `aspire.config.json` to reference the local project path instead of
> the published NuGet package:
>
> ```json
> {
>   "appHost": { "path": "./apphost.ts" },
>   "integrations": [
>     { "path": "../../src/Aspire.Hosting.Netlify/Aspire.Hosting.Netlify.csproj" }
>   ]
> }
> ```

## Run

```sh
aspire run --apphost samples/Quickstart.AppHost.TypeScript/apphost.ts
```

The AppHost starts the Astro dev server alongside the Aspire dashboard.

## Deploy

```sh
aspire deploy --apphost samples/Quickstart.AppHost.TypeScript/apphost.ts
```

Auth resolution is identical to the C# variant — see
[`docs/guides/auth.md`](../../docs/src/content/docs/guides/auth.md).

## What this demonstrates

- `addNetlifyDeployPipeline` → registered via `[AspireExport]`.
- `publishAsNetlifySite({ dir, noBuild }, authToken)` → the canonical
  `[AspireExport]` overload of the C# `PublishAsNetlifySite(builder,
  NetlifyDeployOptions, authToken)`.
- `NetlifyDeployOptions` → marked `[AspireDto]`; the TypeScript shape is
  generated automatically.
- The resource graph and pipeline behave identically to the
  [C# Quickstart](../Quickstart.AppHost/).
