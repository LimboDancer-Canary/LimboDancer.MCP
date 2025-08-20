using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LimboDancer.MCP.Ontology.Repositories;
using LimboDancer.MCP.Ontology.Runtime;

namespace LimboDancer.MCP.Ontology.Store
{
    /// <summary>
    /// Read-optimized in-memory store for a specific TenantScope.
    /// Call LoadAsync to populate from the repository and then use fast lookups.
    /// </summary>
    public sealed class OntologyStore
    {
        private readonly IOntologyRepository _repo;

        private readonly Dictionary<string, EntityDef> _entities = new(StringComparer.Ordinal);
        private readonly Dictionary<(string owner, string local), PropertyDef> _properties = new();
        private readonly Dictionary<string, RelationDef> _relations = new(StringComparer.Ordinal);
        private readonly Dictionary<string, EnumDef> _enums = new(StringComparer.Ordinal);
        private readonly List<AliasDef> _aliases = new();
        private readonly Dictionary<string, ShapeDef> _shapes = new(StringComparer.Ordinal);

        public TenantScope Scope { get; private set; }
        public bool IsLoaded { get; private set; }

        public OntologyStore(IOntologyRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            Scope = default;
        }

        public async Task LoadAsync(TenantScope scope, CancellationToken ct = default)
        {
            scope.EnsureComplete();
            Scope = scope;

            var entitiesTask = _repo.ListEntitiesAsync(scope, ct);
            var propsTask = _repo.ListPropertiesAsync(scope, ct);
            var relsTask = _repo.ListRelationsAsync(scope, ct);
            var enumsTask = _repo.ListEnumsAsync(scope, ct);
            var aliasesTask = _repo.ListAliasesAsync(scope, ct);
            var shapesTask = _repo.ListShapesAsync(scope, ct);

            await Task.WhenAll(entitiesTask, propsTask, relsTask, enumsTask, aliasesTask, shapesTask).ConfigureAwait(false);

            _entities.Clear();
            foreach (var e in entitiesTask.Result) _entities[e.LocalName] = e;

            _properties.Clear();
            foreach (var p in propsTask.Result) _properties[(p.OwnerEntity, p.LocalName)] = p;

            _relations.Clear();
            foreach (var r in relsTask.Result) _relations[r.LocalName] = r;

            _enums.Clear();
            foreach (var en in enumsTask.Result) _enums[en.LocalName] = en;

            _aliases.Clear();
            _aliases.AddRange(aliasesTask.Result);

            _shapes.Clear();
            foreach (var s in shapesTask.Result) _shapes[s.AppliesToEntity] = s;

            IsLoaded = true;
            ValidateReferentials();
        }

        private void ValidateReferentials()
        {
            if (!IsLoaded) return;

            // Ensure parents exist
            foreach (var e in _entities.Values)
            {
                foreach (var parent in e.Parents)
                {
                    if (!_entities.ContainsKey(parent))
                        throw new OntologyValidationException(Scope, $"Entity '{e.LocalName}' references missing parent '{parent}'.");
                }
            }

            // Ensure property owners and ranges exist
            foreach (var p in _properties.Values)
            {
                if (!_entities.ContainsKey(p.OwnerEntity))
                    throw new OntologyValidationException(Scope, $"Property '{p.LocalName}' has missing owner entity '{p.OwnerEntity}'.");

                if (p.Range.Kind == RangeKind.EntityRef && !_entities.ContainsKey(p.Range.Value))
                    throw new OntologyValidationException(Scope, $"Property '{p.LocalName}' range entity '{p.Range.Value}' not found.");
            }

            // Ensure relation endpoints exist
            foreach (var r in _relations.Values)
            {
                if (!_entities.ContainsKey(r.FromEntity))
                    throw new OntologyValidationException(Scope, $"Relation '{r.LocalName}' has missing from entity '{r.FromEntity}'.");
                if (!_entities.ContainsKey(r.ToEntity))
                    throw new OntologyValidationException(Scope, $"Relation '{r.LocalName}' has missing to entity '{r.ToEntity}'.");
            }
        }

        public EntityDef? GetEntity(string localName) => _entities.TryGetValue(localName, out var e) ? e : null;
        public IEnumerable<EntityDef> Entities() => _entities.Values;

        public PropertyDef? GetProperty(string ownerEntity, string localName)
            => _properties.TryGetValue((ownerEntity, localName), out var p) ? p : null;
        public IEnumerable<PropertyDef> Properties() => _properties.Values;

        public RelationDef? GetRelation(string localName) => _relations.TryGetValue(localName, out var r) ? r : null;
        public IEnumerable<RelationDef> Relations() => _relations.Values;

        public EnumDef? GetEnum(string localName) => _enums.TryGetValue(localName, out var e) ? e : null;
        public IEnumerable<EnumDef> Enums() => _enums.Values;

        public IEnumerable<AliasDef> Aliases() => _aliases;

        public ShapeDef? GetShape(string appliesToEntity) => _shapes.TryGetValue(appliesToEntity, out var s) ? s : null;
        public IEnumerable<ShapeDef> Shapes() => _shapes.Values;
    }
}