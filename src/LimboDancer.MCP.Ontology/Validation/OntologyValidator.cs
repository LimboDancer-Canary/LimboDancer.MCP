using LimboDancer.MCP.Ontology.Runtime;
using LimboDancer.MCP.Ontology.Store;

namespace LimboDancer.MCP.Ontology.Validation;

public static class OntologyValidator
{
    public static OntologyValidationResult Validate(OntologyStore store, TenantScope scope)
    {
        var errors = new List<string>();

        foreach (var entity in store.Entities())
            errors.AddRange(OntologyValidators.ValidateEntity(scope, entity, store));

        foreach (var prop in store.Properties())
            errors.AddRange(OntologyValidators.ValidateProperty(scope, prop, store));

        foreach (var rel in store.Relations())
            errors.AddRange(OntologyValidators.ValidateRelation(scope, rel, store));

        foreach (var en in store.Enums())
            errors.AddRange(OntologyValidators.ValidateEnum(scope, en));

        foreach (var shape in store.Shapes())
            errors.AddRange(OntologyValidators.ValidateShape(scope, shape, store));

        return new OntologyValidationResult { Errors = errors };
    }
}