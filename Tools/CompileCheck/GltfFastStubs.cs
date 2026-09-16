using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GLTFast
{
    // Compile-check substitute only. Unity uses com.unity.cloud.gltfast.
    public sealed class GltfImport : IDisposable
    {
        public Task<bool> Load(string url, object importSettings = null, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> InstantiateMainSceneAsync(Transform parent, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public void Dispose() { }
    }
}
