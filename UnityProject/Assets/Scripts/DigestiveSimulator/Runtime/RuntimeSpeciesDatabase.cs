using System;
using System.Collections.Generic;
using DigestiveSimulator.Data;

namespace DigestiveSimulator.Runtime
{
    public sealed class RuntimeSpeciesDatabase
    {
        private readonly Dictionary<string, SpeciesDefinition> species = new Dictionary<string, SpeciesDefinition>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Dictionary<string, OrganDefinition>> organs = new Dictionary<string, Dictionary<string, OrganDefinition>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DigestionGraph> graphs = new Dictionary<string, DigestionGraph>(StringComparer.OrdinalIgnoreCase);

        public void RegisterSpecies(SpeciesDefinition definition, IEnumerable<OrganDefinition> organDefinitions, DigestionGraph graph)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.id)) throw new ArgumentException("Species must have an ID.");
            ScientificDataValidator.ValidateSpecies(definition, organDefinitions, graph);
            species[definition.id] = definition;
            graphs[definition.id] = graph;
            var byId = new Dictionary<string, OrganDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var organ in organDefinitions) byId.Add(organ.id, organ);
            organs[definition.id] = byId;
        }

        public bool RemoveSpecies(string id) { organs.Remove(id); graphs.Remove(id); return species.Remove(id); }
        public SpeciesDefinition GetSpecies(string id) => species.TryGetValue(id, out var value) ? value : null;
        public IReadOnlyCollection<SpeciesDefinition> GetAllSpecies() => species.Values;
        public bool Contains(string id) => species.ContainsKey(id);
        public OrganDefinition GetOrgan(string speciesId, string organId) => organs.TryGetValue(speciesId, out var map) && map.TryGetValue(organId, out var value) ? value : null;
        public DigestionGraph GetGraph(string speciesId) => graphs.TryGetValue(speciesId, out var value) ? value : null;
    }
}

