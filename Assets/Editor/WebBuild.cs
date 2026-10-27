using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BrainrotWaves.EditorTools
{
    /// <summary>
    /// Сборка веб-версии (WebGL) для Vercel / GitHub Pages.
    /// Gzip + decompression fallback: работает на любом статическом хостинге без настройки заголовков.
    /// Результат: D:\Unity\Builds\WebGL и Logs/webgl-build.txt
    /// </summary>
    public static class WebBuild
    {
        public const string OutDir = "D:/Unity/Builds/WebGL";
        const string ReportPath = "Logs/webgl-build.txt";

        [MenuItem("Brainrot/Build WebGL")]
        public static void Start()
        {
            File.WriteAllText(ReportPath, "running");
            EditorApplication.delayCall += Run;
        }

        public static void Run()
        {
            try
            {
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
                PlayerSettings.WebGL.decompressionFallback = true;
                PlayerSettings.WebGL.dataCaching = true;
                PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
                PlayerSettings.WebGL.memorySize = 512;
                PlayerSettings.runInBackground = true;
                PlayerSettings.productName = "Brainrot Waves";

                var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
                if (Directory.Exists(OutDir)) Directory.Delete(OutDir, true);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = OutDir,
                    target = BuildTarget.WebGL,
                    targetGroup = BuildTargetGroup.WebGL,
                    options = BuildOptions.None
                });
                var s = report.summary;
                string text = $"{s.result} errors={s.totalErrors} warnings={s.totalWarnings} size={s.totalSize / (1024 * 1024)}MB time={s.totalTime}";
                if (s.result != BuildResult.Succeeded)
                    foreach (var step in report.steps)
                        foreach (var m in step.messages)
                            if (m.type == LogType.Error || m.type == LogType.Exception) text += "\n" + m.content;
                File.WriteAllText(ReportPath, text);
            }
            catch (System.Exception e)
            {
                File.WriteAllText(ReportPath, "Exception " + e);
            }
        }
    }
}
