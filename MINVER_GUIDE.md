# MinVer Quick Reference

This project uses [MinVer](https://github.com/adamralph/minver) for automatic semantic versioning from git tags.

## How It Works

MinVer automatically calculates your package version at build time based on git history:

- **No tags**: Uses `0.1.0-preview.0.0` (configured via `MinVerMinimumMajorMinor`)
- **On a tag**: Uses the tag version (e.g., tag `v1.0.0` → version `1.0.0`)
- **After a tag**: Increments patch and adds pre-release info (e.g., `1.0.1-preview.0.3+abc1234`)

## Tagging for Releases

### Release Versions (Stable)

```bash
# Create a release tag
git tag v1.0.0
git push origin v1.0.0

# This produces NuGet package version: 1.0.0
```

### Pre-Release Versions

```bash
# Create a pre-release tag
git tag v2.0.0-alpha.1
git push origin v2.0.0-alpha.1

# This produces NuGet package version: 2.0.0-alpha.1
```

### Common Pre-Release Labels

- `v1.0.0-alpha.1` - Alpha releases
- `v1.0.0-beta.1` - Beta releases  
- `v1.0.0-rc.1` - Release candidates
- `v1.0.0-preview.1` - Preview releases

## Version Calculation Examples

| Git State | MinVer Output |
|-----------|---------------|
| Tag `v1.0.0` | `1.0.0` |
| Tag `v1.0.0` + 3 commits | `1.0.1-preview.0.3+abc1234` |
| Tag `v1.0.0-beta.1` | `1.0.0-beta.1` |
| Tag `v1.0.0-beta.1` + 2 commits | `1.0.0-beta.1.2+xyz5678` |
| No tags | `0.1.0-preview.0.5+def9012` |

## Local Testing

To see what version MinVer will generate locally:

```bash
# Build and check the version
dotnet build src/Aspire.Hosting.Netlify/Aspire.Hosting.Netlify.csproj

# Or use MinVer CLI
dotnet tool install --global minver-cli
minver
```

## Best Practices

1. **Tag releases in main/master branch only**
   ```bash
   git checkout main
   git pull
   git tag v1.0.0
   git push origin v1.0.0
   ```

2. **Use annotated tags for important releases**
   ```bash
   git tag -a v1.0.0 -m "Release version 1.0.0"
   git push origin v1.0.0
   ```

3. **Follow semantic versioning**
   - **Major** (v2.0.0): Breaking changes
   - **Minor** (v1.1.0): New features, backward compatible
   - **Patch** (v1.0.1): Bug fixes, backward compatible

4. **Create GitHub releases from tags**
   - Tags trigger the publish workflow
   - Releases provide changelog and notes

## Configuration

MinVer is configured in `Aspire.Hosting.Netlify.csproj`:

```xml
<PropertyGroup>
  <!-- Tags must start with 'v' -->
  <MinVerTagPrefix>v</MinVerTagPrefix>
  
  <!-- Default version when no tags exist -->
  <MinVerMinimumMajorMinor>0.1</MinVerMinimumMajorMinor>
  
  <!-- Default pre-release label -->
  <MinVerDefaultPreReleaseIdentifiers>preview.0</MinVerDefaultPreReleaseIdentifiers>
</PropertyGroup>
```

## Troubleshooting

### Version shows as 0.0.0

- Ensure git history is available (`fetch-depth: 0` in CI)
- Check that tags are pushed to the repository

### Version includes unexpected commit count

- This is normal for commits after a tag
- Create a new tag to publish a clean version

### Tags not recognized

- Verify tag prefix matches (`v` is required)
- Ensure tags follow format `v{major}.{minor}.{patch}`

## Additional Resources

- [MinVer GitHub Repository](https://github.com/adamralph/minver)
- [Semantic Versioning Specification](https://semver.org/)
- [Git Tagging Basics](https://git-scm.com/book/en/v2/Git-Basics-Tagging)
