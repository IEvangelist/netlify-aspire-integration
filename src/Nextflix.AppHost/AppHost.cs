var builder = DistributedApplication.CreateBuilder(args);

// Astro app - Static Site Generator
builder.AddNpmApp("astro", "../astro", "dev")
    .WithNpmPackageInstallation()
    .WithHttpEndpoint(targetPort: 4321, env: "PORT")
    .PublishAsNetlifySite(new NetlifyDeployOptions() { Dir = "dist" });

// React app - Vite + TypeScript
builder.AddNpmApp("react", "../react", "dev")
    .WithNpmPackageInstallation()
    .WithHttpEndpoint(targetPort: 5173, env: "PORT")
    .PublishAsNetlifySite(new NetlifyDeployOptions() { Dir = "dist" });

// Vue app - Vite
builder.AddNpmApp("vue", "../vue", "dev")
    .WithNpmPackageInstallation()
    .WithHttpEndpoint(targetPort: 5174, env: "PORT")
    .PublishAsNetlifySite(new NetlifyDeployOptions() { Dir = "dist" });

// Svelte app - Vite + TypeScript
builder.AddNpmApp("svelte", "../svelte", "dev")
    .WithNpmPackageInstallation()
    .WithHttpEndpoint(targetPort: 5175, env: "PORT")
    .PublishAsNetlifySite(new NetlifyDeployOptions() { Dir = "dist" });

// Angular app - Angular CLI
builder.AddNpmApp("angular", "../angular", "start")
    .WithNpmPackageInstallation()
    .WithHttpEndpoint(targetPort: 4200, env: "PORT")
    .PublishAsNetlifySite(new NetlifyDeployOptions() { Dir = "dist/angular/browser" });

// Next.js app - Static Export (requires next.config.js with output: 'export')
builder.AddNpmApp("next", "../next", "dev")
    .WithNpmPackageInstallation()
    .WithHttpEndpoint(targetPort: 3000, env: "PORT")
    .PublishAsNetlifySite(new NetlifyDeployOptions() { Dir = "out" });

builder.Build().Run();

/*
Scenarios to try:

1. Run without netlify CLI installed.
   - Should get a prompt to install it?
   - Currently just installs it...

2. Run with netlify CLI installed but not logged in.
    - Should get a prompt to log in?
    - Currently just calls ntl login,
      if there's a authToken provided, or an options.Auth value, it uses that.
      Need to also consider the `NETLIFY_AUTH_TOKEN` env var.

3. Run with netlify CLI installed and logged in.
   - Should proceed with the deployment prompting only for the site/project ID, if not otherwise provided.
*/