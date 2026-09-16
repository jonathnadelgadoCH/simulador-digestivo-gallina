using System;
using System.Collections.Generic;
using System.Linq;
using DigestiveSimulator.Data;
using DigestiveSimulator.Runtime;
using UnityEngine;

namespace DigestiveSimulator.Presentation
{
    public enum AnatomyVisibility { Exterior, Transparent, DigestiveOnly }

    public sealed class AnatomySchematicBuilder : IAnatomyView
    {
        private readonly Dictionary<string, OrganView> views = new Dictionary<string, OrganView>(StringComparer.OrdinalIgnoreCase);
        private GameObject root;
        private Renderer bodyRenderer;
        private Material bodyMaterial;

        public Transform Root => root != null ? root.transform : null;
        public IReadOnlyDictionary<string, OrganView> Views => views;

        public void Build(SpeciesDefinition species, RuntimeSpeciesDatabase database)
        {
            Clear();
            root = new GameObject($"Anatomy_{species.id}");
            var graph = database.GetGraph(species.id);
            var nodeById = graph.nodes.ToDictionary(x => x.id, StringComparer.OrdinalIgnoreCase);
            var mainNodes = OrderedMainPath(graph, nodeById).ToArray();
            var nodePositions = new Dictionary<string, Vector3>(StringComparer.OrdinalIgnoreCase);

            for (var index = 0; index < mainNodes.Length; index++)
            {
                var y = (mainNodes.Length - 1) * 0.34f - index * 0.68f;
                var position = new Vector3(Mathf.Sin(index * 0.62f) * 0.42f, y, Mathf.Cos(index * 0.47f) * 0.18f);
                nodePositions[mainNodes[index].id] = position;
                CreateOrgan(species.id, mainNodes[index], database, position, index);
            }

            var side = -1f;
            foreach (var node in graph.nodes.Where(x => x.accessory))
            {
                var target = FindFirstMainTarget(node, nodeById, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                var anchor = target != null && nodePositions.TryGetValue(target.id, out var value) ? value : Vector3.zero;
                var position = anchor + new Vector3(1.35f * side, 0.2f, 0f);
                side *= -1f;
                CreateOrgan(species.id, node, database, position, views.Count);
            }

            CreateBody(mainNodes.Length);
            SetVisibility(AnatomyVisibility.Transparent);
        }

        public void SetVisibility(AnatomyVisibility mode)
        {
            if (bodyRenderer == null) return;
            bodyRenderer.enabled = mode != AnatomyVisibility.DigestiveOnly;
            if (mode == AnatomyVisibility.Exterior) SetBodyAlpha(0.82f);
            else if (mode == AnatomyVisibility.Transparent) SetBodyAlpha(0.14f);
            foreach (var view in views.Values) view.gameObject.SetActive(mode != AnatomyVisibility.Exterior);
        }

        public bool TryGetView(string organId, out OrganView view) => views.TryGetValue(organId, out view);

        public void SetActive(bool active)
        {
            if (root != null) root.SetActive(active);
        }

        public void Clear()
        {
            views.Clear();
            if (root != null) UnityEngine.Object.Destroy(root);
            root = null;
            bodyRenderer = null;
        }

        private void CreateOrgan(string speciesId, DigestiveNode node, RuntimeSpeciesDatabase database, Vector3 position, int colorIndex)
        {
            var primitive = node.accessory ? PrimitiveType.Sphere : node.type == "branch" ? PrimitiveType.Cube : PrimitiveType.Capsule;
            var organObject = GameObject.CreatePrimitive(primitive);
            organObject.name = node.organId;
            organObject.transform.SetParent(root.transform, false);
            organObject.transform.localPosition = position;
            organObject.transform.localScale = node.accessory ? Vector3.one * 0.42f : new Vector3(0.48f, 0.34f, 0.48f);
            var renderer = organObject.GetComponent<Renderer>();
            renderer.material = CreateMaterial(false);
            var color = Color.HSVToRGB(Mathf.Repeat(colorIndex * 0.113f, 1f), 0.58f, 0.9f);
            var view = organObject.AddComponent<OrganView>();
            view.Initialize(database.GetOrgan(speciesId, node.organId), color);
            views.Add(node.organId, view);
        }

        private void CreateBody(int mainNodeCount)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Exterior_Schematic";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(2.15f, Mathf.Max(2.1f, mainNodeCount * 0.35f), 1.5f);
            var collider = body.GetComponent<Collider>();
            if (collider != null) { collider.enabled = false; UnityEngine.Object.Destroy(collider); }
            bodyMaterial = CreateMaterial(true);
            bodyRenderer = body.GetComponent<Renderer>();
            bodyRenderer.material = bodyMaterial;
        }

        private void SetBodyAlpha(float alpha)
        {
            var color = new Color(0.72f, 0.78f, 0.84f, alpha);
            bodyMaterial.color = color;
            if (bodyMaterial.HasProperty("_BaseColor")) bodyMaterial.SetColor("_BaseColor", color);
        }

        private static Material CreateMaterial(bool transparent)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            var material = new Material(shader);
            if (!transparent) return material;
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.renderQueue = 3000;
            return material;
        }

        private static IEnumerable<DigestiveNode> OrderedMainPath(DigestionGraph graph, IReadOnlyDictionary<string, DigestiveNode> byId)
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<string>();
            queue.Enqueue(graph.entryNodeId);
            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                if (!visited.Add(id) || !byId.TryGetValue(id, out var node)) continue;
                if (!node.accessory) yield return node;
                foreach (var next in node.nextNodeIds ?? Array.Empty<string>())
                    if (byId.TryGetValue(next, out var candidate) && !candidate.accessory) queue.Enqueue(next);
            }
        }

        private static DigestiveNode FindFirstMainTarget(DigestiveNode node, IReadOnlyDictionary<string, DigestiveNode> byId, ISet<string> visited)
        {
            if (!visited.Add(node.id)) return null;
            foreach (var nextId in node.nextNodeIds ?? Array.Empty<string>())
            {
                if (!byId.TryGetValue(nextId, out var next)) continue;
                if (!next.accessory) return next;
                var target = FindFirstMainTarget(next, byId, visited);
                if (target != null) return target;
            }
            return null;
        }
    }
}
