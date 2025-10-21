// Copyright (c) David Pine. All rights reserved.
// Licensed under the MIT License.

namespace Aspire.Hosting;

internal static class NetlifyDeployStepNames
{
    public const string CheckNetlifyCli = "netlify-check-cli";

    public const string InstallNetlifyCli = "netlify-install-cli";

    public const string AuthenticateNetlifyCli = "netlify-authenticate-cli";

    public const string RunNpmCommands = "netlify-run-npm-commands";

    public const string ResolveNetlifySiteId = "netlify-resolve-site-id";

    public const string DeployToNetlify = "netlify-deploy";

    public static string GetFriendlyName(string stepName) => stepName switch
    {
        CheckNetlifyCli => "🔍 Check for Netlify CLI",
        InstallNetlifyCli => "📦 Install Netlify CLI",
        AuthenticateNetlifyCli => "🔐 Authenticate with Netlify",
        RunNpmCommands => "📦 Run npm commands",
        ResolveNetlifySiteId => "✅ Resolve Netlify Site ID",
        DeployToNetlify => "🚀 Deploy to Netlify",

        _ => stepName
    };
}