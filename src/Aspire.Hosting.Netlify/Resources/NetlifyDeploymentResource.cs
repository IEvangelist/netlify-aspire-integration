// Copyright (c) David Pine. All rights reserved.
// Licensed under the MIT License.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a Netlify site deployer.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="javaScriptAppResource">The JavaScript application resource to deploy.</param>
/// <param name="deployOptions">The deployment options.</param>
/// <param name="authToken">An optional parameter resource containing the Netlify authentication token.</param>
public class NetlifyDeploymentResource(
    string name,
    JavaScriptAppResource javaScriptAppResource,
    NetlifyDeployOptions deployOptions,
    IResourceBuilder<ParameterResource>? authToken = null) : IComputeEnvironmentResource
{
    /// <summary>
    /// Gets the name of the resource.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the name of the associated JavaScript application resource.
    /// </summary>
    internal string JavaScriptAppResourceName => JavaScriptAppResource.Name;

    /// <summary>
    /// Gets the associated JavaScript application resource.
    /// </summary>
    internal JavaScriptAppResource JavaScriptAppResource { get; } = javaScriptAppResource;

    /// <summary>
    /// Gets the annotations associated with the resource.
    /// </summary>
    public ResourceAnnotationCollection Annotations { get; } = [];

    /// <summary>
    /// Gets the working directory of the JavaScript application resource.
    /// </summary>
    public string WorkingDirectory => JavaScriptAppResource.WorkingDirectory;

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