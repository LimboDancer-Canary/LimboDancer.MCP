using System;
using System.Collections.Generic;
using System.Linq;
using LimboDancer.MCP.Ontology.Runtime;
using LimboDancer.MCP.Ontology.Store;

namespace LimboDancer.MCP.Ontology.Validation
{
    /// <summary>
    /// Lightweight validators for ontology artifacts. Intended for compile-time/model-time checks,
    /// not instance data validation.
    /// </summary>
    public static class OntologyValidators
    {
        public static IReadOnlyList<string> ValidateEntity(TenantScope scope, EntityDef entity, OntologyStore store)
        {
            var errors = new List<string>();
            scope.EnsureSame(entity.Scope);

            if (string.IsNullOrWhiteSpace(entity.LocalName))
                errors.Add("Entity.LocalName is required.");

            foreach (var p in entity.Parents)
            {
                if (store.GetEntity(p) is null)
                    errors.Add($"Entity '{entity.LocalName}' references missing parent '{p}'.");
            }

            return errors;
        }

        public static IReadOnlyList<string> ValidateProperty(TenantScope scope, PropertyDef prop, OntologyStore store)
        {
            var errors = new List<string>();
            scope.EnsureSame(prop.Scope);

            if (store.GetEntity(prop.OwnerEntity) is null)
                errors.Add($"Property '{prop.LocalName}' owner entity '{prop.OwnerEntity}' not found.");

            if (prop.MinCardinality < 0) errors.Add("Property.MinCardinality cannot be negative.");
            if (prop.MaxCardinality is < 0) errors.Add("Property.MaxCardinality cannot be negative.");
            if (prop.MaxCardinality is not null && prop.MinCardinality > prop.MaxCardinality)
                errors.Add("Property.MinCardinality > MaxCardinality.");

            if (prop.Range.Kind == RangeKind.EntityRef && store.GetEntity(prop.Range.Value) is null)
                errors.Add($"Property '{prop.LocalName}' range entity '{prop.Range.Value}' not found.");

            return errors;
        }

        public static IReadOnlyList<string> ValidateRelation(TenantScope scope, RelationDef rel, OntologyStore store)
        {
            var errors = new List<string>();
            scope.EnsureSame(rel.Scope);

            if (store.GetEntity(rel.FromEntity) is null)
                errors.Add($"Relation '{rel.LocalName}' from entity '{rel.FromEntity}' not found.");

            if (store.GetEntity(rel.ToEntity) is null)
                errors.Add($"Relation '{rel.LocalName}' to entity '{rel.ToEntity}' not found.");

            if (rel.MinCardinality < 0) errors.Add("Relation.MinCardinality cannot be negative.");
            if (rel.MaxCardinality is < 0) errors.Add("Relation.MaxCardinality cannot be negative.");
            if (rel.MaxCardinality is not null && rel.MinCardinality > rel.MaxCardinality)
                errors.Add("Relation.MinCardinality > MaxCardinality.");

            return errors;
        }

        public static IReadOnlyList<string> ValidateEnum(TenantScope scope, EnumDef en)
        {
            scope.EnsureSame(en.Scope);
            if (string.IsNullOrWhiteSpace(en.LocalName)) return new[] { "Enum.LocalName is required." };
            if (en.Values.Count == 0) return new[] { $"Enum '{en.LocalName}' has no values." };
            if (en.Values.Distinct(StringComparer.Ordinal).Count() != en.Values.Count)
                return new[] { $"Enum '{en.LocalName}' has duplicate values." };
            return Array.Empty<string>();
        }

        public static IReadOnlyList<string> ValidateShape(TenantScope scope, ShapeDef shape, OntologyStore store)
        {
            var errors = new List<string>();
            scope.EnsureSame(shape.Scope);

            if (store.GetEntity(shape.AppliesToEntity) is null)
                errors.Add($"Shape applies to missing entity '{shape.AppliesToEntity}'.");

            foreach (var pc in shape.PropertyConstraints)
            {
                var prop = store.GetProperty(shape.AppliesToEntity, pc.Property);
                if (prop is null)
                {
                    errors.Add($"Shape references missing property '{pc.Property}' on entity '{shape.AppliesToEntity}'.");
                    continue;
                }

                if (pc.MinCardinality < 0) errors.Add($"Shape property '{pc.Property}' MinCardinality cannot be negative.");
                if (pc.MaxCardinality is < 0) errors.Add($"Shape property '{pc.Property}' MaxCardinality cannot be negative.");
                if (pc.MaxCardinality is not null && pc.MinCardinality > pc.MaxCardinality)
                    errors.Add($"Shape property '{pc.Property}' MinCardinality > MaxCardinality.");
            }

            return errors;
        }
    }
}