// Copyright (c) David Pine. All rights reserved.
// Licensed under the MIT License.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation that associates an npm script runner resource with a Node.js application resource.
/// </summary>
/// <param name="resource">The npm script runner resource.</param>
public sealed class NpmCommandAnnotation(NpmCommandResource resource) : IResourceAnnotation
{
    /// <summary>
    /// Gets the npm script runner resource associated with the Node.js application.
    /// </summary>
    public NpmCommandResource Resource { get; } = resource;
}