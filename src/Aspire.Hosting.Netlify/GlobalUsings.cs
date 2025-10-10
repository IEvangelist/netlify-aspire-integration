global using System.Runtime.InteropServices;
global using System.Text.RegularExpressions;
global using Aspire.Hosting.ApplicationModel;
global using Aspire.Hosting.Publishing;
global using CliWrap.Buffered;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;

// Need an alias, both Aspire.Hosting.Cli and CliWrap.Cli exist
global using CliWrapper = CliWrap.Cli;
global using CommandOnPath = (bool IsFound, string? Path);
global using CliArgs = (string[] Raw, string[] Redacted);