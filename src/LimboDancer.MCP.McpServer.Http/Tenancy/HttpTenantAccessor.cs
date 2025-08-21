using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using LimboDancer.MCP.Core.Tenancy;

namespace LimboDancer.MCP.McpServer.Http.Tenancy
{
    public sealed class HttpTenantAccessor : ITenantAccessor
    {
        private const string TenantClaimType = "tenant_id";
        private const string DeprecatedTenantClaimType = "tid"; // for transitional support; logs a warning if used
        private const string TenantHeaderName = "X-Tenant-Id";
        private const string HttpItemsKey = "__mcp_tenant_id";
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HttpTenantAccessor> _logger;

        public HttpTenantAccessor(
            IHttpContextAccessor httpContextAccessor,
            IHostEnvironment environment,
            IConfiguration configuration,
            ILogger<HttpTenantAccessor> logger)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool IsDevelopment => _environment.IsDevelopment();

        public string TenantId
        {
            get
            {
                var httpContext = _httpContextAccessor.HttpContext
                    ?? throw new InvalidOperationException("No active HTTP context is available to resolve the tenant.");

                if (httpContext.Items.TryGetValue(HttpItemsKey, out var cached) && cached is string cachedTenant && !string.IsNullOrWhiteSpace(cachedTenant))
                {
                    return cachedTenant;
                }

                // 1) Primary: claim "tenant_id" (standardized)
                var user = httpContext.User;
                var tenantFromClaim = user?.FindFirstValue(TenantClaimType);
                if (!string.IsNullOrWhiteSpace(tenantFromClaim))
                {
                    tenantFromClaim = tenantFromClaim!.Trim();
                    httpContext.Items[HttpItemsKey] = tenantFromClaim;
                    _logger.LogDebug("Resolved tenant from claim '{ClaimType}'.", TenantClaimType);
                    return tenantFromClaim;
                }

                // Transitional support for "tid" with warning, to ease migration
                var deprecatedClaim = user?.FindFirstValue(DeprecatedTenantClaimType);
                if (!string.IsNullOrWhiteSpace(deprecatedClaim))
                {
                    deprecatedClaim = deprecatedClaim!.Trim();
                    httpContext.Items[HttpItemsKey] = deprecatedClaim;
                    _logger.LogWarning("Resolved tenant from deprecated claim '{ClaimType}'. Please migrate to '{Preferred}'.", DeprecatedTenantClaimType, TenantClaimType);
                    return deprecatedClaim;
                }

                // 2) Development-only fallbacks: header or config
                if (IsDevelopment)
                {
                    if (httpContext.Request.Headers.TryGetValue(TenantHeaderName, out var headerValues))
                    {
                        var tenantFromHeader = headerValues.FirstOrDefault()?.Trim();
                        if (!string.IsNullOrWhiteSpace(tenantFromHeader))
                        {
                            httpContext.Items[HttpItemsKey] = tenantFromHeader!;
                            _logger.LogWarning("Resolved tenant from '{Header}' header in Development.", TenantHeaderName);
                            return tenantFromHeader!;
                        }
                    }

                    var tenantFromConfig = _configuration["Tenancy:DefaultTenantId"]?.Trim();
                    if (!string.IsNullOrWhiteSpace(tenantFromConfig))
                    {
                        httpContext.Items[HttpItemsKey] = tenantFromConfig!;
                        _logger.LogWarning("Resolved tenant from configuration key 'Tenancy:DefaultTenantId' in Development.");
                        return tenantFromConfig!;
                    }
                }

                _logger.LogError("Unable to resolve tenant. No '{ClaimType}' claim and Development-only fallbacks did not yield a tenant.", TenantClaimType);
                throw new InvalidOperationException("Tenant resolution failed. A valid tenant_id claim is required.");
            }
        }
    }

    internal static class ClaimsPrincipalExtensions
    {
        public static string? FindFirstValue(this ClaimsPrincipal? principal, string claimType)
            => principal?.FindFirst(claimType)?.Value;
    }
}