using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DigestiveSimulator.Data;
using GLTFast;
using UnityEngine;

namespace DigestiveSimulator.Runtime
{
    public enum SpeciesModelLoadingState { Idle, LoadingExterior, LoadingDigestiveSystem, Ready, Error }

    public sealed class LoadedSpeciesModel : IDisposable
    {
        private readonly GltfImport exteriorImport;
        private readonly GltfImport digestiveImport;

        public GameObject Root { get; }
        public GameObject ExteriorRoot { get; }
        public GameObject DigestiveRoot { get; }

        internal LoadedSpeciesModel(GameObject root, GameObject exteriorRoot, GameObject digestiveRoot, GltfImport exterior, GltfImport digestive)
        {
            Root = root;
            ExteriorRoot = exteriorRoot;
            DigestiveRoot = digestiveRoot;
            exteriorImport = exterior;
            digestiveImport = digestive;
        }

        public void Dispose()
        {
            if (Root != null) UnityEngine.Object.Destroy(Root);
            exteriorImport?.Dispose();
            digestiveImport?.Dispose();
        }
    }

    public sealed class SpeciesModelLoader : MonoBehaviour
    {
        public SpeciesModelLoadingState State { get; private set; } = SpeciesModelLoadingState.Idle;
        public string LastError { get; private set; }

        public async Task<LoadedSpeciesModel> Load(SpeciesDefinition species, CancellationToken cancellationToken)
        {
            if (species?.model == null) throw new ArgumentException("Species model definition is required.", nameof(species));
            LastError = null;
            var root = new GameObject($"LoadedModel_{species.id}");
            root.transform.localScale = Vector3.one * Mathf.Max(0.0001f, species.model.scale);
            var exteriorRoot = new GameObject("Exterior");
            var digestiveRoot = new GameObject("DigestiveSystem");
            exteriorRoot.transform.SetParent(root.transform, false);
            digestiveRoot.transform.SetParent(root.transform, false);
            GltfImport exteriorImport = null;
            GltfImport digestiveImport = null;

            try
            {
                State = SpeciesModelLoadingState.LoadingExterior;
                exteriorImport = new GltfImport();
                var exteriorUrl = ModelUrl(species.id, species.model.exteriorFile);
                if (!await exteriorImport.Load(exteriorUrl, cancellationToken: cancellationToken) || !await exteriorImport.InstantiateMainSceneAsync(exteriorRoot.transform, cancellationToken))
                    throw new InvalidOperationException($"Could not load exterior GLB '{exteriorUrl}'.");

                State = SpeciesModelLoadingState.LoadingDigestiveSystem;
                digestiveImport = new GltfImport();
                var digestiveUrl = ModelUrl(species.id, species.model.digestiveSystemFile);
                if (!await digestiveImport.Load(digestiveUrl, cancellationToken: cancellationToken) || !await digestiveImport.InstantiateMainSceneAsync(digestiveRoot.transform, cancellationToken))
                    throw new InvalidOperationException($"Could not load digestive GLB '{digestiveUrl}'.");

                cancellationToken.ThrowIfCancellationRequested();
                State = SpeciesModelLoadingState.Ready;
                return new LoadedSpeciesModel(root, exteriorRoot, digestiveRoot, exteriorImport, digestiveImport);
            }
            catch (Exception exception)
            {
                if (root != null) Destroy(root);
                exteriorImport?.Dispose();
                digestiveImport?.Dispose();
                if (exception is OperationCanceledException) throw;
                LastError = exception.Message;
                State = SpeciesModelLoadingState.Error;
                throw;
            }
        }

        private static string ModelUrl(string speciesId, string file)
        {
            var contentRoot = StreamingAssetClient.Join(Application.streamingAssetsPath, "DigestiveSimulator");
            var path = StreamingAssetClient.Join(contentRoot, $"species/{speciesId}/{file}");

            // glTFast expects a URL. Desktop StreamingAssets paths are ordinary
            // absolute paths, while WebGL/Android already provide URL-like paths.
            if (!path.Contains("://", StringComparison.Ordinal) && Path.IsPathRooted(path))
                return new Uri(path).AbsoluteUri;

            return path;
        }
    }
}
