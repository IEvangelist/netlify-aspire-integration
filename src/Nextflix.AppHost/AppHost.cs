var builder = DistributedApplication.CreateBuilder(args);

// Complete workflow:
// - Run    →   npm run dev
// - Deploy →   netlify deploy
builder.AddNpmApp("sample", "../sample-site", "dev")
       .WithHttpEndpoint(targetPort: 4321)
       .PublishAsNetlifySite(new NetlifyDeployOptions() { Dir = "dist" });

builder.Build().Run();
