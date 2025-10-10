namespace Aspire.Hosting;

/// <summary>
/// Options for deploying to Netlify using the Netlify CLI.
/// For more information, see <see href="https://cli.netlify.com/commands/deploy/">Netlify CLI deploy command documentation</see>.
/// </summary>
public sealed class NetlifyDeployOptions
{
    /// <summary>
    /// Specifies the alias for deployment, the string at the beginning of the deploy subdomain.
    /// Useful for creating predictable deployment URLs. Avoid setting an alias string to the same value as a deployed branch.
    /// Alias doesn't create a branch deploy and can't be used in conjunction with the branch subdomain feature.
    /// Maximum 37 characters.
    /// </summary>
    public string? Alias { get; set; }

    /// <summary>
    /// Specify a deploy context for environment variables read during the build
    /// ("production", "deploy-preview", "branch-deploy", "dev") or "branch:your-branch" where "your-branch" is the name of a branch.
    /// Default: dev
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// Create a new site and deploy to it. Optionally specify a name, otherwise a random name will be generated.
    /// Requires <see cref="Team"/> flag if you have multiple teams.
    /// </summary>
    public string? CreateSite { get; set; }

    /// <summary>
    /// Specify a folder to deploy.
    /// </summary>
    public string? Dir { get; set; }

    /// <summary>
    /// For monorepos, specify the name of the application to run the command in.
    /// </summary>
    public string? Filter { get; set; }

    /// <summary>
    /// Specify a functions folder to deploy.
    /// </summary>
    public string? Functions { get; set; }

    /// <summary>
    /// Output deployment data as JSON.
    /// </summary>
    public bool? Json { get; set; }

    /// <summary>
    /// A short message to include in the deploy log.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Do not run build command before deploying. Only use this if you have no need for a build or your project has already been built.
    /// </summary>
    public bool? NoBuild { get; set; }

    /// <summary>
    /// Open project after deploy.
    /// </summary>
    public bool? Open { get; set; }

    /// <summary>
    /// Deploy to production if unlocked, create a draft otherwise.
    /// </summary>
    public bool? ProdIfUnlocked { get; set; }

    /// <summary>
    /// Print debugging information.
    /// </summary>
    public bool? Debug { get; set; }

    /// <summary>
    /// Netlify auth token - can be used to run this command without logging in.
    /// </summary>
    public string? Auth { get; set; }

    /// <summary>
    /// Deploy to production.
    /// </summary>
    public bool? Prod { get; set; }

    /// <summary>
    /// A project name or ID to deploy to.
    /// </summary>
    public string? Site { get; set; }

    /// <summary>
    /// Ignore any functions created as part of a previous build or deploy commands, forcing them to be bundled again as part of the deployment.
    /// </summary>
    public bool? SkipFunctionsCache { get; set; }

    /// <summary>
    /// Specify team slug when creating a site. Only works with <see cref="CreateSite"/> flag.
    /// </summary>
    public string? Team { get; set; }

    /// <summary>
    /// Timeout to wait for deployment to finish.
    /// </summary>
    public string? Timeout { get; set; }

    /// <summary>
    /// Trigger a new build of your project on Netlify without uploading local files.
    /// </summary>
    public bool? Trigger { get; set; }

    internal CliArgs ToArguments(string? resolvedBuildDir = null)
    {
        List<string> args = ["deploy"];
        List<string> redactedArgs = ["deploy"];

        // Add directory
        args.AddRange(["--dir", resolvedBuildDir ?? Dir ?? "."]);
        redactedArgs.AddRange(["--dir", resolvedBuildDir ?? Dir ?? "."]);

        // Add all option flags
        if (!string.IsNullOrEmpty(Alias))
        {
            args.AddRange(["--alias", Alias]);
            redactedArgs.AddRange(["--alias", Alias]);
        }

        if (!string.IsNullOrEmpty(Context))
        {
            args.AddRange(["--context", Context]);
            redactedArgs.AddRange(["--context", Context]);
        }

        if (!string.IsNullOrEmpty(CreateSite))
        {
            args.AddRange(["--create-site", CreateSite]);
            redactedArgs.AddRange(["--create-site", CreateSite]);
        }

        if (!string.IsNullOrEmpty(Filter))
        {
            args.AddRange(["--filter", Filter]);
            redactedArgs.AddRange(["--filter", Filter]);
        }

        if (!string.IsNullOrEmpty(Functions))
        {
            args.AddRange(["--functions", Functions]);
            redactedArgs.AddRange(["--functions", Functions]);
        }

        if (Json is true)
        {
            args.Add("--json");
            redactedArgs.Add("--json");
        }

        if (!string.IsNullOrEmpty(Message))
        {
            args.AddRange(["--message", Message]);
            redactedArgs.AddRange(["--message", Message]);
        }

        if (NoBuild is true)
        {
            args.Add("--no-build");
            redactedArgs.Add("--no-build");
        }

        if (Open is true)
        {
            args.Add("--open");
            redactedArgs.Add("--open");
        }

        if (ProdIfUnlocked is true)
        {
            args.Add("--prod-if-unlocked");
            redactedArgs.Add("--prod-if-unlocked");
        }

        if (Debug is true)
        {
            args.Add("--debug");
            redactedArgs.Add("--debug");
        }

        if (!string.IsNullOrEmpty(Auth))
        {
            args.AddRange(["--auth", Auth]);
            redactedArgs.AddRange(["--auth", Redact(Auth)]);
        }

        if (Prod is true)
        {
            args.Add("--prod");
            redactedArgs.Add("--prod");
        }

        if (!string.IsNullOrEmpty(Site))
        {
            args.AddRange(["--site", Site]);
            redactedArgs.AddRange(["--site", Redact(Site)]);
        }

        if (SkipFunctionsCache is true)
        {
            args.Add("--skip-functions-cache");
            redactedArgs.Add("--skip-functions-cache");
        }

        if (!string.IsNullOrEmpty(Team))
        {
            args.AddRange(["--team", Team]);
            redactedArgs.AddRange(["--team", Redact(Team)]);
        }

        if (!string.IsNullOrEmpty(Timeout))
        {
            args.AddRange(["--timeout", Timeout]);
            redactedArgs.AddRange(["--timeout", Timeout]);
        }

        if (Trigger is true)
        {
            args.Add("--trigger");
            redactedArgs.Add("--trigger");
        }

        return ([.. args], [.. redactedArgs]);

        static string Redact(string? value, char redactChar = '*', int maxLength = 5)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            // If the value is shorter than the max length, just redact the whole thing
            if (value.Length <= maxLength)
            {
                return new string(redactChar, maxLength);
            }

            // Otherwise, redact the middle of the string
            var start = value[..2];
            var end = value[^2..];

            return $"{start}{new string(redactChar, maxLength)}{end}";
        }
    }

    internal string ToEnvironmentDescription()
    {
        var environmentDescription = Prod is true
            ? "production"
            : ProdIfUnlocked is true
                ? "production-if-unlocked"
                : !string.IsNullOrEmpty(Alias)
                    ? $"alias: {Alias}"
                    : "preview";

        return environmentDescription;
    }
}
