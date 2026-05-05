---
title: Astro
description: Wire up an Astro static site for Netlify deploys via Aspire.Hosting.Netlify.
---

The Astro sample under [`samples/astro`](https://github.com/IEvangelist/netlify-aspire-integration/tree/main/samples/astro)
uses Astro's static site generator. `npm run build` writes prerendered assets to
`dist/`, which the integration deploys directly.

## AppHost wiring

```csharp
builder.AddJavaScriptApp("astro", "../astro")
    .WithHttpEndpoint(targetPort: 4321, env: "PORT")
    .PublishAsNetlifySite("dist", authToken);
```

Astro is the simplest fit for the directory-only `PublishAsNetlifySite("dist")`
overload — its `dist/` folder is already a static site bundle ready to deploy.

:::tip
This very documentation site is built with Astro + Starlight and deployed to
GitHub Pages. The exact same `PublishAsNetlifySite("dist")` pattern works to
deploy it to Netlify instead.
:::

## Build & deploy locally

```sh
cd samples/astro
npm ci
npm run build
cd ../..
aspire deploy --apphost samples/Quickstart.AppHost/Quickstart.AppHost.csproj
```

## See also

- [Astro docs](https://docs.astro.build)
- [C# Quickstart](../guides/quickstart-csharp.md)
- [Configuration](../guides/configuration.md)
