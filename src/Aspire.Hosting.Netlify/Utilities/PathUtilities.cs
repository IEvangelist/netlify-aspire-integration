namespace Aspire.Hosting;

internal static class PathUtilities
{
    private const string NetlifyFolderName = ".netlify";
    private const string NetlifyStateJsonFileName = "state.json";
    private const string NetlifyStateFilePath = $"{NetlifyFolderName}/{NetlifyStateJsonFileName}";

    internal static async Task<NetlifyDeployState?> ReadNetlifyStateAsync(
        string workingDirectory, ILogger logger, CancellationToken cancellationToken)
    {
        var siteIdFile = Path.Combine(workingDirectory, NetlifyStateFilePath);

        if (!File.Exists(siteIdFile))
        {
            return null;
        }

        var content = await File.ReadAllTextAsync(siteIdFile, cancellationToken);

        if (string.IsNullOrWhiteSpace(content))
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Netlify state file is empty: {FilePath}", siteIdFile);
            }

            return null;
        }

        return JsonSerializer.Deserialize(
            json: content,
            jsonTypeInfo: NetlifyWebJsonContext.Default.NetlifyDeployState);
    }

    internal static string ResolveOnPath(string name)
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
    internal static string ResolveFullPath(string path, string? baseDirectory = null)
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
}