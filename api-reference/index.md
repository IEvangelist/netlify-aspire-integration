# API Reference

The complete public surface of `Aspire.Hosting.Netlify`, generated from the
project's XML doc comments.

> Looking for guides, quickstarts, or framework walkthroughs? Head back to the
> <a href="../">main docs site</a>.

## At a glance

| Type | Kind | Description |
| --- | --- | --- |
| [`JavaScriptHostingExtensions`](xref:Aspire.Hosting.JavaScriptHostingExtensions) | Static class | `PublishAsNetlifySite`, `WithNpmCommand`, `WithNpmRunCommand` extension methods on `IResourceBuilder<JavaScriptAppResource>`. |
| [`NetlifyDistributedApplicationPipelineExtensions`](xref:Aspire.Hosting.NetlifyDistributedApplicationPipelineExtensions) | Static class | `AddNetlifyDeployPipeline()` extension that registers the multi-step deploy pipeline. |
| [`NetlifyDeployOptions`](xref:Aspire.Hosting.NetlifyDeployOptions) | Class | All options for a single Netlify deploy: `Dir`, `Site`, `Message`, `Production`, `NoBuild`, etc. |
| [`NetlifyDeploymentResource`](xref:Aspire.Hosting.ApplicationModel.NetlifyDeploymentResource) | Class | The Aspire resource that represents a Netlify deploy in the application model. |
| [`NetlifyDeploymentAnnotation`](xref:Aspire.Hosting.NetlifyDeploymentAnnotation) | Class | Annotation linking a `JavaScriptAppResource` to its `NetlifyDeploymentResource`. |
| [`NpmCommandResource`](xref:Aspire.Hosting.ApplicationModel.NpmCommandResource) | Class | Resource that wraps an `npm`/`npm run` invocation associated with a JavaScript app. |
| [`NpmCommandAnnotation`](xref:Aspire.Hosting.ApplicationModel.NpmCommandAnnotation) | Class | Annotation linking a `JavaScriptAppResource` to its `NpmCommandResource`. |
| [`NetlifySite`](xref:Aspire.Hosting.NetlifySite) | Record | Snapshot of a Netlify site as returned by the Netlify CLI. |
| [`NetlifySiteCapabilities`](xref:Aspire.Hosting.NetlifySiteCapabilities) | Record | Per-account capability flags returned alongside `NetlifySite`. |

## Most-used methods

```csharp
// One-line directory deploy.
builder.AddJavaScriptApp("astro", "../astro")
       .PublishAsNetlifySite("dist");

// Full options form.
builder.AddJavaScriptApp("react", "../react")
       .WithHttpEndpoint(targetPort: 5173, env: "PORT")
       .PublishAsNetlifySite(
           options: new NetlifyDeployOptions
           {
               Dir = "dist",
               Site = "<site-id>",
               Production = true,
           },
           authToken: authToken);

// Wire the deploy pipeline.
builder.Pipeline.AddNetlifyDeployPipeline();
```

## TypeScript bindings

This integration is annotated with `[AspireExport]` / `[AspireDto]`, so
TypeScript AppHosts get matching bindings under `.modules/aspire.ts`:

| C# member | TypeScript binding |
| --- | --- |
| `PublishAsNetlifySite(string dir, ...)` | `publishAsNetlifySite({ dir, noBuild: true }, authToken?)` |
| `AddNetlifyDeployPipeline()` | `pipeline.addNetlifyDeployPipeline()` |
| `WithNpmCommand(...)` | `withNpmCommand(...)` |
| `WithNpmRunCommand(...)` | `withNpmRunCommand(...)` |

See the <a href="../guides/multi-language/">Multi-language guide</a> for the full story.

## Browse the namespace

Use the navigation on the left to browse every type, member, and overload, or
jump straight to:

- [`Aspire.Hosting`](xref:Aspire.Hosting) — extension methods, options, and core types.
- [`Aspire.Hosting.ApplicationModel`](xref:Aspire.Hosting.ApplicationModel) —
  resources and annotations attached by the integration.

