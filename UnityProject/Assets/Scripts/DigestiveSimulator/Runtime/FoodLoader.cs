using System;
using System.Collections;
using DigestiveSimulator.Data;
using UnityEngine;

namespace DigestiveSimulator.Runtime
{
    public sealed class FoodLoader : MonoBehaviour
    {
        private readonly StreamingAssetClient client = new StreamingAssetClient();
        public RuntimeFoodDatabase Database { get; } = new RuntimeFoodDatabase();

        public IEnumerator LoadCatalog(Action onComplete, Action<string> onError)
        {
            var root = StreamingAssetClient.Join(Application.streamingAssetsPath, "DigestiveSimulator");
            FoodCatalog catalog = null;
            yield return client.GetText(StreamingAssetClient.Join(root, "catalogs/food_catalog.json"), json => catalog = JsonDataReader.Read<FoodCatalog>(json), onError);
            if (catalog == null) yield break;
            ScientificDataValidator.ValidateVersion(catalog.schemaVersion, "food catalog");
            foreach (var entry in catalog.foods ?? Array.Empty<CatalogEntry>())
            {
                FoodDefinition food = null;
                yield return client.GetText(StreamingAssetClient.Join(root, entry.definitionPath), json => food = JsonDataReader.Read<FoodDefinition>(json), onError);
                if (food == null) yield break;
                if (!string.Equals(food.id, entry.id, StringComparison.OrdinalIgnoreCase)) { onError?.Invoke($"Food catalog ID mismatch for '{entry.id}'."); yield break; }
                Database.RegisterFood(food);
            }
            onComplete?.Invoke();
        }
    }
}

