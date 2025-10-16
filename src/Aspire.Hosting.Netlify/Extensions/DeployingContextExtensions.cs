// Copyright (c) David Pine. All rights reserved.
// Licensed under the MIT License.

namespace Aspire.Hosting;

internal static class DeployingContextExtensions
{
    public static IEnumerable<NetlifyDeploymentResource> GetNetlifyDeploymentResources(
        this DeployingContext context)
    {
        return context.Model.Resources.OfType<NetlifyDeploymentResource>();
    }
}