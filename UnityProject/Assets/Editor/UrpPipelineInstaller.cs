#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DigestiveSimulator.Editor
{
    public static class UrpPipelineInstaller
    {
        private const string SettingsFolder = "Assets/Settings";
        private const string RenderingFolder = "Assets/Settings/Rendering";
        private const string PipelinePath = RenderingFolder + "/DigestiveSimulatorURP.asset";
        private const string RendererPath = RenderingFolder + "/DigestiveSimulatorRenderer.asset";

        [InitializeOnLoadMethod]
        private static void ScheduleEnsurePipeline()
        {
            EditorApplication.delayCall += EnsurePipeline;
        }

        [MenuItem("Digestive Simulator/Configurar Universal Render Pipeline")]
        public static void EnsurePipeline()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            EnsureFolder("Assets", "Settings", SettingsFolder);
            EnsureFolder(SettingsFolder, "Rendering", RenderingFolder);

            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                rendererData.name = "DigestiveSimulatorRenderer";
                AssetDatabase.CreateAsset(rendererData, RendererPath);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                pipeline.name = "DigestiveSimulatorURP";
                pipeline.supportsHDR = false;
                pipeline.supportsCameraDepthTexture = false;
                pipeline.supportsCameraOpaqueTexture = false;
                pipeline.msaaSampleCount = 2;
                pipeline.renderScale = 1f;
                pipeline.shadowDistance = 20f;
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            if (GraphicsSettings.defaultRenderPipeline != pipeline)
            {
                GraphicsSettings.defaultRenderPipeline = pipeline;
                QualitySettings.renderPipeline = null;
                EditorUtility.SetDirty(pipeline);
                AssetDatabase.SaveAssets();
            }
        }

        private static void EnsureFolder(string parent, string name, string fullPath)
        {
            if (!AssetDatabase.IsValidFolder(fullPath)) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
