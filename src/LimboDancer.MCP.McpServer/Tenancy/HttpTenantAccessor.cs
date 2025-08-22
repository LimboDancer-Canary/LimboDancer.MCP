using LimboDancer.MCP.Core.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace LimboDancer.MCP.McpServer.Tenancy;

/// <summary>
/// Resolves the current TenantId (and optional package/channel hints) from HTTP headers or defaults.
/// Headers (case-insensitive):
///   X-Tenant-Id: guid
///   X-Tenant-Package: string
///   X-Tenant-Channel: string
/// </summary>
public sealed class HttpTenantAccessor : ITenantAccessor
{
    private readonly IHttpContextAccessor _http;
    private readonly TenancyOptions _opts;

    public HttpTenantAccessor(IHttpContextAccessor http, IOptions<TenancyOptions> opts)
    {
        _http = http;
        _opts = opts.Value;
    }

    public string TenantId
    {
        get
        {
            var ctx = _http.HttpContext;
            if (ctx?.Request?.Headers is null) return _opts.DefaultTenantId.ToString();

            if (ctx.Request.Headers.TryGetValue(TenantHeaders.TenantId, out var v) &&
                Guid.TryParse(v.ToString(), out var g))
                return g.ToString();

            return _opts.DefaultTenantId.ToString();
        }
    }

    public bool IsDevelopment => false; // unchanged

    public string? Package
    {
        get
        {
            var ctx = _http.HttpContext;
            if (ctx?.Request?.Headers is null) return _opts.DefaultPackage;
            return ctx.Request.Headers.TryGetValue(TenantHeaders.Package, out var v)
                ? v.ToString()
                : _opts.DefaultPackage;
        }
    }

    public string? Channel
    {
        get
        {
            var ctx = _http.HttpContext;
            if (ctx?.Request?.Headers is null) return _opts.DefaultChannel;
            return ctx.Request.Headers.TryGetValue(TenantHeaders.Channel, out var v)
                ? v.ToString()
                : _opts.DefaultChannel;
        }
    }
}