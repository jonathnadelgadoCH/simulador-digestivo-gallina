using System;

namespace DigestiveSimulator.Data
{
    [Serializable]
    public sealed class SpeciesFoodProfile
    {
        public string schemaVersion;
        public string contentVersion;
        public string scientificStatus;
        public string speciesId;
        public string foodId;
        public DigestibilityDefinition digestibility;
        public PhysiologicalResponseDefinition physiologicalResponse;
        public StudyConditionsDefinition studyConditions;
        public SimulationProfile simulation;
        public string[] referenceIds;
    }

    [Serializable] public sealed class DigestibilityDefinition { public float? dryMatter; public float? protein; public float? starch; public float? fat; public float? energy; public string type; public string method; public string units; public string energyUnits; }
    [Serializable] public sealed class PhysiologicalResponseDefinition { public string[] mainOrganIds; public string[] motilityEffects; public string[] secretions; public string[] neuroendocrineResponses; public string[] metabolicNotes; }
    [Serializable] public sealed class StudyConditionsDefinition { public string age; public string geneticLine; public string diet; public string processing; public string method; }
    [Serializable] public sealed class SimulationProfile { public string particleColor; public float speedMultiplier = 1f; public string[] highlightedNutrients; public SimulationStageOverride[] stageOverrides; }
    [Serializable] public sealed class SimulationStageOverride { public string organId; public float durationSeconds; public string message; public string audioFile; }
}
