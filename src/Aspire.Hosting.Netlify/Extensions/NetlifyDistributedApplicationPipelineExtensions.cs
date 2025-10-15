namespace Aspire.Hosting;

public static class NetlifyDistributedApplicationPipelineExtensions
{
    /// <summary>
    /// Adds the Netlify deployment pipeline to the distributed application pipeline.
    /// </summary>
    /// <param name="pipeline">The distributed application pipeline.</param>
    /// <returns>The updated distributed application pipeline.</returns>
    public static IDistributedApplicationPipeline AddNetlifyDeployPipeline(
        this IDistributedApplicationPipeline pipeline)
    {
        pipeline.AddStep(
            name: "netlify-deploy",
            action: DeployNetlifySitesAsync);

        return pipeline;
    }

    private static async Task DeployNetlifySitesAsync(DeployingContext context)
    {
        List<NetlifyDeploymentResource> netlifyDeployments =
        [
            .. context.Model.Resources.OfType<NetlifyDeploymentResource>()
        ];

        if (netlifyDeployments.Count is 0)
        {
            return;
        }

        var deployManager = new NetlifyDeploymentManager([.. netlifyDeployments]);

        await deployManager.PerformDeployAsync(context);
    }
}