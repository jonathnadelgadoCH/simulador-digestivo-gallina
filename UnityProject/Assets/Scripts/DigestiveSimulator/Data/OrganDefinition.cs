using System;

namespace DigestiveSimulator.Data
{
    [Serializable]
    public sealed class OrganDefinition
    {
        public string schemaVersion;
        public string contentVersion;
        public string scientificStatus;
        public string id;
        public string speciesId;
        public string commonName;
        public string anatomicalName;
        public AnatomyDefinition anatomy;
        public PhysiologyDefinition physiology;
        public RegulationDefinition regulation;
        public NutritionDefinition nutrition;
        public MicrobiologyDefinition microbiology;
        public string narrationFile;
        public OrganModelDefinition model;
        public string[] referenceIds;
    }

    [Serializable] public sealed class AnatomyDefinition { public string location; public string morphology; public string relations; public string histology; }
    [Serializable] public sealed class PhysiologyDefinition { public string mainFunction; public string mechanicalDigestion; public string chemicalDigestion; public string motility; public string[] secretions; public string[] enzymes; public string[] absorption; public string[] digestiveProducts; }
    [Serializable] public sealed class RegulationDefinition { public string nervousControl; public string autonomicControl; public NeuroendocrineSignal[] signals; }
    [Serializable] public sealed class NutritionDefinition { public string[] nutrients; public string[] foodInteractions; public string[] limitations; }
    [Serializable] public sealed class MicrobiologyDefinition { public bool applicable; public string[] microorganisms; public string[] fermentation; public string[] products; }
    [Serializable] public sealed class OrganModelDefinition { public string file; public string objectId; public float scale = 1f; }

    [Serializable]
    public sealed class NeuroendocrineSignal
    {
        public string name;
        public string type;
        public string origin;
        public string receptor;
        public string targetOrganId;
        public string response;
        public string referenceId;
    }
}
