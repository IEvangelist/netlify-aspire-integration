namespace Aspire.Hosting;

internal static class CiDetector
{
    // A broad list of CI / pipeline environment variable names gathered across systems.
    // Note: This list is non-exhaustive, and should be extended when new systems are adopted.
    private static readonly string[] DefaultKnownCiVariables =
    [
        "CI",
        "CONTINUOUS_INTEGRATION",
        "BUILD_NUMBER",
        "RUN_ID",
        "RUN_NUMBER",
        "GITHUB_ACTIONS",
        "GITHUB_RUN_ID",
        "GITHUB_RUN_NUMBER",
        "GITLAB_CI",
        "GITLAB_CI_PIPELINE_ID",
        "GITLAB_CI_PIPELINE_IID",
        "TF_BUILD",
        "AZP",                 // Azure Pipelines short form prefix
        "SYSTEM_TEAMPROJECT",  // Azure DevOps
        "SYSTEM_COLLECTIONURI",
        "AGENT_ID",            // Azure DevOps agent
        "BUILD_REASON",
        "JENKINS_URL",
        "JOB_NAME",
        "BUILD_TAG",
        "BUILD_ID",
        "BUILD_NUMBER",
        "BUILD_URL",
        "TEAMCITY_VERSION",
        "BAMBOO_BUILDKEY",
        "BITBUCKET_BUILD_NUMBER",
        "BITBUCKET_COMMIT",
        "BITRISE_BUILD_SLUG",
        "CIRCLECI",
        "DRONE",               // Drone CI
        "DRONE_BUILD_NUMBER",
        "DRONE_BUILD_ID",
        "TRAVIS",              // Travis CI
        "TRAVIS_BUILD_ID",
        "TRAVIS_PULL_REQUEST",
        "HEROKU_TEST_RUN_ID",
        "WERCKER",             // Wercker CI
        "WERCKER_GIT_BRANCH",
        "APPVEYOR",
        "APPVEYOR_BUILD_ID",
        "APPVEYOR_REPO_NAME",
        "SEMAPHORE",           // Semaphore CI
        "SEMAPHORE_JOB_ID",
        "WOODPECKER",          // Woodpecker CI (fork of Drone) :contentReference[oaicite:0]{index=0}
        "CI_SYSTEM_NAME",
        "CI_WORKFLOW",
        "CI_PIPELINE_ID",
        "CI_JOB_ID",
        "CI_JOB_NAME",
        "CI_SERVER_URL",
        "CI_SERVER_NAME"
    ];

    /// <summary>
    /// Determines if the current environment is likely a CI/CD pipeline,
    /// by checking a broad set of environment variables via IConfiguration.
    /// </summary>
    /// <param name="configuration">The configuration object (e.g. built with environment variable provider)</param>
    /// <param name="extraKeys">Optional additional environment‐variable keys your org wants to include</param>
    /// <returns>True if looks like running in CI, false otherwise</returns>
    public static bool IsRunningInCi(IConfiguration configuration, IEnumerable<string>? extraKeys = null)
    {
        // Merge default + extras
        var keysToCheck = DefaultKnownCiVariables;

        if (extraKeys is not null)
        {
            // combine into a new list
            var list = new List<string>(DefaultKnownCiVariables.Length + 10);

            list.AddRange(DefaultKnownCiVariables);
            list.AddRange(extraKeys);

            keysToCheck = [.. list];
        }

        foreach (var key in keysToCheck)
        {
            // If configuration provider(s) include environment variables, this resolves to the env var value
            var value = configuration[key];

            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            // Normalize for check (trim, ToLower)
            var norm = value.Trim().ToLowerInvariant();

            // If explicitly false or "0", skip
            if (norm is "false" or "0")
                continue;

            // If the key is one of the "boolean" style ones, accept "true"/"1"
            if (norm is "true" or "1")
                return true;

            // Otherwise, presence of the variable with non-empty, non-false value is enough
            return true;
        }

        return false;
    }
}