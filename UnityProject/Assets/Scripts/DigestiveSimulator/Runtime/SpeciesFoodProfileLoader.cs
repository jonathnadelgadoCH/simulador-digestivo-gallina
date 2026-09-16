using System;
using System.Collections;
using DigestiveSimulator.Data;
using UnityEngine;

namespace DigestiveSimulator.Runtime
{
    public sealed class SpeciesFoodProfileLoader : MonoBehaviour
    {
        private readonly StreamingAssetClient client = new StreamingAssetClient();

        public IEnumerator Load(string speciesId, string foodId, SpeciesDefinition species, FoodDefinition food, Action<SpeciesFoodProfile> onSuccess, Action<string> onError)
        {
            var root = StreamingAssetClient.Join(Application.streamingAssetsPath, "DigestiveSimulator");
            var path = StreamingAssetClient.Join(root, $"profiles/{speciesId}/{foodId}.json");
            SpeciesFoodProfile profile = null;
            yield return client.GetText(path, json => profile = JsonDataReader.Read<SpeciesFoodProfile>(json), onError);
            if (profile == null) yield break;
            ScientificDataValidator.ValidateProfile(profile, species, food);
            onSuccess?.Invoke(profile);
        }
    }
}

