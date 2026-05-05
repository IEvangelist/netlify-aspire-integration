// Copyright (c) David Pine. All rights reserved.
// Licensed under the MIT License.

// Multi-language quickstart: deploy a single Astro site to Netlify from a
// TypeScript AppHost using the [AspireExport]-generated `publishAsNetlifySite`
// surface from `Aspire.Hosting.Netlify`.
//
// Prereq: run `aspire restore` once to generate `.modules/` from
// `aspire.config.json`. The integration's exports surface in
// `.modules/aspire.ts`.
//
// Run with:
//     aspire run --apphost samples/Quickstart.AppHost.TypeScript/apphost.ts
//
// Deploy with:
//     aspire deploy --apphost samples/Quickstart.AppHost.TypeScript/apphost.ts

// @ts-ignore: `.modules/` is generated on demand by `aspire restore` and is
// gitignored. The import resolves once you run `aspire restore` from this
// folder; until then, treat this line as a placeholder.
import { Aspire } from "./.modules/aspire";

const builder = Aspire.createBuilder();

const authToken = builder.addParameterFromConfiguration(
  "netlify-token",
  "NETLIFY_AUTH_TOKEN",
  { secret: true },
);

builder.pipeline.addNetlifyDeployPipeline();

builder
  .addJavaScriptApp("astro", "../astro")
  .withHttpEndpoint({ targetPort: 4321, env: "PORT" })
  .publishAsNetlifySite({ dir: "dist", noBuild: true }, authToken);

builder.build().run();
