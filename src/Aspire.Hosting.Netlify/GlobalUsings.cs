global using System.Runtime.InteropServices;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Text.RegularExpressions;
global using Aspire.Hosting.ApplicationModel;
global using Aspire.Hosting.Pipelines;
global using Aspire.Hosting.Publishing;
global using CliWrap.Buffered;
global using Json.More;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;

global using static Aspire.Hosting.PathUtilities;

global using CliArgs = (string[] Raw, string[] Redacted);
// Need an alias, both Aspire.Hosting.Cli and CliWrap.Cli exist
global using CliWrapper = CliWrap.Cli;
global using CommandOnPath = (bool IsFound, string? Path);
