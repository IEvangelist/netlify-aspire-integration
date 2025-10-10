using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using CliWrap.Buffered;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Need an alias, both Aspire.Hosting.Cli and CliWrap.Cli exist
using CliWrapper = CliWrap.Cli;
using CommandOnPath = (bool IsFound, string? Path);

namespace Aspire.Hosting;

#pragma warning disable ASPIREINTERACTION001
#pragma warning disable ASPIREPUBLISHERS001

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
    /// Sets the <c>NETLIFY_SITE_ID</c> environment variable for the Node.js app.
    /// For more information, see <see href="https://docs.netlify.com/api-and-cli-guides/cli-guides/get-started-with-cli/#link-with-an-environment-variable">Netlify CLI documentation: Link with an environment variable</see>.
    /// <remarks>
    /// Go to <b>Project configuration</b> > <b>General</b> > <b>Project details</b> > <b>Project information</b>,
    /// and copy the value for <b>Project ID</b>. (Also known as Site ID.)
    /// </remarks>
    /// </summary>
    /// <param name="builder">The Node.js app resource builder.</param>
    /// <param name="siteId">The Netlify Site ID value.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<NodeAppResource> WithEnvironmentSiteId(
        this IResourceBuilder<NodeAppResource> builder,
        IResourceBuilder<ParameterResource> siteId)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(siteId);

        return builder.WithEnvironment("NETLIFY_SITE_ID", siteId);
    }

    /// <summary>
    /// Configures the Node.js app to be deployed to Netlify using the Netlify CLI
    /// with default deployment options.
    /// </summary>
    /// <param name="builder">The Node.js app resource builder.</param>
    /// <param name="buildDir">The directory to deploy (relative to the working directory).</param>
    /// <param name="siteName">The optional Netlify site name (alias) for the deployment.</param>
    /// <param name="deploymentEnvironment">The deployment environment: "preview" (default), "staging", or "production".</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the Netlify CLI is
    /// not installed or not found in PATH.</exception>
    /// <remarks>
    /// Go to <b>Project configuration</b> > <b>General</b> > <b>Project details</b> > <b>Project information</b>,
    /// and copy the value for <b>Project ID</b>. (Also known as Site ID.)
    /// </remarks>
    public static IResourceBuilder<NodeAppResource> PublishAsNetlifySite(
        this IResourceBuilder<NodeAppResource> builder,
        string buildDir = "dist",
        string? siteName = null,
        string deploymentEnvironment = "preview") => builder.PublishAsNetlifySite(new NetlifyDeployOptions()
        {
            Dir = buildDir,
            Alias = siteName,
            Prod = string.Equals(deploymentEnvironment, "prod", StringComparison.OrdinalIgnoreCase)
                || string.Equals(deploymentEnvironment, "production", StringComparison.OrdinalIgnoreCase)
        });

    /// <summary>
    /// Configures the Node.js app to be deployed to Netlify using the Netlify CLI with custom deployment options.
    /// </summary>
    /// <param name="builder">The Node.js app resource builder.</param>
    /// <param name="options">The Netlify deployment options.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the Netlify CLI is not installed or not found in PATH.</exception>
    public static IResourceBuilder<NodeAppResource> PublishAsNetlifySite(
        this IResourceBuilder<NodeAppResource> builder,
        NetlifyDeployOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);

        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            var deployerName = $"{builder.Resource.Name}-netlify-deploy";
            var deployer = new NetlifyDeployerResource(
                deployerName,
                builder.Resource.WorkingDirectory,
                options.Dir ?? "dist",
                options.Site,
                "custom");

            var deployerBuilder = builder.ApplicationBuilder.AddResource(deployer)
                .WithParentRelationship(builder.Resource)
                .ExcludeFromManifest();

            builder.WithAnnotation(new DeployingCallbackAnnotation(async (context) =>
            {
                await PerformNetlifyDeployment(
                    deployer,
                    context,
                    options,
                    CancellationToken.None);
            }));

            builder.WithAnnotation(new NetlifyDeploymentAnnotation(deployer));
        }

        return builder;
    }

    /// <summary>
    /// Checks if the Netlify CLI is available in the system PATH.
    /// </summary>
    /// <returns>True if Netlify CLI is available, false otherwise.</returns>
    private static async Task<CommandOnPath> IsNetlifyCliAvailableAsync(IPublishingActivityReporter reporter)
    {
        var step = await reporter.CreateStepAsync("Checking for Netlify CLI.");

        try
        {
            var task = await step.CreateTaskAsync("Resolving Netlify CLI on PATH");

            var ntlPath = ResolveOnPath("ntl");
            if (string.Equals(ntlPath, "ntl", StringComparison.Ordinal))
            {
                await task.WarnAsync("Netlify CLI not found on PATH");
                await task.CompleteAsync();
            }
            else
            {
                await task.CompleteAsync($"Found Netlify CLI at: {ntlPath}");
            }

            task = await step.CreateTaskAsync("Verifying Netlify CLI installation");
            BufferedCommandResult? result;

            try
            {
                result = await CliWrapper.Wrap(ntlPath)
                    .WithArguments("--version")
                    .ExecuteBufferedAsync();
            }
            catch (Exception ex)
            {
                await task.FailAsync($"Failed to execute Netlify CLI: {ex.Message}");

                return (false, null);
            }

            await task.CompleteAsync(result.IsSuccess ? result.StandardOutput : result.StandardError);

            return (result.IsSuccess, ntlPath);
        }
        catch
        {
            return (false, null);
        }
        finally
        {
            await step.CompleteAsync("Netlify CLI check completed");
        }
    }

    private static string ResolveOnPath(string name)
    {
        var paths = (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        string[] exts;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // On Windows, always use PATHEXT and prioritize common executables
            var pathext = Environment.GetEnvironmentVariable("PATHEXT") ?? ".COM;.EXE;.BAT;.CMD";
            exts = pathext.Split(';', StringSplitOptions.RemoveEmptyEntries);
        }
        else
        {
            exts = [];
        }

        foreach (var dir in paths)
        {
            // On Windows, check for extensions first before the bare name
            // This handles cases where npm installs both 'ntl' (Unix) and 'ntl.cmd' (Windows)
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                foreach (var ext in exts)
                {
                    var candidate = Path.Combine(dir, name + ext);

                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
            }

            var bareCandidate = Path.Combine(dir, name);

            if (File.Exists(bareCandidate))
            {
                return bareCandidate;
            }

            // On non-Windows, check extensions after the bare name
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                foreach (var ext in exts)
                {
                    var candidate = Path.Combine(dir, name + ext);

                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
            }
        }

        return name; // fallback to name — OS will search PATH
    }

    /// <summary>
    /// Resolves a path to a fully qualified absolute path.
    /// If the path is already rooted, returns it as-is (normalized).
    /// Otherwise, combines it with the base directory and normalizes.
    /// </summary>
    /// <param name="path">The path to resolve.</param>
    /// <param name="baseDirectory">The base directory to use if path is relative. If null or empty, uses current directory.</param>
    /// <returns>A fully qualified absolute path.</returns>
    private static string ResolveFullPath(string path, string? baseDirectory = null)
    {
        if (Path.IsPathRooted(path))
        {
            return Path.GetFullPath(path);
        }

        var basePath = string.IsNullOrEmpty(baseDirectory)
            ? Directory.GetCurrentDirectory()
            : baseDirectory;

        return Path.GetFullPath(Path.Combine(basePath, path));
    }

    /// <summary>
    /// Performs the actual Netlify deployment using the Netlify CLI with custom options.
    /// </summary>
    /// <param name="deployer">The Netlify deployer resource.</param>
    /// <param name="context">The deployment context.</param>
    /// <param name="options">The Netlify deployment options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private static async Task PerformNetlifyDeployment(
        NetlifyDeployerResource deployer,
        DeployingContext context,
        NetlifyDeployOptions options,
        CancellationToken cancellationToken = default)
    {
        var logger = context.Logger;
        var reporter = context.ActivityReporter;
        var interaction = context.Services.GetRequiredService<IInteractionService>();

        // Check if Netlify CLI is available on PATH
        var commandInfo = await IsNetlifyCliAvailableAsync(reporter);
        if (commandInfo is { IsFound: false })
        {
            await InstallNetlifyCliAsync(reporter, logger, cancellationToken);

            // throw new InvalidOperationException("""
            //     Netlify CLI is not installed or not available in PATH. Please install it using 'npm install -g netlify-cli' or visit https://docs.netlify.com/cli/get-started/
            //     """);
        }

        if (interaction.IsAvailable)
        {
            var result = await PromptForProjectIdAsync(interaction, cancellationToken);
            if (result.Data is not null && result.Data.Value is { } siteId)
            {
                options.Site = siteId;
            }
        }

        var step = await reporter.CreateStepAsync(
            $"Deploying to Netlify ({options.ToEnvironmentDescription()})", cancellationToken);

        try
        {
            var task = await step.CreateTaskAsync("Resolving deployment paths", cancellationToken);

            var resolvedWorkingDir = ResolveFullPath(deployer.WorkingDirectory);
            var resolvedBuildDir = ResolveFullPath(options.Dir ?? deployer.BuildDirectory, resolvedWorkingDir);

            await task.CompleteAsync($"Build directory: {resolvedBuildDir}", cancellationToken: cancellationToken);

            task = await step.CreateTaskAsync("Preparing deployment command", cancellationToken);

            var args = options.ToArguments(resolvedBuildDir);

            await task.CompleteAsync($"Command: ntl {string.Join(" ", args)}", cancellationToken: cancellationToken);

            // Execute deployment
            await ExecuteNetlifyDeploymentAsync(
                commandInfo.Path!, args, resolvedWorkingDir, step, logger, cancellationToken);
        }
        catch (Exception ex)
        {
            await step.FailAsync($"Deployment failed: {ex.Message}", cancellationToken: cancellationToken);

            logger.LogError(ex, "Failed to deploy to Netlify");

            throw;
        }
    }

    private static async Task<InteractionResult<InteractionInput>> PromptForProjectIdAsync(
        IInteractionService interaction,
        CancellationToken cancellationToken)
    {
        return await interaction.PromptInputAsync(
            title: "Netlify project/site ID",
            message: "Please provide the Netlify project/site ID",
            inputLabel: "Netlify Project ID",
            placeHolder: Guid.Empty.ToString(),
            options: new InputsDialogInteractionOptions()
            {
                EnableMessageMarkdown = true,
                ValidationCallback = static context =>
                {
                    var projectIdInput = context.Inputs.FirstOrDefault(i => i.Label is "Netlify Project ID");

                    if (projectIdInput is not null &&
                        Guid.TryParse(projectIdInput.Value, out var projectIdGuid) is false)
                    {
                        context.AddValidationError(projectIdInput!, "The project/site ID must be a GUID.");
                    }

                    return Task.CompletedTask;
                }
            },
            cancellationToken: cancellationToken);
    }

    private static async Task InstallNetlifyCliAsync(
        IPublishingActivityReporter reporter,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var command = CliWrapper.Wrap("npm")
            .WithArguments(["install", "-g", "netlify-cli"])
            .WithStandardOutputPipe(CliWrap.PipeTarget.ToDelegate(line =>
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    return;
                }

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("npm: {Output}", line);
                }
            }))
            .WithStandardErrorPipe(CliWrap.PipeTarget.ToDelegate(line =>
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    return;
                }

                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("npm: {Error}", line);
                }
            }));

        var result = await command.ExecuteBufferedAsync(cancellationToken);
        if (result.ExitCode is not 0)
        {
            var errorMessage = result.StandardError.Length > 0
                ? result.StandardError
                : $"npm install exited with code {result.ExitCode}";

            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError("Failed to install Netlify CLI: {Error}", errorMessage);
            }

            throw new InvalidOperationException($"Failed to install Netlify CLI: {errorMessage}");
        }
    }

    /// <summary>
    /// Executes the Netlify CLI deployment command.
    /// </summary>
    private static async Task ExecuteNetlifyDeploymentAsync(
        string ntlPath,
        List<string> args,
        string workingDirectory,
        IPublishingStep step,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            // Execute deployment
            var task = await step.CreateTaskAsync("Executing Netlify deployment", cancellationToken);

            var outputLines = new List<string>();
            var errorLines = new List<string>();

            var cmd = CliWrapper
                .Wrap(ntlPath)
                .WithArguments(args)
                .WithWorkingDirectory(workingDirectory)
                .WithStandardOutputPipe(CliWrap.PipeTarget.ToDelegate(line =>
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        return;
                    }

                    outputLines.Add(line);

                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation("Netlify: {Output}", line);
                    }
                }))
                .WithStandardErrorPipe(CliWrap.PipeTarget.ToDelegate(async line =>
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        return;
                    }

                    errorLines.Add(line);

                    await task.UpdateAsync(line);

                    if (logger.IsEnabled(LogLevel.Warning))
                    {
                        logger.LogWarning("Netlify: {Error}", line);
                    }
                }));

            var result = await cmd.ExecuteAsync(cancellationToken);

            if (result.ExitCode is 0)
            {
                // Scan output (in order) to capture the first draft deploy URL and build logs URL.
                string? draftDeployUrl = null;
                string? deployLogsUrl = null;

                var draftRegex = NetlifyDraftDeployUrlRegex();
                var logsRegex = NetlifyDeployLogsUrlRegex();

                foreach (var line in outputLines)
                {
                    if (draftDeployUrl is null)
                    {
                        var m = draftRegex.Match(line);
                        if (m.Success)
                        {
                            draftDeployUrl = m.Value.Trim();
                        }
                    }

                    if (deployLogsUrl is null)
                    {
                        var m = logsRegex.Match(line);
                        if (m.Success)
                        {
                            deployLogsUrl = m.Value.Trim();
                        }
                    }

                    if (draftDeployUrl is not null && deployLogsUrl is not null)
                    {
                        break;
                    }
                }

                var message = (draftDeployUrl, deployLogsUrl) switch
                {
                    (not null, not null) => $"Deployed successfully:\n      URL: {draftDeployUrl}\n      Logs: {deployLogsUrl}",
                    (not null, null) => $"Deployed successfully: {draftDeployUrl}",
                    (null, not null) => $"Deployed successfully (logs: {deployLogsUrl})",
                    _ => "Deployed successfully"
                };

                await task.CompleteAsync(message, cancellationToken: cancellationToken);

                await step.CompleteAsync(
                    "Netlify deployment completed successfully", cancellationToken: cancellationToken);
            }
            else
            {
                var errorMessage = errorLines.Count > 0
                    ? string.Join(Environment.NewLine, errorLines)
                    : $"Deployment failed with exit code {result.ExitCode}";

                await task.FailAsync(errorMessage, cancellationToken: cancellationToken);

                throw new InvalidOperationException($"Netlify deployment failed: {errorMessage}");
            }
        }
        catch (Exception ex)
        {
            await step.FailAsync($"Deployment failed: {ex.Message}", cancellationToken: cancellationToken);

            logger.LogError(ex, "Failed to deploy to Netlify");

            throw;
        }
    }

    // Matches a Netlify draft (or any) deploy URL ending with .netlify.app (ignores anything with a / after the host).
    [GeneratedRegex(@"https?://[A-Za-z0-9-]+(?:--[A-Za-z0-9-]+)?\.netlify\.app\b", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex NetlifyDraftDeployUrlRegex();

    // Matches Netlify build log URLs that contain a /deploys/{id}/ segment.
    // Examples:
    //   https://app.netlify.com/sites/my-site/deploys/6623456a8e12bc0008cfe321
    //   https://api.netlify.com/api/v1/sites/xyz/deploys/6623456a8e12bc0008cfe321/log
    [GeneratedRegex(@"https?://[^\s|]*?/deploys/[A-Za-z0-9]+[^\s|]*", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex NetlifyDeployLogsUrlRegex();
}

#pragma warning restore ASPIREINTERACTION001
#pragma warning restore ASPIREPUBLISHERS001