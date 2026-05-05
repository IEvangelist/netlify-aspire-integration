// Copyright (c) David Pine. All rights reserved.
// Licensed under the MIT License.

namespace Aspire.Hosting;

internal static class PipelineStepContextExtensions
{
    public static IEnumerable<NetlifyDeploymentResource> GetNetlifyDeploymentResources(
        this PipelineStepContext context)
    {
        return context.Model.Resources.OfType<NetlifyDeploymentResource>();
    }
}