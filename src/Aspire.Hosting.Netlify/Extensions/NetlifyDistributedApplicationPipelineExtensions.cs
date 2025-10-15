// Copyright (c) David Pine. All rights reserved.
// Licensed under the MIT License.

namespace Aspire.Hosting;

public static class NetlifyDistributedApplicationPipelineExtensions
{
    /// <summary>
    /// Adds the Netlify deployment pipeline to the distributed application pipeline.
    /// Pairs with <see cref="NodeJSHostingExtensions.PublishAsNetlifySite"/> to deploy Node.js apps to Netlify.
    /// </summary>
    /// <param name="pipeline">The distributed application pipeline.</param>
    /// <returns>The updated distributed application pipeline.</returns>
    public static IDistributedApplicationPipeline AddNetlifyDeployPipeline(
        this IDistributedApplicationPipeline pipeline)
    {
        pipeline.AddStep(
            name: NetlifyDeployStepNames.CheckNetlifyCli,
            action: NetlifyDeploymentPipelineSteps.CheckForNetlifyCliAsync);

        pipeline.AddStep(
            name: NetlifyDeployStepNames.InstallNetlifyCli,
            action: NetlifyDeploymentPipelineSteps.InstallNetlifyCliAsync,
            dependsOn: NetlifyDeployStepNames.CheckNetlifyCli);

        pipeline.AddStep(
            name: NetlifyDeployStepNames.AuthenticateNetlifyCli,
            action: NetlifyDeploymentPipelineSteps.AuthenticateWithNetlifyAsync,
            dependsOn: NetlifyDeployStepNames.InstallNetlifyCli);

        pipeline.AddStep(
            name: NetlifyDeployStepNames.ResolveNetlifySiteId,
            action: NetlifyDeploymentPipelineSteps.ResolveNetlifySiteIdAsync,
            dependsOn: NetlifyDeployStepNames.AuthenticateNetlifyCli);

        pipeline.AddStep(
            name: NetlifyDeployStepNames.DeployToNetlify,
            action : NetlifyDeploymentPipelineSteps.DeployToNetlifyAsync,
            dependsOn: NetlifyDeployStepNames.ResolveNetlifySiteId);

        return pipeline;
    }
}