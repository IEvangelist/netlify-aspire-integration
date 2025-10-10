using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Represents an annotation for a Netlify deployment resource.
/// </summary>
public sealed class NetlifyDeploymentAnnotation(NetlifyDeployerResource deployerResource) : IResourceAnnotation
{
    /// <summary>
    /// The instance of the Netlify deployer resource used.
    /// </summary>
    public NetlifyDeployerResource Resource { get; } = deployerResource;
}