// Copyright (c) David Pine. All rights reserved.
// Licensed under the MIT License.

namespace Aspire.Hosting;

internal static partial class NetlifyDeploymentPipelineSteps
{
    private static CommandOnPath s_commandOnPath = false;

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

                var ntlPath = ResolveFullPathFromEnvironment("ntl");

                if (string.Equals(ntlPath, "ntl", StringComparison.Ordinal))
                {
                    await resolveOnPathTask.WarnAsync(
                        "Netlify CLI not found on PATH", context.CancellationToken);
                }
                else
                {
                    await resolveOnPathTask.CompleteAsync(
                        $"Found at {ntlPath}", cancellationToken: context.CancellationToken);

                    s_commandOnPath = ntlPath;

                    return;
                }

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
                    await resolveOnPathTask.FailAsync(
                        $"Failed to execute Netlify CLI: {ex.Message}", context.CancellationToken);
                }

                var completionMessage = result is not null
                    ? result.IsSuccess ? result.StandardOutput : result.StandardError
                    : "Unknown error verifying Netlify CLI";

                await resolveOnPathTask.CompleteAsync(
                    completionMessage,
                    cancellationToken: context.CancellationToken);
            }
            catch
            {
                s_commandOnPath = false;
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
            if (s_commandOnPath.IsFound)
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
                else
                {
                    await task.CompleteAsync(
                        "Installation complete", cancellationToken: cancellationToken);
                }
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
                        result = await CliWrapper.Wrap("ntl")
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
                if (options.CreateSite is not null)
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
                $"{friendlyName} — {deployment.NodeAppResourceName}",
                cancellationToken);

            await using (deployStep.ConfigureAwait(false))
            {
                var cliArgs = options.ToArguments();
                var resolvedWorkingDir = ResolveFullPath(deployment.WorkingDirectory);

                try
                {
                    await ExecuteNetlifyDeploymentAsync(
                        s_commandOnPath.Path!, cliArgs, resolvedWorkingDir, deployStep, deployment, context);
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
                var nodeApp = deployment.NodeAppResource;

                // Get all NpmRunnerAnnotations for this node app
                var npmRunners = nodeApp.Annotations
                    .OfType<NpmCommandAnnotation>()
                    .Select(a => a.Resource)
                    .ToList();

                if (npmRunners.Count == 0)
                {
                    continue;
                }

                foreach (var runner in npmRunners)
                {
                    var args = await runner.GetArgumentValuesAsync();

                    var task = await step.CreateTaskAsync(
                        $"npm {string.Join(" ", args)} ({nodeApp.Name})",
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
        };        return message;
    }

    [GeneratedRegex(@"https?://[A-Za-z0-9-]+(?:--[A-Za-z0-9-]+)?\.netlify\.app\b", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex NetlifyDraftDeployUrlRegex();

    [GeneratedRegex(@"https?://[^\s|]*?/deploys/[A-Za-z0-9]+[^\s|]*", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex NetlifyDeployLogsUrlRegex();
}