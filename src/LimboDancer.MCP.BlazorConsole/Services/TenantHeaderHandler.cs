using LimboDancer.MCP.Core.Tenancy;
using Microsoft.Extensions.Options;

namespace LimboDancer.MCP.BlazorConsole.Services;

/// <summary>
/// Delegating handler that ensures the tenant header is present on outgoing requests.
/// </summary>
public sealed class TenantHeaderHandler : DelegatingHandler
{
    private readonly TenantUiState _tenant;
    private readonly IOptions<OntologyApiOptions> _options;

    public TenantHeaderHandler(TenantUiState tenant, IOptions<OntologyApiOptions> options)
    {
        _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var headerName = _options.Value.TenantHeaderName ?? TenantHeaders.TenantId;
        var tenantId = _tenant.CurrentTenantId;

        if (!string.IsNullOrWhiteSpace(tenantId) &&
            !request.Headers.Contains(headerName))
        {
            request.Headers.TryAddWithoutValidation(headerName, tenantId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}