using System;
using System.Collections;
using DigestiveSimulator.Data;
using UnityEngine;

namespace DigestiveSimulator.Runtime
{
    [RequireComponent(typeof(SpeciesLoader), typeof(FoodLoader), typeof(SpeciesFoodProfileLoader))]
    public sealed class DigestiveApplication : MonoBehaviour
    {
        private SpeciesLoader speciesLoader;
        private FoodLoader foodLoader;
        private SpeciesFoodProfileLoader profileLoader;

        public SimulationEngine Simulation { get; } = new SimulationEngine();
        public RuntimeSpeciesDatabase SpeciesDatabase => speciesLoader != null ? speciesLoader.Database : null;
        public RuntimeFoodDatabase FoodDatabase => foodLoader != null ? foodLoader.Database : null;
        public SpeciesDefinition ActiveSpecies { get; private set; }
        public FoodDefinition ActiveFood { get; private set; }
        public bool IsReady { get; private set; }

        public event Action Ready;
        public event Action<string> Error;
        public event Action SelectionChanged;

        private void Awake()
        {
            speciesLoader = GetComponent<SpeciesLoader>();
            foodLoader = GetComponent<FoodLoader>();
            profileLoader = GetComponent<SpeciesFoodProfileLoader>();
        }

        private IEnumerator Start()
        {
            var speciesDone = false;
            var foodsDone = false;
            yield return speciesLoader.LoadCatalog(() => speciesDone = true, ReportError);
            yield return foodLoader.LoadCatalog(() => foodsDone = true, ReportError);
            IsReady = speciesDone && foodsDone;
            if (IsReady) Ready?.Invoke();
        }

        public bool SelectSpecies(string id)
        {
            var value = speciesLoader.Database.GetSpecies(id);
            if (value == null) return false;
            ActiveSpecies = value;
            SelectionChanged?.Invoke();
            return true;
        }

        public bool SelectFood(string id)
        {
            var value = foodLoader.Database.GetFood(id);
            if (value == null) return false;
            ActiveFood = value;
            SelectionChanged?.Invoke();
            return true;
        }

        public IEnumerator PrepareSimulation(Action onReady = null)
        {
            if (ActiveSpecies == null || ActiveFood == null) { ReportError("Select a species and a food before preparing the simulation."); yield break; }
            SpeciesFoodProfile profile = null;
            yield return profileLoader.Load(ActiveSpecies.id, ActiveFood.id, ActiveSpecies, ActiveFood, value => profile = value, ReportError);
            if (profile == null) yield break;
            Simulation.Configure(ActiveSpecies, ActiveFood, profile, speciesLoader.Database.GetGraph(ActiveSpecies.id));
            onReady?.Invoke();
        }

        public IEnumerator LoadProfile(string speciesId, string foodId, Action<SpeciesFoodProfile> onSuccess)
        {
            var species = speciesLoader.Database.GetSpecies(speciesId);
            var food = foodLoader.Database.GetFood(foodId);
            if (species == null || food == null) { ReportError($"Cannot load profile '{speciesId}+{foodId}'."); yield break; }
            yield return profileLoader.Load(speciesId, foodId, species, food, onSuccess, ReportError);
        }

        private void ReportError(string message) { Debug.LogError(message); Error?.Invoke(message); }
    }
}
