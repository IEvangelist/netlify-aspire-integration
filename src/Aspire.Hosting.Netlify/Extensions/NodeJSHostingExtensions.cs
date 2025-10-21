// Copyright (c) David Pine. All rights reserved.
// Licensed under the MIT License.

namespace Aspire.Hosting;

public static partial class NodeJSHostingExtensions
{
    /// <summary>
    /// Configures the Node.js app to run an arbitrary npm command before starting.
    /// </summary>
    /// <param name="builder">The Node.js app resource builder.</param>
    /// <param name="args">The npm command arguments (e.g., "i", "run build:production", "ci --legacy-peer-deps").</param>
    /// <param name="configureRunner">Configure the npm runner resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// This method allows you to run any npm command before the app starts. For example:
    /// <code>
    /// builder.AddNpmApp("myapp", "../myapp", "dev")
    ///     .WithNpmCommand("i")  // npm i
    ///     .WithNpmCommand("run build:production");  // npm run build:production
    /// </code>
    /// </remarks>
    public static IResourceBuilder<NodeAppResource> WithNpmCommand(
        this IResourceBuilder<NodeAppResource> builder,
        string args,
        Action<IResourceBuilder<NpmCommandResource>>? configureRunner = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(args);

        // Sanitize args for use in resource name (replace spaces and special chars)
        var sanitizedArgs = args.Replace(" ", "-").Replace(":", "-");
        var runnerName = $"{builder.Resource.Name}-npm-{sanitizedArgs}";
        var runner = new NpmCommandResource(runnerName, builder.Resource.WorkingDirectory, args);

        var argsArray = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var runnerBuilder = builder.ApplicationBuilder.AddResource(runner)
            .WithArgs(argsArray)
            .WithParentRelationship(builder.Resource)
            .ExcludeFromManifest();

        // Make the parent resource wait for the runner to complete
        builder.WaitForCompletion(runnerBuilder);

        configureRunner?.Invoke(runnerBuilder);

        builder.WithAnnotation(new NpmCommandAnnotation(runner));

        return builder;
    }

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
        Action<IResourceBuilder<NpmCommandResource>>? configureRunner = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(scriptName);

        var runnerName = $"{builder.Resource.Name}-npm-run-{scriptName}";
        var runner = new NpmCommandResource(runnerName, builder.Resource.WorkingDirectory, scriptName);

        var runnerBuilder = builder.ApplicationBuilder.AddResource(runner)
            .WithArgs(["run", scriptName])
            .WithParentRelationship(builder.Resource)
            .ExcludeFromManifest();

        // Make the parent resource wait for the runner to complete
        builder.WaitForCompletion(runnerBuilder);

        configureRunner?.Invoke(runnerBuilder);

        builder.WithAnnotation(new NpmCommandAnnotation(runner));

        return builder;
    }

    /// <summary>
    /// Configures the Node.js app for deployment to Netlify using the Netlify CLI.
    /// Use together with <see cref="NetlifyDistributedApplicationPipelineExtensions.AddNetlifyDeployPipeline"/>.
    /// When that pipeline is added and this method is applied, the app is deployed with the provided options.
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