using System;

namespace DigestiveSimulator.Data
{
    [Serializable]
    public sealed class FoodDefinition
    {
        public string schemaVersion;
        public string contentVersion;
        public string scientificStatus;
        public string id;
        public string commonName;
        public string scientificOrSourceName;
        public string category;
        public string description;
        public NutrientComposition composition;
        public FoodVisualDefinition visual;
        public string[] compositionReferenceIds;
    }

    [Serializable]
    public sealed class NutrientComposition
    {
        public float? dryMatter;
        public float? crudeProtein;
        public float? starch;
        public float? crudeFiber;
        public float? etherExtract;
        public float? energy;
        public string basis;
        public string units;
    }

    [Serializable]
    public sealed class FoodVisualDefinition
    {
        public string modelFile;
        public string iconFile;
        public string color;
        public float scale = 1f;
    }
}

