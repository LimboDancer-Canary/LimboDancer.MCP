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

        public string TenantId
        {
            get
            {
                var httpContext = _httpContextAccessor.HttpContext
                    ?? throw new InvalidOperationException("No active HTTP context is available to resolve the tenant.");

                // Check cached value first
                if (httpContext.Items.TryGetValue(HttpItemsKey, out var cached) &&
                    cached is Guid cachedGuid)
                {
                    return cachedGuid.ToString();
                }

                try
                {
                    // Try to get from claims
                    var user = httpContext.User;
                    if (user?.Identity?.IsAuthenticated == true)
                    {
                        var tenantFromClaim = user.FindFirstValue(TenantClaimType);
                        if (TryParseAndCacheTenant(tenantFromClaim, httpContext, "claim", TenantClaimType, out var guid))
                            return guid.ToString();

                        var deprecatedClaim = user.FindFirstValue(DeprecatedTenantClaimType);
                        if (TryParseAndCacheTenant(deprecatedClaim, httpContext, "deprecated claim", DeprecatedTenantClaimType, out guid))
                        {
                            _logger.LogWarning("Resolved tenant from deprecated claim '{ClaimType}'. Preferred: '{Preferred}'.",
                                DeprecatedTenantClaimType, TenantClaimType);
                            return guid.ToString();
                        }
                    }

                    // Development-only fallbacks
                    if (IsDevelopment)
                    {
                        // Try header
                        if (httpContext.Request?.Headers != null &&
                            httpContext.Request.Headers.TryGetValue(TenantHeaders.TenantId, out var headerValues))
                        {
                            var tenantFromHeader = headerValues.FirstOrDefault()?.Trim();
                            if (TryParseAndCacheTenant(tenantFromHeader, httpContext, "header", TenantHeaders.TenantId, out var guid))
                            {
                                _logger.LogWarning("Resolved tenant from header '{Header}' in Development.", TenantHeaders.TenantId);
                                return guid.ToString();
                            }
                        }

                        // Try configuration
                        var tenantFromConfig = _configuration["Tenancy:DefaultTenantId"]?.Trim();
                        if (TryParseAndCacheTenant(tenantFromConfig, httpContext, "configuration", "Tenancy:DefaultTenantId", out var guid2))
                        {
                            _logger.LogWarning("Resolved tenant from configuration key 'Tenancy:DefaultTenantId' in Development.");
                            return guid2.ToString();
                        }
                    }

                    _logger.LogError("Unable to resolve TenantId from any source. User authenticated: {IsAuthenticated}",
                        user?.Identity?.IsAuthenticated ?? false);
                    throw new InvalidOperationException("Unable to resolve TenantId from claims, header, or configuration.");
                }
                catch (Exception ex) when (ex is not InvalidOperationException)
                {
                    _logger.LogError(ex, "Unexpected error resolving TenantId");
                    throw new InvalidOperationException("Failed to resolve TenantId due to an unexpected error.", ex);
                }
            }
        }

        private bool TryParseAndCacheTenant(string? value, HttpContext context, string source, string sourceName, out Guid tenantId)
        {
            tenantId = Guid.Empty;

            if (string.IsNullOrWhiteSpace(value))
            {
                _logger.LogDebug("No tenant value found in {Source} '{SourceName}'", source, sourceName);
                return false;
            }

            if (!Guid.TryParse(value, out tenantId))
            {
                _logger.LogWarning("Invalid tenant ID format '{Value}' from {Source} '{SourceName}'",
                    value, source, sourceName);
                return false;
            }

            if (tenantId == Guid.Empty)
            {
                _logger.LogWarning("Empty GUID tenant ID from {Source} '{SourceName}'", source, sourceName);
                return false;
            }

            // Cache the valid tenant ID
            context.Items[HttpItemsKey] = tenantId;
            _logger.LogDebug("Resolved tenant {TenantId} from {Source} '{SourceName}'",
                tenantId, source, sourceName);
            return true;
        }
    }
}