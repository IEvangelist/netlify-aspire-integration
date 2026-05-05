// Copyright (c) David Pine. All rights reserved.
// Licensed under the MIT License.

namespace Aspire.Hosting;

internal static partial class NetlifyDeploymentPipelineSteps
{
    /// <summary>
    /// Resolves which auth token to use given the parameter token and env var,
    /// per the documented precedence: parameter &gt; env var &gt; interactive login.
    /// </summary>
    /// <param name="parameterToken">The token resolved from the parameter resource (if any).</param>
    /// <param name="envToken">The token from <c>NETLIFY_AUTH_TOKEN</c> (if any).</param>
    /// <returns>
    /// The chosen token, or <see langword="null"/> if neither was set, in which case the caller
    /// should fall back to interactive <c>ntl login</c>.
    /// </returns>
    internal static string? ResolveAuthToken(string? parameterToken, string? envToken)
    {
        if (!string.IsNullOrWhiteSpace(parameterToken))
        {
            return parameterToken;
        }

        if (!string.IsNullOrWhiteSpace(envToken))
        {
            return envToken;
        }

        return null;
    }

    /// <summary>
    /// Resolves the absolute path to the Netlify CLI on PATH.
    /// Returns <c>"ntl"</c> if not found so the OS can fall back to PATH lookup at execution time.
    /// </summary>
    private static string ResolveNetlifyCliPath() => ResolveFullPathFromEnvironment("ntl");

    /// <summary>
    /// Returns <see langword="true"/> if the supplied path is a fully resolved absolute path
    /// (i.e. <see cref="ResolveFullPathFromEnvironment"/> actually located the executable).
    /// </summary>
    private static bool IsResolvedNetlifyCliPath(string path) =>
        !string.Equals(path, "ntl", StringComparison.Ordinal);

    /// <summary>
    /// Verifies the resolved Netlify CLI binary is actually executable by running
    /// <c>ntl --version</c>. A path on PATH that points to a broken shim (stale
    /// global install, missing Node, partial install) must not be treated as healthy.
    /// </summary>
    private static async Task<bool> IsNetlifyCliExecutableAsync(
        string ntlPath, CancellationToken cancellationToken)
    {
        try
        {
            var result = await CliWrapper.Wrap(ntlPath)
                .WithArguments("--version")
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync(cancellationToken);

            return result.IsSuccess;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Queries <c>npm config get prefix</c> and prepends the npm-global bin directory to the
    /// current process <c>PATH</c> if it isn't already present. This is needed so that
    /// resolution of <c>ntl</c> (and child-process invocation) succeeds in the same pipeline run
    /// as <c>npm install -g netlify-cli</c>, since the .NET process inherited PATH at startup.
    /// </summary>
    private static async Task EnsureNpmGlobalBinOnPathAsync(ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            var result = await CliWrapper.Wrap("npm")
                .WithArguments(["config", "get", "prefix"])
                .ExecuteBufferedAsync(cancellationToken);

            if (result.ExitCode != 0)
            {
                logger.LogWarning(
                    "Failed to query npm prefix (exit code {ExitCode}): {Error}",
                    result.ExitCode, result.StandardError);
                return;
            }

            var prefix = result.StandardOutput.Trim();
            if (string.IsNullOrWhiteSpace(prefix))
            {
                return;
            }

            // On Windows, npm installs scripts directly under prefix (no /bin subdir).
            // On Unix, they go under prefix/bin.
            var binDir = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? prefix
                : Path.Combine(prefix, "bin");

            var currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
            var separator = Path.PathSeparator.ToString();
            var pathSegments = currentPath.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

            if (pathSegments.Any(p => string.Equals(
                    Path.TrimEndingDirectorySeparator(p),
                    Path.TrimEndingDirectorySeparator(binDir),
                    StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            Environment.SetEnvironmentVariable("PATH", $"{binDir}{separator}{currentPath}");
            logger.LogInformation("Prepended npm global bin {BinDir} to PATH for this process.", binDir);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to ensure npm global bin is on PATH; relying on shell-time PATH lookup.");
        }
    }

    internal static async Task CheckForNetlifyCliAsync(PipelineStepContext context)
    {
        var reporter = context.Services.GetRequiredService<IPipelineActivityReporter>();
        var step = await reporter.CreateStepAsync(
            NetlifyDeployStepNames.GetFriendlyName(NetlifyDeployStepNames.CheckNetlifyCli),
            context.CancellationToken);

        await using (step.ConfigureAwait(false))
        {
            try
            {
                var resolveOnPathTask = await step.CreateTaskAsync(
                    "Resolving Netlify CLI on PATH", context.CancellationToken);

                var ntlPath = ResolveNetlifyCliPath();

                if (!IsResolvedNetlifyCliPath(ntlPath))
                {
                    await resolveOnPathTask.WarnAsync(
                        "Netlify CLI not found on PATH", context.CancellationToken);

                    return;
                }

                await resolveOnPathTask.CompleteAsync(
                    $"Found at {ntlPath}", cancellationToken: context.CancellationToken);

                var verifyInstallTask = await step.CreateTaskAsync(
                    "Verifying Netlify CLI installation", context.CancellationToken);

                BufferedCommandResult? result = null;

                try
                {
                    result = await CliWrapper.Wrap(ntlPath)
                        .WithArguments("--version")
                        .ExecuteBufferedAsync(context.CancellationToken);
                }
                catch (Exception ex)
                {
                    await verifyInstallTask.FailAsync(
                        $"Failed to execute Netlify CLI: {ex.Message}", context.CancellationToken);

                    return;
                }

                var completionMessage = result is not null
                    ? result.IsSuccess ? result.StandardOutput : result.StandardError
                    : "Unknown error verifying Netlify CLI";

                await verifyInstallTask.CompleteAsync(
                    completionMessage,
                    cancellationToken: context.CancellationToken);
            }
            catch (Exception ex)
            {
                context.Logger.LogWarning(ex, "Failed to check for Netlify CLI");
            }
        }
    }

    internal static async Task InstallNetlifyCliAsync(PipelineStepContext context)
    {
        var reporter = context.Services.GetRequiredService<IPipelineActivityReporter>();
        var cancellationToken = context.CancellationToken;
        var logger = context.Logger;

        var step = await reporter.CreateStepAsync(
            NetlifyDeployStepNames.GetFriendlyName(NetlifyDeployStepNames.InstallNetlifyCli),
            cancellationToken);

        await using (step.ConfigureAwait(false))
        {
            // Re-resolve every time so we never operate on stale state. We also verify the
            // resolved CLI actually runs (`ntl --version`) — a path on PATH but a broken
            // binary (stale shim, missing Node, partial install) must not be treated as
            // healthy or `npm i -g netlify-cli` will be skipped and later steps will fail.
            var ntlPath = ResolveNetlifyCliPath();
            if (IsResolvedNetlifyCliPath(ntlPath) &&
                await IsNetlifyCliExecutableAsync(ntlPath, cancellationToken))
            {
                await using (await step.CreateTaskAsync(
                    "Already installed, skipping", cancellationToken))
                {
                    return;
                }
            }

            try
            {
                var task = await step.CreateTaskAsync("npm i -g netlify-cli", cancellationToken);

                var result = await CliWrapper.Wrap("npm")
                    .WithArguments(["install", "-g", "netlify-cli"])
                    .ExecuteBufferedAsync(cancellationToken);

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

                // Ensure the npm global bin path is on PATH for this process so subsequent
                // resolution calls (and CliWrapper invocations) can locate the freshly installed
                // CLI. Without this, the post-install resolution often fails because the .NET
                // process cached the parent PATH at startup.
                await EnsureNpmGlobalBinOnPathAsync(logger, cancellationToken);

                // Verify post-install resolution. If it still doesn't resolve, the deploy step
                // will fall back to the bare "ntl" command and rely on shell-time PATH lookup.
                var postInstallPath = ResolveNetlifyCliPath();
                var completionMessage = IsResolvedNetlifyCliPath(postInstallPath)
                    ? $"Installation complete (resolved at {postInstallPath})"
                    : "Installation complete (relying on shell PATH lookup at execution time)";

                await task.CompleteAsync(completionMessage, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await step.FailAsync(
                    $"Installation failed: {ex.Message}", cancellationToken: cancellationToken);

                logger.LogError(ex, "Failed to install Netlify CLI");

                throw;
            }

            await step.CompleteAsync(
                "Netlify CLI ready", cancellationToken: cancellationToken);
        }
    }

    internal static async Task AuthenticateWithNetlifyAsync(PipelineStepContext context)
    {
        var loggedIn = false;
        var reporter = context.Services.GetRequiredService<IPipelineActivityReporter>();
        var cancellationToken = context.CancellationToken;

        var deployments = context.GetNetlifyDeploymentResources();

        var step = await reporter.CreateStepAsync(
            NetlifyDeployStepNames.GetFriendlyName(NetlifyDeployStepNames.AuthenticateNetlifyCli),
            cancellationToken);

        await using (step.ConfigureAwait(false))
        {
            foreach (var deployment in deployments)
            {
                var options = deployment.Options;
                var logger = context.Logger;

                // Auth precedence (highest first):
                //   1. Explicit parameter resource passed to PublishAsNetlifySite(authToken: ...)
                //   2. NETLIFY_AUTH_TOKEN environment variable
                //   3. Interactive `ntl login`
                var parameterToken = deployment.AuthToken is not null
                    ? await deployment.AuthToken.Resource.GetValueAsync(cancellationToken)
                    : null;

                var envToken = context.Services.GetRequiredService<IConfiguration>()
                    .GetValue<string>("NETLIFY_AUTH_TOKEN");

                if (!string.IsNullOrWhiteSpace(parameterToken)
                    && !string.IsNullOrWhiteSpace(envToken)
                    && !string.Equals(parameterToken, envToken, StringComparison.Ordinal))
                {
                    logger.LogWarning(
                        "Both an authToken parameter and NETLIFY_AUTH_TOKEN are set for deployment '{Deployment}'. "
                        + "The parameter token wins per documented precedence; this differs from pre-13.2 behavior.",
                        deployment.Name);
                }

                var resolvedToken = ResolveAuthToken(parameterToken, envToken);
                if (!string.IsNullOrWhiteSpace(resolvedToken))
                {
                    options.Auth = resolvedToken;
                }

                if (!string.IsNullOrWhiteSpace(options.Auth))
                {
                    logger.LogInformation("Using provided Netlify auth token, skipping login step.");

                    continue;
                }

                if (loggedIn)
                {
                    continue;
                }

                try
                {
                    var task = await step.CreateTaskAsync("ntl login", cancellationToken);

                    BufferedCommandResult? result;

                    try
                    {
                        result = await CliWrapper.Wrap(ResolveNetlifyCliPath())
                            .WithArguments("login")
                            .ExecuteBufferedAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        await task.FailAsync(
                            $"Login failed: {ex.Message}", cancellationToken);

                        throw;
                    }

                    if (result.IsSuccess)
                    {
                        loggedIn = true;

                        await task.CompleteAsync(
                            "Authentication successful", cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await task.FailAsync(
                            $"Login failed with exit code {result.ExitCode}",
                            cancellationToken: cancellationToken);
                    }
                }
                catch
                {
                    await step.FailAsync(
                        "Authentication failed", cancellationToken: cancellationToken);

                    throw;
                }

                await step.CompleteAsync(
                    "Authentication complete", cancellationToken: cancellationToken);
            }
        }
    }

    internal static async Task ResolveNetlifySiteIdAsync(PipelineStepContext context)
    {
        var logger = context.Logger;
        var cancellationToken = context.CancellationToken;
        var stateManager = context.Services.GetRequiredService<IDeploymentStateManager>();
        var reporter = context.Services.GetRequiredService<IPipelineActivityReporter>();
        var deployments = context.GetNetlifyDeploymentResources();

        var friendlyName = NetlifyDeployStepNames.GetFriendlyName(NetlifyDeployStepNames.ResolveNetlifySiteId);

        var step = await reporter.CreateStepAsync(
            friendlyName, cancellationToken);

        await using (step.ConfigureAwait(false))
        {
            foreach (var deployment in deployments)
            {
                var options = deployment.Options;
                if (!string.IsNullOrWhiteSpace(options.CreateSite))
                {
                    // Ensure JSON output for parsing site info after creation
                    options.Json = true;

                    continue;
                }

                var sectionName = $"netlify-{deployment.Name}";
                var section = await stateManager.AcquireSectionAsync(sectionName, cancellationToken);

                // Try loading site ID from deploy state file first.
                if (section.Data is not null &&
                    section.Data.TryGetValue("siteId", out var siteIdFromState, out var ex) &&
                    siteIdFromState is not null)
                {
                    options.Site = siteIdFromState.ToString();

                    continue;
                }

                if (options.Site is null)
                {
                    var state = await ReadNetlifyStateAsync(deployment.WorkingDirectory, logger, cancellationToken);
                    if (state is not null && !string.IsNullOrWhiteSpace(state.SiteId))
                    {
                        options.Site = state.SiteId;

                        if (section.Data is not null)
                        {
                            // Upsert property with the SiteId
                            section.Data["siteId"] = state.SiteId;

                            // Persist updated state
                            await stateManager.SaveSectionAsync(section, context.CancellationToken);
                        }

                        continue;
                    }
                }

                var interaction = context.Services.GetRequiredService<IInteractionService>();
                var configuration = context.Services.GetRequiredService<IConfiguration>();

                // If no site ID is provided, and interaction is available, prompt the user to enter one
                if (options.Site is null && interaction.IsAvailable(configuration))
                {
                    var result = await PromptForSiteIdAsync(interaction, cancellationToken);
                    if (result.Data is not null && result.Data.Value is { } siteId)
                    {
                        options.Site = siteId;

                        if (section.Data is not null)
                        {
                            // Upsert property with the SiteId
                            section.Data["siteId"] = siteId;

                            // Persist updated state
                            await stateManager.SaveSectionAsync(section, context.CancellationToken);
                        }

                        continue;
                    }
                }

                // Having this set limits our ability for multiple deploys.
                var siteIdEnvVar = configuration.GetValue<string>("NETLIFY_SITE_ID");
                if (!string.IsNullOrWhiteSpace(siteIdEnvVar))
                {
                    options.Site = siteIdEnvVar;
                }
            }
        }
    }

    private static async Task<InteractionResult<InteractionInput>> PromptForSiteIdAsync(
        IInteractionService interaction,
        CancellationToken cancellationToken)
    {
        return await interaction.PromptInputAsync(
            title: "Netlify project/site ID",
            message: """
                Please provide the Netlify Project/Site ID...

                It can be found in your project configuration, under "Project information".
                Copy the "Project ID" value, and paste it here:
                """,
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

    internal static async Task DeployToNetlifyAsync(PipelineStepContext context)
    {
        var deployments = context.GetNetlifyDeploymentResources();

        foreach (var deployment in deployments)
        {
            var options = deployment.Options;
            var reporter = context.Services.GetRequiredService<IPipelineActivityReporter>();
            var cancellationToken = context.CancellationToken;

            var friendlyName = NetlifyDeployStepNames.GetFriendlyName(NetlifyDeployStepNames.DeployToNetlify);
            var deployStep = await reporter.CreateStepAsync(
                $"{friendlyName} — {deployment.JavaScriptAppResourceName}",
                cancellationToken);

            await using (deployStep.ConfigureAwait(false))
            {
                var cliArgs = options.ToArguments();
                var resolvedWorkingDir = ResolveFullPath(deployment.WorkingDirectory);

                try
                {
                    await ExecuteNetlifyDeploymentAsync(
                        ResolveNetlifyCliPath(), cliArgs, resolvedWorkingDir, deployStep, deployment, context);
                }
                catch (Exception ex)
                {
                    await deployStep.FailAsync(
                        $"Netlify deployment failed: {ex.Message}", cancellationToken);

                    context.Logger.LogError(ex, "Netlify deployment failed");
                }
            }
        }
    }

    private static async Task ExecuteNetlifyDeploymentAsync(
        string ntlPath,
        CliArgs cliArgs,
        string workingDirectory,
        IReportingStep step,
        NetlifyDeploymentResource deployment,
        PipelineStepContext context)
    {
        var (rawArgs, redecatedArgs) = cliArgs;
        var logger = context.Logger;
        // var configuration = context.Services.GetRequiredService<IConfiguration>();
        // var interaction = context.Services.GetRequiredService<IInteractionService>();

        var task = await step.CreateTaskAsync(
            $"ntl {string.Join(" ", redecatedArgs)}", context.CancellationToken);

        await using (task.ConfigureAwait(false))
        {
            try
            {
                var ntlDeployCmd = CliWrapper
                    .Wrap(ntlPath)
                    .WithArguments(rawArgs)
                    .WithWorkingDirectory(workingDirectory);

                // // If interaction is supported, allow prompting from deploy command.
                // if (interaction.IsAvailable(configuration))
                // {
                //     ntlDeployCmd = ntlDeployCmd
                //         .WithStandardInputPipe(PipeSource.FromStream(Console.OpenStandardInput()))
                //         .WithStandardOutputPipe(PipeTarget.ToStream(Console.OpenStandardOutput()))
                //         .WithStandardErrorPipe(PipeTarget.ToStream(Console.OpenStandardError()));
                // }

                var result = await ntlDeployCmd.ExecuteBufferedAsync(context.CancellationToken);
                if (result.ExitCode is 0)
                {
                    if (deployment.Options.Json is true)
                    {
                        try
                        {
                            var siteInfo = JsonSerializer.Deserialize(
                                json: result.StandardOutput,
                                jsonTypeInfo: NetlifySnakeCaseContext.Default.NetlifySite);

                            if (siteInfo is not null)
                            {
                                var message = GetDeploymentCompletionMessage(siteInfo.DeployUrl, siteInfo.Logs);
                                await task.SucceedAsync(message, cancellationToken: context.CancellationToken);

                                var stateManager = context.Services.GetRequiredService<IDeploymentStateManager>();
                                var sectionName = $"netlify-{deployment.Name}";
                                var section = await stateManager.AcquireSectionAsync(sectionName, context.CancellationToken);
                                if (section.Data is not null && siteInfo.SiteId is not null)
                                {
                                    // Upsert property with the SiteId
                                    section.Data["siteId"] = siteInfo.SiteId;

                                    // Persist updated state
                                    await stateManager.SaveSectionAsync(section, context.CancellationToken);
                                }
                                else
                                {
                                    if (logger.IsEnabled(LogLevel.Warning))
                                    {
                                        logger.LogWarning(
                                            "Failed to acquire deployment state or SiteId is null, not saving state.");
                                    }
                                }
                            }
                        }
                        catch (JsonException jex)
                        {
                            logger.LogWarning(jex, "Failed to parse Netlify CLI JSON output");
                        }
                    }
                    else
                    {
                        // Scan output (in order) to capture the first draft deploy URL and build logs URL.
                        string? draftDeployUrl = null;
                        string? deployLogsUrl = null;

                        var draftRegex = NetlifyDraftDeployUrlRegex();
                        var logsRegex = NetlifyDeployLogsUrlRegex();

                        foreach (var line in result.StandardOutput.Split(
                            ['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
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

                        var message = GetDeploymentCompletionMessage(draftDeployUrl, deployLogsUrl);
                        await task.SucceedAsync(message, cancellationToken: context.CancellationToken);
                    }
                }
                else
                {
                    var errorMessage = result.StandardError.Length > 0
                        ? result.StandardError
                        : $"Deployment failed with exit code {result.ExitCode}";

                    await task.FailAsync(errorMessage, cancellationToken: context.CancellationToken);

                    throw new InvalidOperationException($"Netlify deployment failed: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to deploy to Netlify");

                throw;
            }
        }
    }

    internal static async Task RunNpmCommandsAsync(PipelineStepContext context)
    {
        var logger = context.Logger;
        var reporter = context.Services.GetRequiredService<IPipelineActivityReporter>();
        var cancellationToken = context.CancellationToken;

        var step = await reporter.CreateStepAsync(
            NetlifyDeployStepNames.GetFriendlyName(NetlifyDeployStepNames.RunNpmCommands),
            cancellationToken);

        await using (step.ConfigureAwait(false))
        {
            var deployments = context.GetNetlifyDeploymentResources();

            foreach (var deployment in deployments)
            {
                var javaScriptApp = deployment.JavaScriptAppResource;

                // Get all NpmRunnerAnnotations for this JavaScript app
                var npmRunners = javaScriptApp.Annotations
                    .OfType<NpmCommandAnnotation>()
                    .Select(a => a.Resource)
                    .ToList();

                if (npmRunners.Count == 0)
                {
                    continue;
                }

                foreach (var runner in npmRunners)
                {
                    var argumentConfiguration = await ExecutionConfigurationBuilder.Create(runner)
                        .WithArgumentsConfig()
                        .BuildAsync(new(DistributedApplicationOperation.Publish), logger, CancellationToken.None)
                        .ConfigureAwait(false);

                    var args = argumentConfiguration.Arguments.Select(a => a.Value).ToArray();

                    var task = await step.CreateTaskAsync(
                        $"npm {string.Join(" ", args)} ({javaScriptApp.Name})",
                        cancellationToken);

                    await using (task.ConfigureAwait(false))
                    {
                        try
                        {
                            var result = await CliWrapper.Wrap("npm")
                                .WithArguments(args)
                                .WithWorkingDirectory(runner.WorkingDirectory)
                                .ExecuteBufferedAsync(cancellationToken);

                            if (result.ExitCode is 0)
                            {
                                await task.CompleteAsync(
                                    $"Command completed successfully",
                                    cancellationToken: cancellationToken);
                            }
                            else
                            {
                                var errorMessage = result.StandardError.Length > 0
                                    ? result.StandardError
                                    : $"npm command exited with code {result.ExitCode}";

                                await task.FailAsync(errorMessage, cancellationToken: cancellationToken);

                                throw new InvalidOperationException($"npm command failed: {errorMessage}");
                            }
                        }
                        catch (Exception ex)
                        {
                            context.Logger.LogError(ex, "Failed to execute npm command");
                            throw;
                        }
                    }
                }
            }
        }
    }

    private static string GetDeploymentCompletionMessage(string? deployUrl, string? deployLogsUrl)
    {
        var message = (deployUrl, deployLogsUrl) switch
        {
            (not null, not null) =>
                $"Deployment complete\n—URL: {deployUrl}\n—Logs: {deployLogsUrl}",
            (not null, null) => $"Deployment complete\n—URL: {deployUrl}",
            (null, not null) => $"Deployment complete\n—Logs: {deployLogsUrl}",

            _ => "Deployment complete"
        }; return message;
    }

    [GeneratedRegex(@"https?://[A-Za-z0-9-]+(?:--[A-Za-z0-9-]+)?\.netlify\.app\b", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex NetlifyDraftDeployUrlRegex();

    [GeneratedRegex(@"https?://[^\s|]*?/deploys/[A-Za-z0-9]+[^\s|]*", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex NetlifyDeployLogsUrlRegex();
}