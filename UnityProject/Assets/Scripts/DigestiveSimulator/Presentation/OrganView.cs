using DigestiveSimulator.Data;
using UnityEngine;

namespace DigestiveSimulator.Presentation
{
    public sealed class OrganView : MonoBehaviour
    {
        private Renderer cachedRenderer;
        private Color baseColor;
        private bool selected;
        private bool simulationActive;

        public OrganDefinition Definition { get; private set; }
        public string OrganId => Definition != null ? Definition.id : string.Empty;

        public void Initialize(OrganDefinition definition, Color color)
        {
            Definition = definition;
            cachedRenderer = GetComponent<Renderer>();
            baseColor = color;
            RefreshColor();
        }

        public void SetSelected(bool value) { selected = value; RefreshColor(); }
        public void SetSimulationActive(bool value) { simulationActive = value; RefreshColor(); }

        private void RefreshColor()
        {
            if (cachedRenderer == null) return;
            var color = simulationActive ? new Color(0.2f, 0.95f, 1f) : selected ? new Color(1f, 0.82f, 0.2f) : baseColor;
            foreach (var material in cachedRenderer.materials)
            {
                if (material.HasProperty("baseColorFactor")) material.SetColor("baseColorFactor", color);
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            }
        }
    }
}
