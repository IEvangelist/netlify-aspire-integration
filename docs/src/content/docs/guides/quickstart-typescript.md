---
title: Quickstart (TypeScript)
description: Use Aspire.Hosting.Netlify from a TypeScript AppHost via the Aspire Type System.
---

Aspire 13.2 introduced the [Aspire Type System](https://aspire.dev/architecture/multi-language-architecture/),
which lets a TypeScript AppHost call into C# integrations through generated
modules under `.modules/`. `Aspire.Hosting.Netlify` is annotated with the relevant
`[AspireExport]` / `[AspireDto]` attributes so its public surface flows through
to TypeScript automatically.

## 1. Scaffold a TypeScript AppHost

```sh
mkdir MyAppHost && cd MyAppHost
aspire init --language typescript
```

This generates an `apphost.ts`, `aspire.config.json`, `package.json`, and the
`.modules/` folder that holds the generated TypeScript surface for any
integrations you add.

## 2. Reference the integration

Edit `aspire.config.json` to point at the integration's NuGet package (or a
project path during local development):

```json
{
  "appHost": { "path": "./apphost.ts" },
  "integrations": [
    { "package": "Aspire.Hosting.Netlify" }
  ]
}
```

:::tip
While developing locally, use a `path` reference instead of `package`:
`{ "path": "../path/to/Aspire.Hosting.Netlify.csproj" }`.
:::

Then regenerate the modules:

```sh
aspire restore
```

You should now see `publishAsNetlifySite`, `addNetlifyDeployPipeline`,
`withNpmCommand`, and `withNpmRunCommand` exported from `.modules/aspire.ts`.

## 3. Author the AppHost

```ts
// apphost.ts
import { Aspire } from "./.modules/aspire";

const builder = Aspire.createBuilder();

const authToken = builder.addParameterFromConfiguration(
  "netlify-token", "NETLIFY_AUTH_TOKEN", { secret: true });

builder.pipeline.addNetlifyDeployPipeline();

builder.addJavaScriptApp("astro", "../astro")
  .publishAsNetlifySite({ dir: "dist", noBuild: true }, authToken);

builder.build().run();
```

## 4. Run and deploy

```sh
aspire run
aspire deploy
```

The behaviour matches the [C# quickstart](/guides/quickstart-csharp/) — same
pipeline, same auth resolution, same Netlify CLI integration.

## See also

- [Multi-language](/guides/multi-language/) — how the ATS surface is generated.
- [`samples/Quickstart.AppHost.TypeScript`](https://github.com/IEvangelist/netlify-aspire-integration/tree/main/samples/Quickstart.AppHost.TypeScript)
  — a runnable TypeScript AppHost wired up against this integration.
