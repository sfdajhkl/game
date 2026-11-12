#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Arvito.Editor
{
    public static class WebBuild
    {
        [MenuItem("Arvito/Web/Build and Run in Browser")]
        public static void BuildAndRun() => Build(true);
        [MenuItem("Arvito/Web/Build Web Folder")]
        public static void BuildWeb() => Build(false);
        static void Build(bool run)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("먼저 Unity 재생을 멈춰 주세요.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                throw new InvalidOperationException("Unity Hub → Installs → 이 Editor의 톱니바퀴 → Add modules → Web Build Support를 설치해 주세요.");
            ProjectSetup.Prepare();
            var target = NamedBuildTarget.WebGL;
            PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(target, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.Medium);
            PlayerSettings.SetIl2CppCodeGeneration(target, Il2CppCodeGeneration.OptimizeSize);
            // .unityweb files work on static hosting without server-specific Content-Encoding rules.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.initialMemorySize = 128;
            PlayerSettings.WebGL.maximumMemorySize = 512;
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
            PlayerSettings.WebGL.template = "PROJECT:Arvito";
            AssetDatabase.SaveAssets();
            string output = Environment.GetEnvironmentVariable("ARVITO_WEB_BUILD_PATH");
            if (string.IsNullOrWhiteSpace(output)) output = Path.GetFullPath("../../dist/Arvito-Web");
            Directory.CreateDirectory(output);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { "Assets/Scenes/Arvito.unity" }, locationPathName = output,
                target = BuildTarget.WebGL, options = run ? BuildOptions.AutoRunPlayer : BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Arvito Web build failed: " + report.summary.result);
            var licenses = Path.Combine(output, "Licenses"); Directory.CreateDirectory(licenses);
            foreach (string file in new[] { "OFL-NotoSerifKR.txt", "LICENSE-YiSunShin.txt" })
                File.Copy("Assets/Resources/Arvito/Fonts/" + file, Path.Combine(licenses, file), true);
            File.WriteAllText(Path.Combine(output, ".nojekyll"), "");
            Debug.Log("Arvito Web build ready: " + output + " (" + report.summary.totalSize + " bytes). Serve this folder over HTTP/HTTPS; do not open index.html as a file.");
        }
    }
}
#endif
