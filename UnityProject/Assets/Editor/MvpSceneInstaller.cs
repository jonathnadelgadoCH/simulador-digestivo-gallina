#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace DigestiveSimulator.Editor
{
    public static class MvpSceneInstaller
    {
        public const string ScenePath = "Assets/Scenes/00_MVP.unity";

        [InitializeOnLoadMethod]
        private static void ScheduleEnsureScene()
        {
            EditorApplication.delayCall += EnsureSceneAndBuildSettings;
        }

        [MenuItem("Digestive Simulator/Crear o reparar escena MVP")]
        public static void EnsureSceneAndBuildSettings()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                var previous = SceneManager.GetActiveScene();
                var replaceUntitledScene = previous.IsValid() && string.IsNullOrEmpty(previous.path) && !previous.isDirty;
                if (previous.IsValid() && string.IsNullOrEmpty(previous.path) && previous.isDirty) return;
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, replaceUntitledScene ? NewSceneMode.Single : NewSceneMode.Additive);
                EditorSceneManager.SaveScene(scene, ScenePath);
                if (!replaceUntitledScene)
                {
                    EditorSceneManager.CloseScene(scene, true);
                    if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                }
                AssetDatabase.SaveAssets();
            }

            var scenes = EditorBuildSettings.scenes.ToList();
            var existing = scenes.FindIndex(x => x.path == ScenePath);
            if (existing < 0) scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            else scenes[existing] = new EditorBuildSettingsScene(ScenePath, true);
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
