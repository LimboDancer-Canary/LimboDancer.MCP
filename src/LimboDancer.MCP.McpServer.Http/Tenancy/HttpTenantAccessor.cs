using System.Security.Claims;
using LimboDancer.MCP.Core.Tenancy;
using Microsoft.AspNetCore.Http;

namespace LimboDancer.MCP.McpServer.Http.Tenancy;

public sealed class HttpTenantAccessor : ITenantAccessor
{
    private readonly IHttpContextAccessor _http;

    public HttpTenantAccessor(IHttpContextAccessor http) => _http = http;

    public Guid TenantId
    {
        get
        {
            var ctx = _http.HttpContext;
            if (ctx is null) return Guid.Empty;

            // 1) JWT claim (preferred). Adjust claim name to your token setup.
            var claim = ctx.User.FindFirst("tenant_id") ?? ctx.User.FindFirst(ClaimTypes.GroupSid);
            if (claim != null && Guid.TryParse(claim.Value, out var fromClaim))
                return fromClaim;

            // 2) Dev-only header fallback
            if (ctx.Request.Headers.TryGetValue("X-Tenant-Id", out var vals))
            {
                var s = vals.ToString();
                if (Guid.TryParse(s, out var fromHeader))
                    return fromHeader;
            }

            // 3) Final fallback (dev/local): appsettings default, else Guid.Empty
            var sCfg = ctx.RequestServices.GetService<IConfiguration>()?["DefaultTenantId"];
            if (!string.IsNullOrWhiteSpace(sCfg) && Guid.TryParse(sCfg, out var fromCfg))
                return fromCfg;

            return Guid.Empty; // will be rejected by guards
        }
    }
}