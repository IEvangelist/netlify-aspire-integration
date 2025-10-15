// Copyright (c) David Pine. All rights reserved.
// Licensed under the MIT License.

namespace Aspire.Hosting;

/// <summary>
/// Represents an annotation for a Netlify deployment resource.
/// </summary>
public sealed class NetlifyDeploymentAnnotation(NetlifyDeploymentResource deployerResource) : IResourceAnnotation
{
    /// <summary>
    /// The instance of the Netlify deployer resource used.
    /// </summary>
    public NetlifyDeploymentResource Resource { get; } = deployerResource;
}