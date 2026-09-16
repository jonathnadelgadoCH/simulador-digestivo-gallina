using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

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

            Debug.Log($"Build WebGL generado en {outputPath} ({report.summary.totalSize} bytes).");
        }
    }
}
