// Copyright (c) David Pine. All rights reserved.
// Licensed under the MIT License.

var builder = DistributedApplication.CreateBuilder(args);

var authToken = builder.AddParameterFromConfiguration(
   "netlify-token", "NETLIFY_AUTH_TOKEN", secret: true);

builder.Pipeline.AddNetlifyDeployPipeline();

// Astro app - Static Site Generator
builder.AddNpmApp("astro", "../astro", "dev")
    .WithNpmPackageInstallation()
    .WithHttpEndpoint(targetPort: 4321, env: "PORT")
    .PublishAsNetlifySite(
        options: new NetlifyDeployOptions()
        {
            Dir = "dist",
            NoBuild = true,
            Site = "eace4cd9-35ba-4076-9129-5b54418b7512"
        },
        authToken: authToken);

// React app - Vite + TypeScript
builder.AddNpmApp("react", "../react", "dev")
    .WithNpmPackageInstallation()
    .WithHttpEndpoint(targetPort: 5173, env: "PORT")
    .PublishAsNetlifySite(
        options: new NetlifyDeployOptions()
        {
            Dir = "dist",
            NoBuild = true,
            Site = "e936ef36-d1fa-46b2-98d5-8e61b4459330"
        },
        authToken: authToken);

// Vue app - Vite
builder.AddNpmApp("vue", "../vue", "dev")
    .WithNpmPackageInstallation()
    .WithHttpEndpoint(targetPort: 5174, env: "PORT")
    .PublishAsNetlifySite(
        options: new NetlifyDeployOptions()
        {
            Dir = "dist",
            NoBuild = true,
            Site = "a8f5b603-a496-499a-b575-f8786f9456a5"
        },
        authToken: authToken);

// Angular app - Angular CLI
builder.AddNpmApp("angular", "../angular", "start")
    .WithNpmPackageInstallation()
    .WithHttpEndpoint(targetPort: 4200, env: "PORT")
    .PublishAsNetlifySite(
        options: new NetlifyDeployOptions()
        {
            Dir = "dist/angular/browser",
            NoBuild = true,
            Site = "73d17585-2fe2-4bda-84d8-ad503cec7cab"
        },
        authToken: authToken);

// Svelte app - Vite + TypeScript
builder.AddNpmApp("svelte", "../svelte", "dev")
    .WithNpmCommand("i")  // npm i
    .WithNpmRunCommand("build")
    .WithNpmPackageInstallation()
    .WithHttpEndpoint(targetPort: 5175, env: "PORT")
    .PublishAsNetlifySite(
        options: new NetlifyDeployOptions()
        {
            Dir = "dist",
            NoBuild = true,
            Site = "de97e199-3b64-4824-b838-81ee33e13976"
        },
        authToken: authToken);

// Next.js app - Static Export (requires next.config.js with output: 'export')
builder.AddNpmApp("next", "../next", "dev")
    .WithNpmPackageInstallation()
    .WithHttpEndpoint(targetPort: 3000, env: "PORT")
    .PublishAsNetlifySite(
        options: new NetlifyDeployOptions()
        {
            Dir = "out",
            NoBuild = true,
            Site = "79a9e3b6-337e-4008-88c4-883bc53c90bc"
        },
        authToken: authToken);

builder.Build().Run();