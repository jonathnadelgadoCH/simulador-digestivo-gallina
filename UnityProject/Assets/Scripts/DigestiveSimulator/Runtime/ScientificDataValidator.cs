using System;
using System.Collections.Generic;
using System.Linq;
using DigestiveSimulator.Data;

namespace DigestiveSimulator.Runtime
{
    public static class ScientificDataValidator
    {
        public const string SupportedSchemaVersion = "1.0";

        public static void ValidateSpecies(SpeciesDefinition species, IEnumerable<OrganDefinition> sourceOrgans, DigestionGraph graph)
        {
            ValidateVersion(species.schemaVersion, $"species '{species.id}'");
            ValidateApproval(species.scientificStatus, species.referenceIds, $"species '{species.id}'");
            if (graph == null || !string.Equals(graph.speciesId, species.id, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Digestive graph does not belong to the species.");

            var organs = sourceOrgans?.ToArray() ?? Array.Empty<OrganDefinition>();
            var ids = new HashSet<string>(organs.Select(x => x.id), StringComparer.OrdinalIgnoreCase);
            foreach (var declared in species.digestiveSystem?.organs ?? Array.Empty<string>())
                if (!ids.Contains(declared)) throw new InvalidOperationException($"Declared organ '{declared}' has no organ definition.");

            foreach (var organ in organs)
            {
                ValidateVersion(organ.schemaVersion, $"organ '{organ.id}'");
                if (!string.Equals(organ.speciesId, species.id, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException($"Organ '{organ.id}' belongs to another species.");
                ValidateApproval(organ.scientificStatus, organ.referenceIds, $"organ '{organ.id}'");
            }
            DigestionGraphValidator.Validate(graph, ids);
        }

        public static void ValidateProfile(SpeciesFoodProfile profile, SpeciesDefinition species, FoodDefinition food)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            ValidateVersion(profile.schemaVersion, "species-food profile");
            if (!string.Equals(profile.speciesId, species?.id, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Profile species mismatch.");
            if (!string.Equals(profile.foodId, food?.id, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Profile food mismatch.");
            ValidateApproval(profile.scientificStatus, profile.referenceIds, $"profile '{profile.speciesId}+{profile.foodId}'");
        }

        public static void ValidateVersion(string version, string context)
        {
            if (version != SupportedSchemaVersion) throw new NotSupportedException($"Unsupported schema version '{version}' in {context}.");
        }

        public static void ValidateApproval(string status, string[] referenceIds, string context)
        {
            if (string.Equals(status, "approved", StringComparison.OrdinalIgnoreCase) && (referenceIds == null || referenceIds.Length == 0))
                throw new InvalidOperationException($"Approved {context} must cite at least one reference.");
        }
    }
}

