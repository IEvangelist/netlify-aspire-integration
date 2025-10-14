var builder = DistributedApplication.CreateBuilder(args);

var authToken = builder.AddParameterFromConfiguration(
   "netlify-token", "NETLIFY_AUTH_TOKEN", secret: true);

builder.Pipeline.AddNetlifyDeployPipeline();

// Astro app - Static Site Generator
builder.AddNpmApp("astro", "../astro", "dev")
    .WithNpmPackageInstallation()
    .WithHttpEndpoint(targetPort: 4321, env: "PORT")
    .PublishAsNetlifySite(
        options: new NetlifyDeployOptions() { Dir = "dist" },
        authToken: authToken);

// React app - Vite + TypeScript
builder.AddNpmApp("react", "../react", "dev")
    .WithNpmPackageInstallation()
    .WithNpmRunCommand("build")
    .WithHttpEndpoint(targetPort: 5173, env: "PORT")
    .PublishAsNetlifySite(
        options: new NetlifyDeployOptions() { Dir = "dist", CreateSite = "react", NoBuild = true },
        authToken: authToken);

// Vue app - Vite
builder.AddNpmApp("vue", "../vue", "dev")
    .WithNpmPackageInstallation()
    .WithHttpEndpoint(targetPort: 5174, env: "PORT")
    .PublishAsNetlifySite(
        options: new NetlifyDeployOptions() { Dir = "dist", CreateSite = "vue" },
        authToken: authToken);

// Svelte app - Vite + TypeScript
builder.AddNpmApp("svelte", "../svelte", "dev")
    .WithNpmPackageInstallation()
    .WithHttpEndpoint(targetPort: 5175, env: "PORT")
    .PublishAsNetlifySite(
        options: new NetlifyDeployOptions() { Dir = "dist", CreateSite = "svelte" },
        authToken: authToken);

// Angular app - Angular CLI
builder.AddNpmApp("angular", "../angular", "start")
    .WithNpmPackageInstallation()
    .WithHttpEndpoint(targetPort: 4200, env: "PORT")
    .PublishAsNetlifySite(
        options: new NetlifyDeployOptions() { Dir = "dist/angular/browser", CreateSite = "angular" },
        authToken: authToken);

// Next.js app - Static Export (requires next.config.js with output: 'export')
builder.AddNpmApp("next", "../next", "dev")
    .WithNpmPackageInstallation()
    .WithHttpEndpoint(targetPort: 3000, env: "PORT")
    .PublishAsNetlifySite(
        options: new NetlifyDeployOptions() { Dir = "out", CreateSite = "next" },
        authToken: authToken);

builder.Build().Run();