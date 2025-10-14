namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents an npm script runner.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="workingDirectory">The working directory to use for the command.</param>
/// <param name="scriptName">The npm script name to run.</param>
public class NpmRunnerResource(string name, string workingDirectory, string scriptName)
    : ExecutableResource(name, "npm", workingDirectory)
{
    /// <summary>
    /// Gets the npm script name to run. For example, <c>"build"</c>.
    /// </summary>
    public string ScriptName { get; } = scriptName;
}