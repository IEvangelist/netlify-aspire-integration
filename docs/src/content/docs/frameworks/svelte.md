---
title: Svelte
description: Wire up a Vite + Svelte SPA for Netlify deploys via Aspire.Hosting.Netlify.
---

The Svelte sample under [`samples/svelte`](https://github.com/IEvangelist/netlify-aspire-integration/tree/main/samples/svelte)
is a Vite + Svelte + TypeScript SPA. `npm run build` writes a static bundle to
`dist/`.

## AppHost wiring

```csharp
builder.AddJavaScriptApp("svelte", "../svelte")
    .WithHttpEndpoint(targetPort: 5175, env: "PORT")
    .PublishAsNetlifySite(
        options: new NetlifyDeployOptions
        {
            Dir = "dist",
            NoBuild = true,
            Site = "<your-site-id>"
        },
        authToken: authToken);
```

If you migrate to SvelteKit you'll want
[`@sveltejs/adapter-static`](https://kit.svelte.dev/docs/adapter-static) for the
same flat-directory deploy story; switch `Dir` to `build` to match the adapter's
output.

## Build & deploy locally

```sh
cd samples/svelte
npm ci
npm run build
cd ../..
aspire deploy --apphost samples/AllFrameworks.AppHost/AllFrameworks.AppHost.csproj
```

## See also

- [Svelte docs](https://svelte.dev)
- [Configuration](/guides/configuration/)
