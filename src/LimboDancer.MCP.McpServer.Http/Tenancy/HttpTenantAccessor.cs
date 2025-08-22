using System;
using System.Linq;
using System.Security.Claims;
using LimboDancer.MCP.Core.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LimboDancer.MCP.McpServer.Http.Tenancy
{
    public sealed class HttpTenantAccessor : ITenantAccessor
    {
        private const string TenantClaimType = "tenant_id";
        private const string DeprecatedTenantClaimType = "tid";
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

        public Guid TenantId
        {
            get
            {
                var httpContext = _httpContextAccessor.HttpContext
                    ?? throw new InvalidOperationException("No active HTTP context is available to resolve the tenant.");

                if (httpContext.Items.TryGetValue(HttpItemsKey, out var cached) &&
                    cached is Guid cachedGuid)
                {
                    return cachedGuid;
                }

                var user = httpContext.User;
                var tenantFromClaim = user?.FindFirstValue(TenantClaimType);
                if (!string.IsNullOrWhiteSpace(tenantFromClaim) && Guid.TryParse(tenantFromClaim, out var guid))
                {
                    httpContext.Items[HttpItemsKey] = guid;
                    _logger.LogDebug("Resolved tenant from claim '{ClaimType}'.", TenantClaimType);
                    return guid;
                }

                var deprecatedClaim = user?.FindFirstValue(DeprecatedTenantClaimType);
                if (!string.IsNullOrWhiteSpace(deprecatedClaim) && Guid.TryParse(deprecatedClaim, out var deprecatedGuid))
                {
                    httpContext.Items[HttpItemsKey] = deprecatedGuid;
                    _logger.LogWarning("Resolved tenant from deprecated claim '{ClaimType}'. Preferred: '{Preferred}'.",
                        DeprecatedTenantClaimType, TenantClaimType);
                    return deprecatedGuid;
                }

                if (IsDevelopment)
                {
                    if (httpContext.Request.Headers.TryGetValue(TenantHeaders.TenantId, out var headerValues))
                    {
                        var tenantFromHeader = headerValues.FirstOrDefault()?.Trim();
                        if (!string.IsNullOrWhiteSpace(tenantFromHeader) && Guid.TryParse(tenantFromHeader, out var headerGuid))
                        {
                            httpContext.Items[HttpItemsKey] = headerGuid;
                            _logger.LogWarning("Resolved tenant from header '{Header}' in Development.", TenantHeaders.TenantId);
                            return headerGuid;
                        }
                    }

                    var tenantFromConfig = _configuration["Tenancy:DefaultTenantId"]?.Trim();
                    if (!string.IsNullOrWhiteSpace(tenantFromConfig) && Guid.TryParse(tenantFromConfig, out var configGuid))
                    {
                        httpContext.Items[HttpItemsKey] = configGuid;
                        _logger.LogWarning("Resolved tenant from configuration key 'Tenancy:DefaultTenantId' in Development.");
                        return configGuid;
                    }
                }

                throw new InvalidOperationException("Unable to resolve TenantId from claims, header, or configuration.");
            }
        }
    }
}