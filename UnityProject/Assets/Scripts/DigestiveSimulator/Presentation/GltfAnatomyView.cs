using System;
using System.Collections.Generic;
using System.Linq;
using DigestiveSimulator.Data;
using DigestiveSimulator.Runtime;
using UnityEngine;

namespace DigestiveSimulator.Presentation
{
    public sealed class GltfAnatomyView : IAnatomyView, IDisposable
    {
        private readonly LoadedSpeciesModel loadedModel;
        private readonly Dictionary<string, OrganView> views = new Dictionary<string, OrganView>(StringComparer.OrdinalIgnoreCase);
        private readonly Renderer[] exteriorRenderers;

        public Transform Root => loadedModel.Root != null ? loadedModel.Root.transform : null;
        public IReadOnlyDictionary<string, OrganView> Views => views;

        public GltfAnatomyView(LoadedSpeciesModel model, SpeciesDefinition species, RuntimeSpeciesDatabase database)
        {
            loadedModel = model ?? throw new ArgumentNullException(nameof(model));
            exteriorRenderers = model.ExteriorRoot.GetComponentsInChildren<Renderer>(true);
            RepairUnsupportedMaterials(exteriorRenderers, true);
            RepairUnsupportedMaterials(model.DigestiveRoot.GetComponentsInChildren<Renderer>(true), false);
            BindOrgans(species, database);
        }

        public void SetVisibility(AnatomyVisibility mode)
        {
            if (loadedModel.Root == null) return;
            loadedModel.ExteriorRoot.SetActive(mode != AnatomyVisibility.DigestiveOnly);
            loadedModel.DigestiveRoot.SetActive(mode != AnatomyVisibility.Exterior);
            if (mode == AnatomyVisibility.Exterior) SetExteriorAlpha(0.95f);
            else if (mode == AnatomyVisibility.Transparent) SetExteriorAlpha(0.18f);
        }

        public void SetActive(bool active)
        {
            if (loadedModel.Root != null) loadedModel.Root.SetActive(active);
        }

        public bool TryGetView(string organId, out OrganView view) => views.TryGetValue(organId, out view);

        public void Clear() => Dispose();

        public void Dispose()
        {
            views.Clear();
            loadedModel.Dispose();
        }

        private void BindOrgans(SpeciesDefinition species, RuntimeSpeciesDatabase database)
        {
            var transforms = loadedModel.DigestiveRoot.GetComponentsInChildren<Transform>(true);
            for (var index = 0; index < species.digestiveSystem.organs.Length; index++)
            {
                var organId = species.digestiveSystem.organs[index];
                var node = transforms.FirstOrDefault(x => string.Equals(x.name, organId, StringComparison.OrdinalIgnoreCase));
                if (node == null) throw new InvalidOperationException($"GLB does not contain organ object '{organId}'.");
                var renderer = node.GetComponent<Renderer>() ?? node.GetComponentInChildren<Renderer>(true);
                if (renderer == null) throw new InvalidOperationException($"GLB organ '{organId}' has no renderer.");
                var host = renderer.gameObject;
                host.name = organId;
                EnsureCollider(host);
                var view = host.GetComponent<OrganView>() ?? host.AddComponent<OrganView>();
                var color = Color.HSVToRGB(Mathf.Repeat(index * 0.113f, 1f), 0.58f, 0.9f);
                view.Initialize(database.GetOrgan(species.id, organId), color);
                views.Add(organId, view);
            }
        }

        private static void EnsureCollider(GameObject host)
        {
            if (host.GetComponent<Collider>() != null) return;
            var filter = host.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
            {
                var collider = host.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
            }
            else
            {
                host.AddComponent<BoxCollider>();
            }
        }

        private static void RepairUnsupportedMaterials(IEnumerable<Renderer> renderers, bool transparent)
        {
            var fallbackPath = transparent
                ? "DigestiveSimulatorBuildSupport/GltfTransparentDouble"
                : "DigestiveSimulatorBuildSupport/GltfOpaqueDouble";
            var fallback = Resources.Load<Material>(fallbackPath);
            if (fallback == null) return;

            foreach (var renderer in renderers)
            {
                var materials = renderer.materials;
                var changed = false;
                for (var index = 0; index < materials.Length; index++)
                {
                    var original = materials[index];
                    if (original != null && original.shader != null && original.shader.isSupported &&
                        !string.Equals(original.shader.name, "Hidden/InternalErrorShader", StringComparison.Ordinal)) continue;

                    var replacement = new Material(fallback) { name = $"{renderer.name}_WebGLFallback" };
                    if (TryReadBaseColor(original, out var color))
                    {
                        if (replacement.HasProperty("baseColorFactor")) replacement.SetColor("baseColorFactor", color);
                        if (replacement.HasProperty("_BaseColor")) replacement.SetColor("_BaseColor", color);
                        if (replacement.HasProperty("_Color")) replacement.SetColor("_Color", color);
                    }
                    materials[index] = replacement;
                    changed = true;
                }
                if (changed) renderer.materials = materials;
            }
        }

        private static bool TryReadBaseColor(Material material, out Color color)
        {
            color = Color.white;
            if (material == null) return false;
            foreach (var property in new[] { "baseColorFactor", "_BaseColor", "_Color" })
            {
                if (!material.HasProperty(property)) continue;
                color = material.GetColor(property);
                return true;
            }
            return false;
        }

        private void SetExteriorAlpha(float alpha)
        {
            foreach (var renderer in exteriorRenderers)
            {
                foreach (var material in renderer.materials)
                {
                    var property = material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
                    if (!material.HasProperty(property)) continue;
                    var color = material.GetColor(property);
                    color.a = alpha;
                    material.SetColor(property, color);
                }
            }
        }
    }
}
