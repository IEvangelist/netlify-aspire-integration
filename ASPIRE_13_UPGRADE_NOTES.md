# Aspire 13.0 Upgrade Notes

## Summary

The `Aspire.Hosting.Netlify` library has been successfully upgraded to **Aspire 13.0.2** (stable release).

## Completed Changes

✅ **Core Library (`Aspire.Hosting.Netlify`)** - Successfully upgraded and builds without errors:
- Updated from Aspire preview packages to stable 13.0.2
- Replaced `Aspire.Hosting.NodeJS` with `Aspire.Hosting.JavaScript`
- Updated `IDeploymentStateManager` API calls:
  - `LoadStateAsync/SaveStateAsync` → `AcquireSectionAsync/SaveSectionAsync`
  - Updated to use `DeploymentStateSection.Data` property
  - Adjusted to pass entire section object to `SaveSectionAsync`
- Added `ASPIREPIPELINES002` to suppressed warnings (experimental API)
- Added `Aspire.Hosting.JavaScript` namespace to GlobalUsings

## Breaking Changes in Aspire 13

### AddNpmApp Removal

**Impact**: The `AddNpmApp` extension method has been completely removed in Aspire 13.0.

**Affected Components**:
- Sample AppHost (`src/Netlify.AppHost`)
- Unit tests (`src/Aspire.Hosting.Netlify.Tests`)

**Migration Path**: 
The old `AddNpmApp(name, path, scriptName)` needs to be replaced with one of the new Aspire 13 APIs:

1. **For npm-based apps (React, Vue, Angular, Astro, etc.)**:
   ```csharp
   // Old (Aspire 9/preview):
   builder.AddNpmApp("app", "../app", "dev")
   
   // New (Aspire 13):
   builder.AddJavaScriptApp("app", "../app", "dev")
   ```
   
   However, `AddJavaScriptApp` returns `JavaScriptAppResource` instead of `NodeAppResource`, which requires updating the Netlify extension methods to accept both types.

2. **For Node.js server apps**:
   ```csharp
   builder.AddNodeApp("name", "workingDirectory", "scriptPath")
   ```
   
   But this requires a `scriptPath` parameter (path to the .js file), not an npm script name.

### Recommended Next Steps

1. **Update Netlify Extension Methods**: Modify the extension methods in `NodeJSHostingExtensions.cs` to work with both `NodeAppResource` and `JavaScriptAppResource`, or create overloads for `JavaScriptAppResource`.

2. **Update Sample AppHost**: Replace all `AddNpmApp` calls with `AddJavaScriptApp` and update extension method calls accordingly.

3. **Update Tests**: Replace `AddNpmApp` with appropriate new APIs and update assertions.

4. **Update CommunityToolkit References**: The `WithNpmPackageInstallation` extension may need to be replaced with native Aspire 13 package installation methods.

## Package Changes

### Updated Packages:
- `Aspire.Hosting`: `13.0.0-preview.1.25520.1` → `13.0.2`
- `Aspire.Hosting.AppHost`: `13.0.0-preview.1.25520.1` → `13.0.2`  
- `Aspire.Hosting.NodeJs`: (removed) → `Aspire.Hosting.JavaScript` `13.0.2`
- `Aspire.Hosting.Testing`: `13.0.0-preview.1.25513.8` → `13.0.2`
- `CommunityToolkit.Aspire.Hosting.NodeJS.Extensions`: `9.8.0` → `CommunityToolkit.Aspire.Hosting.JavaScript.Extensions` `13.0.0`

### NuGet.config:
- Removed preview package source (`dotnet9` feed)
- Using only stable NuGet.org feed

##Status

- ✅ **Core Library**: Builds successfully with Aspire 13.0.2
- ⚠️ **Sample AppHost**: Requires API migration (AddNpmApp → AddJavaScriptApp)
- ⚠️ **Tests**: Require API migration (AddNpmApp → AddJavaScriptApp)

## Additional Resources

- [Aspire 13.0 What's New](https://aspire.dev/whats-new/aspire-13/)
- [Aspire 13.0 Breaking Changes](https://learn.microsoft.com/en-us/dotnet/aspire/compatibility/13.0/)
- [JavaScript Hosting Extensions API](https://learn.microsoft.com/en-us/dotnet/api/aspire.hosting.javascripthostingextensions?view=dotnet-aspire-13.0)
