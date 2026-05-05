---
title: Vue
description: Wire up a Vite + Vue SPA for Netlify deploys via Aspire.Hosting.Netlify.
---

The Vue sample under [`samples/vue`](https://github.com/IEvangelist/netlify-aspire-integration/tree/main/samples/vue)
is a Vite + Vue 3 SPA. `npm run build` writes a static bundle to `dist/`.

## AppHost wiring

```csharp
builder.AddJavaScriptApp("vue", "../vue")
    .WithHttpEndpoint(targetPort: 5174, env: "PORT")
    .PublishAsNetlifySite(
        options: new NetlifyDeployOptions
        {
            Dir = "dist",
            NoBuild = true,
            Site = "<your-site-id>"
        },
        authToken: authToken);
```

Vue Router users should drop a Netlify `_redirects` file containing
`/*  /index.html  200` into `samples/vue/public/` so client-side routes survive
hard refreshes.

## Build & deploy locally

```sh
cd samples/vue
npm ci
npm run build
cd ../..
aspire deploy --apphost samples/AllFrameworks.AppHost/AllFrameworks.AppHost.csproj
```

## See also

- [Vue docs](https://vuejs.org)
- [Configuration](../guides/configuration.md)
