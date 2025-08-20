using LimboDancer.MCP.Core.Tenancy;
using LimboDancer.MCP.Vector.AzureSearch;

namespace LimboDancer.MCP.McpServer.Vector;

/// <summary>
/// Helper/facade to ensure hybrid search queries are always tenant-filtered.
/// </summary>
public sealed class VectorSearchService
{
    private readonly VectorStore _store;
    private readonly ITenantAccessor _tenant;

    public VectorSearchService(VectorStore store, ITenantAccessor tenant)
    {
        _store = store;
        _tenant = tenant;
    }

    public Task<IReadOnlyList<VectorStore.SearchHit>> SearchAsync(
        string? queryText,
        float[]? vector,
        int k = 5,
        string? extraFilterOData = null,
        CancellationToken ct = default)
    {
        var tenantFilter = $"tenantId eq '{_tenant.TenantId:D}'";
        var filter = string.IsNullOrWhiteSpace(extraFilterOData)
            ? tenantFilter
            : $"{tenantFilter} and ({extraFilterOData})";

        return _store.SearchHybridAsync(queryText, vector, k, filter, ct);
    }
}