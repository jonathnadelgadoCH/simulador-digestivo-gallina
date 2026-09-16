using System;
using System.Collections.Generic;
using DigestiveSimulator.Data;

namespace DigestiveSimulator.Runtime
{
    public sealed class RuntimeFoodDatabase
    {
        private readonly Dictionary<string, FoodDefinition> foods = new Dictionary<string, FoodDefinition>(StringComparer.OrdinalIgnoreCase);

        public void RegisterFood(FoodDefinition food)
        {
            if (food == null || string.IsNullOrWhiteSpace(food.id)) throw new ArgumentException("Food must have an ID.");
            ScientificDataValidator.ValidateVersion(food.schemaVersion, $"food '{food.id}'");
            ScientificDataValidator.ValidateApproval(food.scientificStatus, food.compositionReferenceIds, $"food '{food.id}'");
            foods[food.id] = food;
        }

        public bool RemoveFood(string id) => foods.Remove(id);
        public FoodDefinition GetFood(string id) => foods.TryGetValue(id, out var value) ? value : null;
        public IReadOnlyCollection<FoodDefinition> GetAllFoods() => foods.Values;
        public bool Contains(string id) => foods.ContainsKey(id);
    }
}

