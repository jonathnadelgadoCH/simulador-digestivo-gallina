using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace DigestiveSimulator.Editor
{
    public static class WebGlBuild
    {
        [MenuItem("Digestive Simulator/Build WebGL")]
        public static void Build()
        {
            // GitHub Pages and other managed static hosts do not always expose
            // configurable Content-Encoding headers. Keep Unity's JavaScript
            // decompressor in the build so compressed WebGL files remain portable.
            PlayerSettings.WebGL.decompressionFallback = true;
            // El proyecto crea colliders, primitivas y AudioSource durante la ejecución.
            PlayerSettings.stripEngineCode = false;
            ConfigureProductionProtection();
            PrepareBuildSupportMaterials();

            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
                throw new BuildFailedException("No hay escenas habilitadas en Build Settings.");

            var outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "WebGLBuild"));
            Directory.CreateDirectory(outputPath);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"Falló el build WebGL: {report.summary.result}");

            ValidateProductionOutput(outputPath);
            Debug.Log($"Build WebGL generado en {outputPath} ({report.summary.totalSize} bytes).");
        }

        private static void ConfigureProductionProtection()
        {
            // Un build WebGL siempre debe enviar código compilado al navegador. Estas
            // opciones evitan acompañarlo con símbolos, nombres de pila y diagnósticos
            // que facilitan reconstruir la implementación desde DevTools.
            PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.showDiagnostics = false;

            foreach (LogType logType in Enum.GetValues(typeof(LogType)))
                PlayerSettings.SetStackTraceLogType(logType, StackTraceLogType.None);
        }

        private static void ValidateProductionOutput(string outputPath)
        {
            var forbiddenFiles = Directory
                .EnumerateFiles(outputPath, "*", SearchOption.AllDirectories)
                .Where(path =>
                {
                    var extension = Path.GetExtension(path).ToLowerInvariant();
                    return extension == ".map"
                        || extension == ".pdb"
                        || extension == ".cs"
                        || extension == ".py"
                        || path.EndsWith(".symbols.json", StringComparison.OrdinalIgnoreCase);
                })
                .Select(path => Path.GetRelativePath(outputPath, path))
                .ToArray();

            if (forbiddenFiles.Length > 0)
                throw new BuildFailedException(
                    "El build contiene archivos de desarrollo que no deben publicarse:\n"
                    + string.Join("\n", forbiddenFiles));
        }

        private static void PrepareBuildSupportMaterials()
        {
            const string folder = "Assets/Resources/DigestiveSimulatorBuildSupport";
            EnsureFolder("Assets", "Resources");
            EnsureFolder("Assets/Resources", "DigestiveSimulatorBuildSupport");

            var gltfShader = AssetDatabase.LoadAssetAtPath<Shader>(
                "Packages/com.unity.cloud.gltfast/Runtime/Shader/glTF-pbrMetallicRoughness.shadergraph");
            if (gltfShader == null)
                throw new BuildFailedException("No se encontró el shader PBR de glTFast requerido por los modelos GLB.");

            SaveMaterial($"{folder}/GltfOpaqueDouble.mat", gltfShader, material =>
            {
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetFloat("_Surface", 0f);
                material.SetFloat("_ZWrite", 1f);
                material.SetFloat("_Cull", 0f);
                material.renderQueue = (int)RenderQueue.Geometry;
            });
            SaveMaterial($"{folder}/GltfTransparentDouble.mat", gltfShader, material =>
            {
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.EnableKeyword("_DISABLE_SSR_TRANSPARENT");
                material.EnableKeyword("_ENABLE_FOG_ON_TRANSPARENT");
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_ZWrite", 0f);
                material.SetFloat("_Cull", 0f);
                material.renderQueue = (int)RenderQueue.Transparent;
            });
            SaveMaterial($"{folder}/UrpLit.mat", Shader.Find("Universal Render Pipeline/Lit"), _ => { });
            SaveMaterial($"{folder}/UrpUnlit.mat", Shader.Find("Universal Render Pipeline/Unlit"), _ => { });
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureFolder(string parent, string name)
        {
            var path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
        }

        private static void SaveMaterial(string path, Shader shader, System.Action<Material> configure)
        {
            if (shader == null) throw new BuildFailedException($"No se encontró el shader para {path}.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else material.shader = shader;
            configure(material);
            EditorUtility.SetDirty(material);
        }
    }
}
