using System;
using System.Collections;
using System.Collections.Generic;
using DigestiveSimulator.Data;
using UnityEngine;

namespace DigestiveSimulator.Runtime
{
    public sealed class SpeciesLoader : MonoBehaviour
    {
        private readonly StreamingAssetClient client = new StreamingAssetClient();
        public RuntimeSpeciesDatabase Database { get; } = new RuntimeSpeciesDatabase();

        public IEnumerator LoadCatalog(Action onComplete, Action<string> onError)
        {
            var root = StreamingAssetClient.Join(Application.streamingAssetsPath, "DigestiveSimulator");
            SpeciesCatalog catalog = null;
            yield return client.GetText(StreamingAssetClient.Join(root, "catalogs/species_catalog.json"), json => catalog = JsonDataReader.Read<SpeciesCatalog>(json), onError);
            if (catalog == null) yield break;
            ScientificDataValidator.ValidateVersion(catalog.schemaVersion, "species catalog");
            foreach (var entry in catalog.species ?? Array.Empty<CatalogEntry>())
                yield return LoadSpecies(root, entry, onError);
            onComplete?.Invoke();
        }

        private IEnumerator LoadSpecies(string root, CatalogEntry entry, Action<string> onError)
        {
            SpeciesDefinition species = null;
            yield return client.GetText(StreamingAssetClient.Join(root, entry.definitionPath), json => species = JsonDataReader.Read<SpeciesDefinition>(json), onError);
            if (species == null) yield break;
            if (!string.Equals(species.id, entry.id, StringComparison.OrdinalIgnoreCase)) { onError?.Invoke($"Species catalog ID mismatch for '{entry.id}'."); yield break; }

            var basePath = entry.definitionPath.Substring(0, entry.definitionPath.LastIndexOf('/'));
            DigestionGraph graph = null;
            yield return client.GetText(StreamingAssetClient.Join(root, $"{basePath}/{species.digestiveSystem.graphFile}"), json => graph = JsonDataReader.Read<DigestionGraph>(json), onError);
            if (graph == null) yield break;

            var organs = new List<OrganDefinition>();
            foreach (var organId in species.digestiveSystem.organs ?? Array.Empty<string>())
            {
                OrganDefinition organ = null;
                var path = $"{basePath}/organs/{organId}/organ.json";
                yield return client.GetText(StreamingAssetClient.Join(root, path), json => organ = JsonDataReader.Read<OrganDefinition>(json), onError);
                if (organ == null) yield break;
                organs.Add(organ);
            }
            Database.RegisterSpecies(species, organs, graph);
        }
    }
}
