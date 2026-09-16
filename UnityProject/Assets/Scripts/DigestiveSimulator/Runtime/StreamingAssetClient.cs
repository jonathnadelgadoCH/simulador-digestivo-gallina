using System;
using System.Collections;
using UnityEngine.Networking;

namespace DigestiveSimulator.Runtime
{
    public sealed class StreamingAssetClient
    {
        public IEnumerator GetText(string url, Action<string> onSuccess, Action<string> onError)
        {
            using (var request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success) onError?.Invoke($"Could not load '{url}': {request.error}");
                else onSuccess?.Invoke(request.downloadHandler.text);
            }
        }

        public static string Join(string root, string relative) => $"{root.TrimEnd('/')}/{relative.TrimStart('/')}";
    }
}

