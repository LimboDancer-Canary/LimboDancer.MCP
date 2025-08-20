using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LimboDancer.MCP.Ontology.Runtime;

namespace LimboDancer.MCP.Ontology.Repositories
{
    /// <summary>
    /// Authoritative persistence contract for ontology artifacts.
    /// All methods MUST be scoped with TenantScope, and must not allow cross-scope operations.
    /// </summary>
    public interface IOntologyRepository
    {
        // Entities
        Task UpsertEntitiesAsync(TenantScope scope, IEnumerable<EntityDef> entities, CancellationToken ct = default);
        Task<EntityDef?> GetEntityAsync(TenantScope scope, string localName, CancellationToken ct = default);
        Task<IReadOnlyList<EntityDef>> ListEntitiesAsync(TenantScope scope, CancellationToken ct = default);
        Task DeleteEntityAsync(TenantScope scope, string localName, CancellationToken ct = default);

        // Properties
        Task UpsertPropertiesAsync(TenantScope scope, IEnumerable<PropertyDef> properties, CancellationToken ct = default);
        Task<PropertyDef?> GetPropertyAsync(TenantScope scope, string ownerEntity, string localName, CancellationToken ct = default);
        Task<IReadOnlyList<PropertyDef>> ListPropertiesAsync(TenantScope scope, CancellationToken ct = default);
        Task DeletePropertyAsync(TenantScope scope, string ownerEntity, string localName, CancellationToken ct = default);

        // Relations
        Task UpsertRelationsAsync(TenantScope scope, IEnumerable<RelationDef> relations, CancellationToken ct = default);
        Task<RelationDef?> GetRelationAsync(TenantScope scope, string localName, CancellationToken ct = default);
        Task<IReadOnlyList<RelationDef>> ListRelationsAsync(TenantScope scope, CancellationToken ct = default);
        Task DeleteRelationAsync(TenantScope scope, string localName, CancellationToken ct = default);

        // Enums
        Task UpsertEnumsAsync(TenantScope scope, IEnumerable<EnumDef> enums, CancellationToken ct = default);
        Task<EnumDef?> GetEnumAsync(TenantScope scope, string localName, CancellationToken ct = default);
        Task<IReadOnlyList<EnumDef>> ListEnumsAsync(TenantScope scope, CancellationToken ct = default);
        Task DeleteEnumAsync(TenantScope scope, string localName, CancellationToken ct = default);

        // Aliases
        Task UpsertAliasesAsync(TenantScope scope, IEnumerable<AliasDef> aliases, CancellationToken ct = default);
        Task<IReadOnlyList<AliasDef>> ListAliasesAsync(TenantScope scope, CancellationToken ct = default);
        Task DeleteAliasAsync(TenantScope scope, string canonical, string? locale = null, CancellationToken ct = default);

        // Shapes
        Task UpsertShapesAsync(TenantScope scope, IEnumerable<ShapeDef> shapes, CancellationToken ct = default);
        Task<IReadOnlyList<ShapeDef>> ListShapesAsync(TenantScope scope, CancellationToken ct = default);
        Task DeleteShapeAsync(TenantScope scope, string appliesToEntity, CancellationToken ct = default);
    }
}