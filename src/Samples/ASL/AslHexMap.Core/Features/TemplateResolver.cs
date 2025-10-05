using AslHexMap.Core.Schema;
using System;
using System.Collections.Generic;

namespace AslHexMap.Core.Features
{
    /// <summary>
    /// Resolves hex templates from template dictionaries and individual hex data.
    /// </summary>
    public class TemplateResolver
    {
        /// <summary>
        /// Resolves the template for a given hex from the available templates.
        /// </summary>
        /// <param name="hex">The individual hex to resolve template for</param>
        /// <param name="templates">Available templates dictionary</param>
        /// <returns>The resolved template, or null if not found</returns>
        public HexTemplate? ResolveTemplate(IndividualHex hex, Dictionary<string, HexTemplate> templates)
        {
            if (hex is null || templates is null)
                return null;

            if (string.IsNullOrWhiteSpace(hex.TemplateId))
                return null;

            templates.TryGetValue(hex.TemplateId!, out var template);
            return template;
        }

        /// <summary>
        /// Extracts building specification from a resolved template.
        /// </summary>
        /// <param name="template">The template to extract building spec from</param>
        /// <returns>Building specification or null if not present</returns>
        public BuildingSpec? ExtractBuildingSpec(HexTemplate? template)
        {
            return template?.Building;
        }
    }
}