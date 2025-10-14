namespace Aspire.Hosting;

internal partial class NetlifyDeployPipelineStepFactory(params NetlifyDeploymentResource[] deployments)
{
    private readonly NetlifyDeploymentResource[] _deployments = deployments;
    private CommandOnPath _commandOnPath = (false, null);

    public IEnumerable<PipelineStep> CreatePipelineSteps()
    {
        var checkForNetlifyCliStep = new PipelineStep
        {
            Name = " Check for Netlify CLI",
            Action = async context =>
            {
                _commandOnPath = await IsNetlifyCliAvailableAsync(context);
            }
        };

        var installNetlifyCliStep = new PipelineStep
        {
            Name = " Install Netlify CLI",
            Action = async context =>
            {
                var installPath = await InstallNetlifyCliAsync(context);
                if (installPath is not null)
                {
                    _commandOnPath = (true, installPath);
                }
            }
        };

        installNetlifyCliStep.DependsOn(checkForNetlifyCliStep);

        var netlifyLoginStep = new PipelineStep
        {
            Name = $"Netlify CLI Login (or use auth token)",
            Action = async context =>
            {
                await PerformNetlifyCliLoginAsync(context, _deployments);
            }
        };

        netlifyLoginStep.DependsOn(checkForNetlifyCliStep);
        netlifyLoginStep.DependsOn(installNetlifyCliStep);

        var determineSiteIdStep = new PipelineStep
        {
            Name = "Finding Site ID (or prompting for it)",
            Action = async context =>
            {
                await FindSiteIdAsync(context, _deployments);
            }
        };

        var deployStep = new PipelineStep
        {
            Name = $"Deploy to Netlify",
            Action = async context =>
            {
                await CallNetlifyDeployAsync(_commandOnPath.Path!, _deployments, context);
            }
        };

        deployStep.DependsOn(netlifyLoginStep);

        return
        [
            checkForNetlifyCliStep,
            installNetlifyCliStep,
            netlifyLoginStep,
            determineSiteIdStep,
            deployStep
        ];
    }

    private static async Task<CommandOnPath> IsNetlifyCliAvailableAsync(DeployingContext context)
    {
        var step = await context.ActivityReporter.CreateStepAsync(
            "Checking for Netlify CLI.", context.CancellationToken);

        await using (step.ConfigureAwait(false))
        {
            try
            {
                var resolveOnPathTask = await step.CreateTaskAsync(
                    "Resolving Netlify CLI on PATH", context.CancellationToken);

                var ntlPath = ResolveOnPath("ntl");
                if (string.Equals(ntlPath, "ntl", StringComparison.Ordinal))
                {
                    await resolveOnPathTask.WarnAsync(
                        "Netlify CLI not found on PATH", context.CancellationToken);
                }
                else
                {
                    await resolveOnPathTask.CompleteAsync(
                        $"Found Netlify CLI at: {ntlPath}", cancellationToken: context.CancellationToken);
                }

                var verifyInstallTask = await step.CreateTaskAsync(
                    "Verifying Netlify CLI installation", context.CancellationToken);

                BufferedCommandResult? result;

                try
                {
                    result = await CliWrapper.Wrap(ntlPath)
                        .WithArguments("--version")
                        .ExecuteBufferedAsync(context.CancellationToken);
                }
                catch (Exception ex)
                {
                    await resolveOnPathTask.FailAsync(
                        $"Failed to execute Netlify CLI: {ex.Message}", context.CancellationToken);

                    return (false, null);
                }

                await resolveOnPathTask.CompleteAsync(
                    result.IsSuccess ? result.StandardOutput : result.StandardError,
                    cancellationToken: context.CancellationToken);

                return (result.IsSuccess, ntlPath);
            }
            catch
            {
                return (false, null);
            }
            finally
            {
                await step.CompleteAsync(
                    "Done checking for Netlify CLI", cancellationToken: context.CancellationToken);
            }
        }
    }

    private static async Task<string?> InstallNetlifyCliAsync(DeployingContext context)
    {
        var reporter = context.ActivityReporter;
        var logger = context.Logger;
        var cancellationToken = context.CancellationToken;

        var step = await reporter.CreateStepAsync("Installing Netlify CLI", cancellationToken);

        await using (step.ConfigureAwait(false))
        {
            try
            {
                var task = await step.CreateTaskAsync("npm i -g netlify-cli", cancellationToken);

                var command = CliWrapper.Wrap("npm")
                    .WithArguments(["install", "-g", "netlify-cli"])
                    .WithStandardOutputPipe(CliWrap.PipeTarget.ToDelegate(async line =>
                    {
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            return;
                        }

                        await task.UpdateAsync(line);

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
                else
                {
                    await task.CompleteAsync(
                        "Installed Netlify CLI", cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await step.FailAsync(
                    $"Failed to install Netlify CLI: {ex.Message}", cancellationToken: cancellationToken);

                logger.LogError(ex, "Failed to install Netlify CLI");

                throw;
            }

            await step.CompleteAsync(
                "Netlify CLI installation step completed", cancellationToken: cancellationToken);

            return "ntl";
        }
    }

    private static async Task PerformNetlifyCliLoginAsync(
        DeployingContext context,
        NetlifyDeploymentResource[] deployments)
    {
        var loggedIn = false;

        foreach (var deployment in deployments)
        {
            if (loggedIn)
            {
                return;
            }

            var options = deployment.Options;
            var reporter = context.ActivityReporter;
            var logger = context.Logger;
            var cancellationToken = context.CancellationToken;

            // If an auth token resource is provided, resolve its value
            if (deployment.AuthToken is not null)
            {
                var authToken = await deployment.AuthToken.Resource.GetValueAsync(cancellationToken);
                if (!string.IsNullOrWhiteSpace(authToken))
                {
                    options.Auth = authToken;
                }
            }

            // If the NETLIFY_AUTH_TOKEN env var is set, use that
            var authTokenEnvVar = context.Services.GetRequiredService<IConfiguration>()
                .GetValue<string>("NETLIFY_AUTH_TOKEN");

            if (!string.IsNullOrWhiteSpace(authTokenEnvVar))
            {
                options.Auth = authTokenEnvVar;
            }

            // If an auth token is provided, no need to perform `ntl login`
            if (!string.IsNullOrWhiteSpace(options.Auth))
            {
                logger.LogInformation("Using provided Netlify auth token, skipping login step.");

                return;
            }

            var step = await reporter.CreateStepAsync("🔐 Authenticating with Netlify CLI", cancellationToken);

            await using (step.ConfigureAwait(false))
            {

                try
                {
                    var task = await step.CreateTaskAsync("ntl login", cancellationToken);

                    BufferedCommandResult? result;

                    try
                    {
                        result = await CliWrapper.Wrap("ntl")
                            .WithArguments("login")
                            .ExecuteBufferedAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        await task.FailAsync(
                            $"☹️ ntl login failed with error: {ex.Message}", cancellationToken);

                        throw;
                    }

                    if (result.IsSuccess)
                    {
                        loggedIn = true;

                        await task.CompleteAsync(
                            "🔓 Success: ntl login", cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await task.FailAsync(
                            $"☹️ ntl login exited with code: {result.ExitCode}",
                            cancellationToken: cancellationToken);
                    }
                }
                catch
                {
                    await step.FailAsync(
                        "🔒 Netlify CLI authentication check failed", cancellationToken: cancellationToken);

                    throw;
                }

                await step.CompleteAsync(
                    "✅ Completed Netlify CLI authentication check", cancellationToken: cancellationToken);
            }
        }
    }

    private static async Task FindSiteIdAsync(
        DeployingContext context,
        NetlifyDeploymentResource[] deployments)
    {
        foreach (var deployment in deployments)
        {
            var logger = context.Logger;
            var options = deployment.Options;
            var cancellationToken = context.CancellationToken;
            var stateManager = context.Services.GetRequiredService<IDeploymentStateManager>();
            var deployState = await stateManager.LoadStateAsync(cancellationToken);

            // Try loading site ID from deploy state file first.
            if (deployState is not null &&
                deployState.TryGetValue(deployment.Name, out var siteIdFromState, out var ex) &&
                siteIdFromState is not null)
            {
                options.Site = siteIdFromState.ToString();
                return;
            }

            if (options.Site is null && options.CreateSite is not null)
            {
                // Whenever we see, the --create-site option, we also want --json
                // We will read the site ID from the JSON output
                options.Json = true;
                return;
            }

            if (options.Site is null)
            {
                var state = await ReadNetlifyStateAsync(deployment.WorkingDirectory, logger, cancellationToken);
                if (state is not null && !string.IsNullOrWhiteSpace(state.SiteId))
                {
                    options.Site = state.SiteId;
                    return;
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
                    return;
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

    private async Task CallNetlifyDeployAsync(
        string ntlPath,
        NetlifyDeploymentResource[] deployments,
        DeployingContext context)
    {
        foreach (var deployment in deployments)
        {
            var options = deployment.Options;
            var reporter = context.ActivityReporter;
            var cancellationToken = context.CancellationToken;

            var deployStep = await reporter.CreateStepAsync(
                $"Deploying to Netlify ({options.ToEnvironmentDescription()})", cancellationToken);

            await using (deployStep.ConfigureAwait(false))
            {
                var resolvedWorkingDir = ResolveFullPath(deployment.WorkingDirectory);
                var resolvedBuildDir = ResolveFullPath(deployment.BuildDirectory, resolvedWorkingDir);

                var cliArgs = options.ToArguments(resolvedBuildDir);

                await ExecuteNetlifyDeploymentAsync(
                    ntlPath, cliArgs, resolvedWorkingDir, deployStep, deployment, context);
            }
        }
    }

    private static async Task ExecuteNetlifyDeploymentAsync(
        string ntlPath,
        CliArgs cliArgs,
        string workingDirectory,
        IPublishingStep step,
        NetlifyDeploymentResource deployment,
        DeployingContext context)
    {
        var (rawArgs, redecatedArgs) = cliArgs;
        var logger = context.Logger;

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
                                var message = (siteInfo.DeployUrl, siteInfo.Logs) switch
                                {
                                    (not null, not null) => $"Deployed successfully:\n      URL: {siteInfo.DeployUrl}\n      Logs: {siteInfo.Logs}",
                                    (not null, null) => $"Deployed successfully: {siteInfo.DeployUrl}",
                                    (null, not null) => $"Deployed successfully (logs: {siteInfo.Logs})",
                                    _ => "Deployed successfully"
                                };

                                await task.SucceedAsync(message, cancellationToken: context.CancellationToken);

                                var stateManager = context.Services.GetRequiredService<IDeploymentStateManager>();
                                var state = await stateManager.LoadStateAsync(context.CancellationToken);
                                if (state is not null && siteInfo.SiteId is not null)
                                {
                                    // Upsert property named after the deployment with the SiteId
                                    state[deployment.Name] = siteInfo.SiteId;

                                    // Persist updated state
                                    await stateManager.SaveStateAsync(state, context.CancellationToken);
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

                        var message = (draftDeployUrl, deployLogsUrl) switch
                        {
                            (not null, not null) => $"Deployed successfully:\n      URL: {draftDeployUrl}\n      Logs: {deployLogsUrl}",
                            (not null, null) => $"Deployed successfully: {draftDeployUrl}",
                            (null, not null) => $"Deployed successfully (logs: {deployLogsUrl})",
                            _ => "Deployed successfully"
                        };

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

    [GeneratedRegex(@"https?://[A-Za-z0-9-]+(?:--[A-Za-z0-9-]+)?\.netlify\.app\b", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex NetlifyDraftDeployUrlRegex();

    [GeneratedRegex(@"https?://[^\s|]*?/deploys/[A-Za-z0-9]+[^\s|]*", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex NetlifyDeployLogsUrlRegex();
}