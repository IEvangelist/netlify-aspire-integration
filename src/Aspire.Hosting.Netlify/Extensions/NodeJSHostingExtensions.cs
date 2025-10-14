namespace Aspire.Hosting;

public static partial class NodeJSHostingExtensions
{
    /// <summary>
    /// Configures the Node.js app to run an npm script before starting.
    /// </summary>
    /// <param name="builder">The Node.js app resource builder.</param>
    /// <param name="scriptName">The npm script name to run (e.g., "build", "test", "lint").</param>
    /// <param name="configureRunner">Configure the npm runner resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<NodeAppResource> WithNpmRunCommand(
        this IResourceBuilder<NodeAppResource> builder,
        string scriptName,
        Action<IResourceBuilder<NpmRunnerResource>>? configureRunner = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(scriptName);

        if (!builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            var runnerName = $"{builder.Resource.Name}-npm-run-{scriptName}";
            var runner = new NpmRunnerResource(runnerName, builder.Resource.WorkingDirectory, scriptName);

            var runnerBuilder = builder.ApplicationBuilder.AddResource(runner)
                .WithArgs(["run", scriptName])
                .WithParentRelationship(builder.Resource)
                .ExcludeFromManifest();

            // Make the parent resource wait for the runner to complete
            builder.WaitForCompletion(runnerBuilder);

            configureRunner?.Invoke(runnerBuilder);

            builder.WithAnnotation(new NpmRunnerAnnotation(runner));
        }

        return builder;
    }

    /// <summary>
    /// Configures the Node.js app to be deployed to Netlify using the Netlify CLI with custom deployment options.
    /// </summary>
    /// <param name="builder">The Node.js app resource builder.</param>
    /// <param name="options">The Netlify deployment options.</param>
    /// <param name="authToken">An optional parameter resource containing the Netlify authentication token.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the Netlify CLI is not installed or not found in PATH.</exception>
    public static IResourceBuilder<NodeAppResource> PublishAsNetlifySite(
        this IResourceBuilder<NodeAppResource> builder,
        NetlifyDeployOptions options,
        IResourceBuilder<ParameterResource>? authToken = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);

        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            var deployerName = $"{builder.Resource.Name}-netlify-deploy";

            var deployment = new NetlifyDeploymentResource(
                deployerName,
                builder.Resource,
                options,
                authToken);

            _ = builder.ApplicationBuilder.AddResource(deployment)
                .WithParentRelationship(builder.Resource)
                .ExcludeFromManifest();

            // builder.WithAnnotation(new PipelineStepAnnotation(
            //     () => new NetlifyDeployPipelineStepFactory(deployment).CreatePipelineSteps()
            // ));

            builder.WithAnnotation(new NetlifyDeploymentAnnotation(deployment));
        }

        return builder;
    }
}