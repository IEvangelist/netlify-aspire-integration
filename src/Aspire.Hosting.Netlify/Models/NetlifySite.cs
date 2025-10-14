namespace Aspire.Hosting;

/// <summary>
/// Represents a Netlify site with its details.
/// </summary>
/// <param name="SiteId">The unique identifier for the site.</param>
/// <param name="SiteName">The name of the site.</param>
/// <param name="SiteUrl">The URL of the site.</param>
/// <param name="SiteAdminUrl">The admin URL of the site.</param>
/// <param name="DeployId">The unique identifier for the deployment.</param>
/// <param name="DeployUrl">The URL of the deployment.</param>
/// <param name="Logs">The URL of the deployment logs.</param>
/// <param name="DeploySslUrl">The SSL URL of the deployment.</param>
/// <param name="DeployTime">The time taken for the deployment.</param>
/// <param name="AdminUrl">The admin URL of the site.</param>
/// <param name="DeployMessage">The message associated with the deployment.</param>
/// <param name="Skipped">Indicates if the deployment was skipped.</param>
/// <param name="ManualDeploy">Indicates if the deployment was manual.</param>
/// <param name="Branch">The branch associated with the deployment.</param>
/// <param name="Framework">The framework used for the site.</param>
/// <param name="SiteCapabilities">The capabilities of the site.</param>
public sealed record class NetlifySite(
    string SiteId,
    string SiteName,
    string SiteUrl,
    string SiteAdminUrl,
    string DeployId,
    string DeployUrl,
    string Logs,
    string DeploySslUrl,
    int DeployTime,
    string AdminUrl,
    string? DeployMessage,
    bool Skipped,
    bool ManualDeploy,
    string Branch,
    string? Framework,
    NetlifySiteCapabilities SiteCapabilities
);

/// <summary>
/// Represents the capabilities of a Netlify site.
/// </summary>
/// <param name="AssetAcceleration">Indicates if asset acceleration is enabled.</param>
/// <param name="FormProcessing">Indicates if form processing is enabled.</param>
/// <param name="CdnPropagation">The CDN propagation status.</param>
/// <param name="DomainAliases">Indicates if domain aliases are supported.</param>
/// <param name="SecureSite">Indicates if the site is secure.</param>
/// <param name="Proxying">Indicates if proxying is enabled.</param>
/// <param name="Ssl">The SSL status of the site.</param>
/// <param name="RateCents">The rate in cents for the site.</param>
/// <param name="Ipv6Domain">Indicates if the site has an IPv6 domain.</param>
/// <param name="BranchDeploy">Indicates if branch deploys are enabled.</param>
/// <param name="CdnTier">The CDN tier for the site.</param>
public sealed record class NetlifySiteCapabilities(
    bool AssetAcceleration,
    bool FormProcessing,
    string CdnPropagation,
    bool DomainAliases,
    bool SecureSite,
    bool Proxying,
    string Ssl,
    int RateCents,
    string Ipv6Domain,
    bool BranchDeploy,
    string CdnTier
);