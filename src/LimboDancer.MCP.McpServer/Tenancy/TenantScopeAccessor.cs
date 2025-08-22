using LimboDancer.MCP.Core.Tenancy;
using LimboDancer.MCP.Ontology.Runtime;
using Microsoft.Extensions.Options;

namespace LimboDancer.MCP.McpServer.Tenancy;

/// <summary>
/// Builds a TenantScope from headers or defaults.
/// </summary>
public sealed class TenantScopeAccessor : ITenantScopeAccessor
{
    private readonly IHttpContextAccessor _http;
    private readonly ITenantAccessor _tenantAccessor;
    private readonly TenancyOptions _options;

    public TenantScopeAccessor(
        IHttpContextAccessor http,
        ITenantAccessor tenantAccessor,
        IOptions<TenancyOptions> options)
    {
        _http = http;
        _tenantAccessor = tenantAccessor;
        _options = options.Value;
    }

    public TenantScope GetCurrentScope()
    {
        var tenantId = _tenantAccessor.TenantId.ToString();
        var ctx = _http.HttpContext;
        var headers = ctx?.Request?.Headers;

        var package = headers is not null && headers.TryGetValue(TenantHeaders.Package, out var p)
            ? p.ToString()
            : _options.DefaultPackage;

        var channel = headers is not null && headers.TryGetValue(TenantHeaders.Channel, out var c)
            ? c.ToString()
            : _options.DefaultChannel;

        if (string.IsNullOrWhiteSpace(package)) package = _options.DefaultPackage;
        if (string.IsNullOrWhiteSpace(channel)) channel = _options.DefaultChannel;

        return new TenantScope(tenantId, package, channel);
    }
}