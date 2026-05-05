// Copyright (c) David Pine. All rights reserved.
// Licensed under the MIT License.

namespace Aspire.Hosting;

public static partial class JavaScriptHostingExtensions
{
    /// <summary>
    /// Configures the JavaScript app to run an arbitrary npm command before starting.
    /// </summary>
    /// <param name="builder">The JavaScript app resource builder.</param>
    /// <param name="args">The npm command arguments (e.g., "i", "run build:production", "ci --legacy-peer-deps").</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// This method allows you to run any npm command before the app starts. For example:
    /// <code>
    /// builder.AddJavaScriptApp("myapp", "../myapp")
    ///     .WithNpmCommand("i")  // npm i
    ///     .WithNpmCommand("run build:production");  // npm run build:production
    /// </code>
    /// </remarks>
    [AspireExport(MethodName = "withNpmCommand", Description = "Runs an npm command before the app starts.")]
    public static IResourceBuilder<JavaScriptAppResource> WithNpmCommand(
        this IResourceBuilder<JavaScriptAppResource> builder,
        string args)
        => WithNpmCommandCore(builder, args, configureRunner: null);

    /// <summary>
    /// Configures the JavaScript app to run an arbitrary npm command before starting,
    /// with a callback to further configure the resulting <see cref="NpmCommandResource"/>.
    /// </summary>
    /// <param name="builder">The JavaScript app resource builder.</param>
    /// <param name="args">The npm command arguments (e.g., "i", "run build:production", "ci --legacy-peer-deps").</param>
    /// <param name="configureRunner">Configure the npm runner resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// This overload accepts a synchronous <see cref="Action{T}"/> callback that runs inline at AppHost
    /// build time, which is not modeled by the Aspire Type System and therefore not exported to
    /// non-C# AppHosts. From a TypeScript AppHost, use the no-callback overload of <c>withNpmCommand</c>
    /// and configure additional runner state via the underlying <c>npmCommandResource</c> APIs.
    /// </remarks>
    [AspireExportIgnore]
    public static IResourceBuilder<JavaScriptAppResource> WithNpmCommand(
        this IResourceBuilder<JavaScriptAppResource> builder,
        string args,
        Action<IResourceBuilder<NpmCommandResource>>? configureRunner)
        => WithNpmCommandCore(builder, args, configureRunner);

    private static IResourceBuilder<JavaScriptAppResource> WithNpmCommandCore(
        IResourceBuilder<JavaScriptAppResource> builder,
        string args,
        Action<IResourceBuilder<NpmCommandResource>>? configureRunner)
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
    /// Configures the JavaScript app to run an npm script before starting.
    /// </summary>
    /// <param name="builder">The JavaScript app resource builder.</param>
    /// <param name="scriptName">The npm script name to run (e.g., "build", "test", "lint").</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    [AspireExport(MethodName = "withNpmRunCommand", Description = "Runs an npm script (e.g., 'build') before the app starts.")]
    public static IResourceBuilder<JavaScriptAppResource> WithNpmRunCommand(
        this IResourceBuilder<JavaScriptAppResource> builder,
        string scriptName)
        => WithNpmRunCommandCore(builder, scriptName, configureRunner: null);

    /// <summary>
    /// Configures the JavaScript app to run an npm script before starting,
    /// with a callback to further configure the resulting <see cref="NpmCommandResource"/>.
    /// </summary>
    /// <param name="builder">The JavaScript app resource builder.</param>
    /// <param name="scriptName">The npm script name to run (e.g., "build", "test", "lint").</param>
    /// <param name="configureRunner">Configure the npm runner resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// This overload accepts a synchronous <see cref="Action{T}"/> callback that runs inline at AppHost
    /// build time, which is not modeled by the Aspire Type System and therefore not exported to
    /// non-C# AppHosts. From a TypeScript AppHost, use the no-callback overload of <c>withNpmRunCommand</c>.
    /// </remarks>
    [AspireExportIgnore]
    public static IResourceBuilder<JavaScriptAppResource> WithNpmRunCommand(
        this IResourceBuilder<JavaScriptAppResource> builder,
        string scriptName,
        Action<IResourceBuilder<NpmCommandResource>>? configureRunner)
        => WithNpmRunCommandCore(builder, scriptName, configureRunner);

    private static IResourceBuilder<JavaScriptAppResource> WithNpmRunCommandCore(
        IResourceBuilder<JavaScriptAppResource> builder,
        string scriptName,
        Action<IResourceBuilder<NpmCommandResource>>? configureRunner)
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
    /// Configures the JavaScript app for deployment to Netlify using the Netlify CLI.
    /// Use together with <see cref="NetlifyDistributedApplicationPipelineExtensions.AddNetlifyDeployPipeline"/>.
    /// When that pipeline is added and this method is applied, the app is deployed with the provided options.
    /// </summary>
    /// <param name="builder">The JavaScript app resource builder.</param>
    /// <param name="options">The Netlify deployment options.</param>
    /// <param name="authToken">An optional parameter resource containing the Netlify authentication token.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the Netlify CLI is not installed or not found in PATH.</exception>
    [AspireExport(MethodName = "publishAsNetlifySite", Description = "Configures the JavaScript app to deploy to Netlify with the supplied options.")]
    public static IResourceBuilder<JavaScriptAppResource> PublishAsNetlifySite(
        this IResourceBuilder<JavaScriptAppResource> builder,
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

            builder.WithAnnotation(new NetlifyDeploymentAnnotation(deployment));
        }

        return builder;
    }

    /// <summary>
    /// Configures the JavaScript app for deployment to Netlify by publishing a prebuilt directory.
    /// </summary>
    /// <param name="builder">The JavaScript app resource builder.</param>
    /// <param name="dir">
    /// The directory containing the prebuilt site to deploy (e.g., <c>"dist"</c>, <c>"build"</c>, <c>".next"</c>).
    /// Resolved relative to the JavaScript app's working directory.
    /// </param>
    /// <param name="authToken">An optional parameter resource containing the Netlify authentication token.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// <para>
    /// This is a convenience overload for the most common scenario: the JavaScript app produces a static
    /// build output and you want Netlify to upload it as-is. It forwards to
    /// <see cref="PublishAsNetlifySite(IResourceBuilder{JavaScriptAppResource}, NetlifyDeployOptions, IResourceBuilder{ParameterResource})"/>
    /// with <c>NoBuild = true</c> and <c>Dir = dir</c>. For full control over deployment options
    /// (site, alias, message, prod, environment-context, etc.), use that overload directly.
    /// </para>
    /// <para>
    /// Pair with <see cref="WithNpmRunCommand(IResourceBuilder{JavaScriptAppResource}, string)"/>
    /// (e.g., <c>WithNpmRunCommand("build")</c>) to ensure the prebuilt directory exists before
    /// deployment runs in the pipeline.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.AddJavaScriptApp("astro-site", "../astro")
    ///     .WithNpmRunCommand("build")
    ///     .PublishAsNetlifySite("dist");
    /// </code>
    /// </example>
    [AspireExportIgnore]
    public static IResourceBuilder<JavaScriptAppResource> PublishAsNetlifySite(
        this IResourceBuilder<JavaScriptAppResource> builder,
        string dir,
        IResourceBuilder<ParameterResource>? authToken = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(dir);

        return builder.PublishAsNetlifySite(
            new NetlifyDeployOptions
            {
                Dir = dir,
                NoBuild = true,
            },
            authToken);
    }
}