---
title: Angular
description: Wire up an Angular CLI app for Netlify deploys via Aspire.Hosting.Netlify.
---

The Angular sample under [`samples/angular`](https://github.com/IEvangelist/netlify-aspire-integration/tree/main/samples/angular)
is a standard Angular CLI app whose `ng build` output (`dist/angular/browser/`)
is deployed as a static site to Netlify.

## AppHost wiring

```csharp
builder.AddJavaScriptApp("angular", "../angular", "start")
    .WithHttpEndpoint(targetPort: 4200, env: "PORT")
    .PublishAsNetlifySite(
        options: new NetlifyDeployOptions
        {
            Dir = "dist/angular/browser",
            NoBuild = true,
            Site = "<your-site-id>"
        },
        authToken: authToken);
```

Angular's browser builder writes to `dist/<projectName>/browser/`, so `Dir` must
include the project name segment. The `"start"` script is what Aspire invokes to
boot the dev server (`ng serve --port $PORT`).

## Build & deploy locally

```sh
cd samples/angular
npm ci
npm run build
cd ../..
aspire deploy --apphost samples/AllFrameworks.AppHost/AllFrameworks.AppHost.csproj
```

## See also

- [Angular CLI docs](https://angular.dev/tools/cli)
- [Configuration](/guides/configuration/)
- [Authentication](/guides/auth/)
