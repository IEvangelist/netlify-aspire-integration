# NuGet Package Publishing Setup

This document explains how to set up and use the automated NuGet package publishing workflow.

## Prerequisites

1. **NuGet.org Account**: You need an account on [NuGet.org](https://www.nuget.org)
2. **API Key**: Create an API key from your NuGet.org account settings

## Setup Instructions

### 1. Create a NuGet API Key

1. Go to [NuGet.org](https://www.nuget.org) and sign in
2. Navigate to your account settings → API Keys
3. Click "Create" to generate a new API key
4. Configure the key:
   - **Key Name**: `GitHub Actions - netlify-aspire-integration`
   - **Select Scopes**: Check "Push new packages and package versions"
   - **Select Packages**: Choose "All packages" or select specific ones
   - **Glob Pattern**: `Aspire.Hosting.Netlify*` (or leave as `*`)
   - **Expiration**: Set an appropriate expiration date (recommended: 1 year)
5. Copy the generated API key (you won't be able to see it again!)

### 2. Add the Secret to GitHub

1. Go to your GitHub repository: `https://github.com/IEvangelist/netlify-aspire-integration`
2. Navigate to **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Add the secret:
   - **Name**: `NUGET_API_KEY`
   - **Value**: Paste the API key you copied from NuGet.org
5. Click **Add secret**

### 3. Create a Release to Trigger Publishing

The workflow automatically triggers when you create a new GitHub release:

1. Go to your repository on GitHub
2. Click on **Releases** → **Draft a new release**
3. Create a new tag following semantic versioning:
   - Tag format: `v1.0.0`, `v1.0.1`, `v2.0.0-preview.1`, etc.
   - The workflow will strip the 'v' prefix automatically
4. Fill in the release details:
   - **Release title**: e.g., "v1.0.0 - Initial Release"
   - **Description**: Add release notes
5. Click **Publish release**

The workflow will automatically:
- Extract the version from the tag
- Build the project with that version
- Run all tests (must pass for green build)
- Pack the NuGet package
- Push to NuGet.org

### 4. Manual Publishing (Optional)

You can also manually trigger the workflow:

1. Go to **Actions** → **Publish NuGet Package**
2. Click **Run workflow**
3. Click **Run workflow** (version determined automatically by MinVer from git tags)

## Version Format with MinVer

The project uses **MinVer** for automatic version calculation from git tags. MinVer follows semantic versioning:
- **Release versions**: `v1.0.0`, `v2.1.3`, etc.
- **Pre-release versions**: Calculated automatically from commits since last tag
- **Tag prefix**: Must start with `v` (e.g., `v1.0.0`)

### How MinVer Works

MinVer calculates the package version based on:
1. **Latest git tag**: Finds the most recent tag matching `v*.*.*`
2. **Commit height**: Counts commits since the tag
3. **Branch/commit info**: Adds metadata for pre-release versions

### Tag Examples

```bash
# Release versions (published as-is)
git tag v1.0.0
git tag v1.0.1
git tag v2.0.0

# MinVer will automatically add pre-release info for commits without tags
# Example: If you have 3 commits after v1.0.0, MinVer generates: 1.0.1-preview.0.3+abc1234

# For explicit pre-release tags
git tag v2.0.0-alpha.1
git tag v2.0.0-beta.1
git tag v2.0.0-rc.1

# Push tags
git push origin v1.0.0
```

### MinVer Configuration

The project is configured with:
- **MinVerTagPrefix**: `v` (tags must start with 'v')
- **MinVerMinimumMajorMinor**: `0.1` (default version if no tags exist)
- **MinVerDefaultPreReleaseIdentifiers**: `preview.0` (default pre-release label)

## Package Information

The published package will include:
- **Package ID**: `Aspire.Hosting.Netlify`
- **Title**: Aspire Hosting Integration for Netlify
- **Authors**: IEvangelist
- **License**: MIT
- **Repository**: https://github.com/IEvangelist/netlify-aspire-integration
- **Symbols**: Debug symbols included (.snupkg)
- **README**: Repository README.md included in package

## Workflow Features

✅ **Clean Build Requirement**: Tests must pass before publishing
✅ **Automatic Versioning**: MinVer calculates version from git tags
✅ **Artifact Upload**: Package artifacts saved for 90 days
✅ **Test Results**: Test results uploaded and preserved
✅ **Skip Duplicates**: Won't fail if version already exists
✅ **Source Link**: Debug symbols linked to source code
✅ **Manual Trigger**: Can be manually run (version auto-determined)
✅ **.NET 10 Preview**: Uses the latest .NET 10 preview SDK

## Troubleshooting

### Workflow fails with "Context access might be invalid"

This is a linting warning and can be ignored. The workflow will run successfully as long as the `NUGET_API_KEY` secret is properly configured.

### Tests fail

The workflow requires a clean/green build. If tests fail:
1. Check the test results in the workflow output
2. Fix the failing tests locally
3. Create a new release with the corrected code

### Package not appearing on NuGet.org

- It can take a few minutes for packages to be indexed
- Check the workflow logs for any errors
- Verify the API key has the correct permissions
- Ensure the package version doesn't already exist

### API Key expired

If your API key expires:
1. Create a new API key on NuGet.org
2. Update the `NUGET_API_KEY` secret in GitHub

## Next Steps

After your first successful publish:
1. Monitor the package on NuGet.org: `https://www.nuget.org/packages/Aspire.Hosting.Netlify`
2. Consider adding a NuGet badge to your README
3. Set up release notes automation
4. Configure package validation and signing (optional)

## Example Release Workflow

```bash
# 1. Ensure all changes are committed and pushed
git add .
git commit -m "Prepare for v1.0.0 release"
git push origin main

# 2. Create and push a tag
git tag v1.0.0
git push origin v1.0.0

# 3. Create a GitHub release (via UI or CLI)
gh release create v1.0.0 --title "v1.0.0 - Initial Release" --notes "First stable release"

# 4. The workflow will automatically trigger and publish to NuGet.org
```

## Additional Resources

- [NuGet.org Documentation](https://docs.microsoft.com/nuget/)
- [GitHub Actions Documentation](https://docs.github.com/actions)
- [Semantic Versioning](https://semver.org/)
- [SourceLink Documentation](https://github.com/dotnet/sourcelink)
