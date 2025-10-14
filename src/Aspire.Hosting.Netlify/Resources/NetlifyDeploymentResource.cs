namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a Netlify site deployer.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="nodeAppResource">The Node.js application resource to deploy.</param>
/// <param name="deployOptions">The deployment options.</param>
/// <param name="authToken">An optional parameter resource containing the Netlify authentication token.</param>
public class NetlifyDeploymentResource(
    string name,
    NodeAppResource nodeAppResource,
    NetlifyDeployOptions deployOptions,
    IResourceBuilder<ParameterResource>? authToken = null) : IComputeEnvironmentResource
{
    private readonly NodeAppResource _nodeAppResource = nodeAppResource;

    /// <summary>
    /// Gets the name of the resource.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the annotations associated with the resource.
    /// </summary>
    public ResourceAnnotationCollection Annotations { get; } = [];

    /// <summary>
    /// Gets the working directory of the Node.js application resource.
    /// </summary>
    public string WorkingDirectory => _nodeAppResource.WorkingDirectory;

    /// <summary>
    /// Gets the deployment options.
    /// </summary>
    public NetlifyDeployOptions Options { get; } = deployOptions;

    /// <summary>
    /// Gets the directory containing the built static files to deploy.
    /// </summary>
    public string BuildDirectory => Options.Dir ?? "dist";

    /// <summary>
    /// Gets the optional site name for the Netlify site.
    /// </summary>
    public string? SiteName => Options.Site;

    /// <summary>
    /// Gets the deployment environment (prod or draft preview).
    /// </summary>
    public string DeploymentEnvironment => Options.Prod is true ? "prod" : "preview";

    /// <summary>
    /// Gets the optional parameter resource containing the Netlify authentication token.
    /// When provided, this token is used in place of the interactive <c>netlify login</c> command.
    /// </summary>
    public IResourceBuilder<ParameterResource>? AuthToken { get; } = authToken;
}