using LimboDancer.MCP.Core.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace LimboDancer.MCP.McpServer.Tenancy;

/// <summary>
/// Resolves the current TenantId (and optional package/channel hints) from HTTP headers or defaults.
/// Headers:
///   x-tenant-id: guid
///   x-tenant-package: string
///   x-tenant-channel: string
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

            if (ctx.Request.Headers.TryGetValue("x-tenant-id", out var v) &&
                Guid.TryParse(v.ToString(), out var g))
                return g.ToString();

            return _opts.DefaultTenantId.ToString();
        }
    }

    public bool IsDevelopment => false; // TODO: Add IHostEnvironment dependency if needed

    // These may not be part of the ITenantAccessor interface; exposed via explicit getters for convenience if present.
    public string? Package
    {
        get
        {
            var ctx = _http.HttpContext;
            if (ctx?.Request?.Headers is null) return _opts.DefaultPackage;
            return ctx.Request.Headers.TryGetValue("x-tenant-package", out var v) ? v.ToString() : _opts.DefaultPackage;
        }
    }

    public string? Channel
    {
        get
        {
            var ctx = _http.HttpContext;
            if (ctx?.Request?.Headers is null) return _opts.DefaultChannel;
            return ctx.Request.Headers.TryGetValue("x-tenant-channel", out var v) ? v.ToString() : _opts.DefaultChannel;
        }
    }
}