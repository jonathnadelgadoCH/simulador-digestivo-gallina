using System;

namespace DigestiveSimulator.Data
{
    [Serializable]
    public sealed class SpeciesDefinition
    {
        public string schemaVersion;
        public string contentVersion;
        public string scientificStatus;
        public string id;
        public string commonName;
        public string scientificName;
        public string digestiveType;
        public SpeciesModelDefinition model;
        public DigestiveSystemDefinition digestiveSystem;
        public AbsorptionRouteDefinition[] absorptionRoutes;
        public string[] referenceIds;
    }

    [Serializable]
    public sealed class SpeciesModelDefinition
    {
        public string exteriorFile;
        public string digestiveSystemFile;
        public float scale = 1f;
    }

    [Serializable]
    public sealed class DigestiveSystemDefinition
    {
        public string graphFile;
        public string[] organs;
    }

    [Serializable]
    public sealed class AbsorptionRouteDefinition
    {
        public string id;
        public string displayName;
        public string[] nutrientKeys;
        public string absorbedForm;
        public string[] siteOrganIds;
        public string transport;
        public string targetOrganId;
        public string[] destinations;
        public string color;
        public string caveat;
        public string[] referenceIds;
    }
}
