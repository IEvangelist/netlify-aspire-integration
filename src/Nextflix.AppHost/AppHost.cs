var builder = DistributedApplication.CreateBuilder(args);

// Complete workflow:
// - Run    →   npm run dev
// - Deploy →   netlify deploy
   builder.AddNpmApp("sample", "../sample-site", "dev")
       .WithNpmPackageInstallation()
       .WithHttpEndpoint(targetPort: 4321)
       .PublishAsNetlifySite(new NetlifyDeployOptions() { Dir = "dist" });

builder.AddNpmApp("sample-2", "../sample-site", "dev")
       .WithNpmPackageInstallation()
       .WithNpmRunCommand("build:prod")
       .PublishAsNetlifySite(new NetlifyDeployOptions()
       {
           Dir = "dist",
           NoBuild = true, // Skip the build step since we already built it above.
       });

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