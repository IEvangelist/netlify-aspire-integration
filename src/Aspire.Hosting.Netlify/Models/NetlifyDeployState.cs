namespace Aspire.Hosting;

/// <summary>
/// Represents a Netlify site with its unique site ID.
/// </summary>
/// <param name="SiteId"></param>
internal record class NetlifyDeployState(string SiteId);