// Copyright (c) David Pine. All rights reserved.
// Licensed under the MIT License.

// 30-second quickstart: deploy a single Astro site to Netlify using the
// directory-only convenience overload of PublishAsNetlifySite.
//
// Run with:
//     aspire run --apphost samples/Quickstart.AppHost/Quickstart.AppHost.csproj
//
// Deploy with:
//     aspire deploy --apphost samples/Quickstart.AppHost/Quickstart.AppHost.csproj
var builder = DistributedApplication.CreateBuilder(args);

// Optional: pass an auth token explicitly via configuration. When omitted the pipeline
// falls back to NETLIFY_AUTH_TOKEN from the environment, then to interactive `ntl login`.
var authToken = builder.AddParameterFromConfiguration(
    "netlify-token", "NETLIFY_AUTH_TOKEN", secret: true);

builder.Pipeline.AddNetlifyDeployPipeline();

builder.AddJavaScriptApp("astro", "../astro")
    .WithHttpEndpoint(targetPort: 4321, env: "PORT")
    .PublishAsNetlifySite("dist", authToken);

builder.Build().Run();
