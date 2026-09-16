using UnityEngine;

namespace DigestiveSimulator.Presentation
{
    public static class RuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateApplication()
        {
            if (Object.FindAnyObjectByType<MvpController>() != null) return;
            var root = new GameObject("DigestiveSimulator_MVP");
            Object.DontDestroyOnLoad(root);
            root.AddComponent<MvpController>();
        }
    }
}
