using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a Netlify site deployer.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="workingDirectory">The working directory to use for the deployment.</param>
/// <param name="buildDirectory">The directory containing the built static files to deploy.</param>
/// <param name="siteName">Optional site name for the Netlify site.</param>
/// <param name="deploymentEnvironment">The deployment environment (prod, staging, preview). Defaults to "prod".</param>
public class NetlifyDeployerResource(string name, string workingDirectory, string buildDirectory, string? siteName = null, string deploymentEnvironment = "prod")
    : ExecutableResource(name, "netlify", workingDirectory)
{
    /// <summary>
    /// Gets the directory containing the built static files to deploy.
    /// </summary>
    public string BuildDirectory { get; } = buildDirectory;

    /// <summary>
    /// Gets the optional site name for the Netlify site.
    /// </summary>
    public string? SiteName { get; } = siteName;

    /// <summary>
    /// Gets the deployment environment (prod, staging, preview, etc.).
    /// </summary>
    public string DeploymentEnvironment { get; } = deploymentEnvironment;
}