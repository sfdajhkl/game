#if UNITY_EDITOR
using System;
using System.IO;
using Arvito.Core;
using Arvito.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Arvito.Editor
{
    /// <summary>Creates real Unity assets on first import. No hand-written scene or prefab YAML.</summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        const string ScenePath = "Assets/Scenes/Arvito.unity";
        const string FontPath = "Assets/Resources/Arvito/Fonts/Arvito-Korean.asset";
        const string PlayRequested = "Arvito.PlayRequested";
        static ProjectSetup()
        {
            EditorApplication.delayCall += FirstImport;
            if (SessionState.GetBool(PlayRequested, false)) EditorApplication.delayCall += EnterGame;
        }
        static void FirstImport()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += FirstImport; return; }
            if (!File.Exists(ScenePath) || !File.Exists(FontPath)) Prepare();
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        }
        [MenuItem("Arvito/Prepare Project")]
        public static void Prepare()
        {
            if (!File.Exists(ScenePath) && !Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ValidateSerialization();
            if (AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset") == null)
                TMP_PackageResourceImporter.ImportResources(true, false, false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureAssets();
            PlayerSettings.companyName = "Arvito";
            PlayerSettings.productName = "아르비토";
            PlayerSettings.bundleVersion = "0.2.0";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, "games.arvito.pc");
            PlayerSettings.defaultScreenWidth = 1440; PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true; PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = false;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetApiCompatibilityLevel(UnityEditor.Build.NamedBuildTarget.Standalone, ApiCompatibilityLevel.NET_Standard);
            EditorSettings.serializationMode = SerializationMode.ForceText;
            EditorSettings.enterPlayModeOptionsEnabled = false;
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory("Assets/Scenes");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("Arvito", typeof(GameApp));
                var camera = new GameObject("Main Camera", typeof(Camera)); camera.tag = "MainCamera";
                var c = camera.GetComponent<Camera>(); c.orthographic = true; c.clearFlags = CameraClearFlags.SolidColor; c.backgroundColor = Ui.Ink; c.transform.position = new Vector3(0, 0, -10);
                camera.AddComponent<AudioListener>();
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Arvito prepared. Open Assets/Scenes/Arvito.unity and press Play.");
        }
        [MenuItem("Arvito/Validate Unity Save Format")]
        public static void ValidateSerialization()
        {
            var catalog = JsonUtility.FromJson<Catalog>(File.ReadAllText("Assets/Resources/Arvito/catalog.json")); catalog.Normalize();
            var original = Game.New(catalog, "저장 검사", 42);
            original.Apply(new Effects { recruit = "mira" });
            original.State.region = "forest"; original.State.eventId = "first_hunt"; original.Choose(0);
            if (original.State.mode != "combat") throw new InvalidDataException("Opening event did not reach combat.");
            var restored = new Game(catalog, JsonUtility.FromJson<GameState>(JsonUtility.ToJson(original.State)));
            if (!Game.Valid(restored.State, catalog)) throw new InvalidDataException("Unity could not restore the combat save.");
            original.Act("guard", original.State.combat.active); restored.Act("guard", restored.State.combat.active);
            if (JsonUtility.ToJson(original.State) != JsonUtility.ToJson(restored.State)) throw new InvalidDataException("Combat diverged after Unity JSON restore.");
            Debug.Log("Arvito Unity JSON round-trip check passed.");
        }
        static void ConfigureAssets()
        {
            foreach (string id in new[] { "village", "forest", "ruins", "camp", "battle", "characters" })
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath("Assets/Resources/Arvito/Art/" + id + ".png");
                importer.textureType = TextureImporterType.Default; importer.filterMode = FilterMode.Point; importer.wrapMode = TextureWrapMode.Clamp; importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 2048;
                if (id == "characters") PixelCharacterImporter.Configure(importer);
                importer.SaveAndReimport();
            }
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) == null)
            {
                var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Arvito/Fonts/NotoSerifKR-Regular.ttf");
                var font = TMP_FontAsset.CreateFontAsset(source, 48, 5, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
                font.name = "Arvito-Korean"; AssetDatabase.CreateAsset(font, FontPath);
                foreach (var atlas in font.atlasTextures) { atlas.name = "Arvito Korean Atlas"; AssetDatabase.AddObjectToAsset(atlas, font); }
                font.material.name = "Arvito Korean Material"; AssetDatabase.AddObjectToAsset(font.material, font);
                EditorUtility.SetDirty(font);
            }
        }
        [MenuItem("Arvito/Open Game Scene")]
        public static void OpenScene() { if (!File.Exists(ScenePath)) Prepare(); else if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath); }
        [MenuItem("Arvito/Play Game _F5")]
        public static void PlayGame()
        {
            if (Application.isBatchMode) throw new InvalidOperationException("Play Game requires an interactive Editor.");
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Prepare();
            EditorSceneManager.OpenScene(ScenePath);
            // Keep the request across an asset import/domain reload during setup.
            SessionState.SetBool(PlayRequested, true);
            EditorApplication.delayCall += EnterGame;
        }
        static void EnterGame()
        {
            if (!SessionState.GetBool(PlayRequested, false)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += EnterGame; return; }
            SessionState.SetBool(PlayRequested, false);
            if (EditorSceneManager.GetActiveScene().path != ScenePath) return;
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            EditorApplication.isPlaying = true;
        }
        [MenuItem("Arvito/Build Windows")]
        public static void BuildWindows()
        {
            Prepare();
            string output = Environment.GetEnvironmentVariable("ARVITO_BUILD_PATH");
            if (string.IsNullOrWhiteSpace(output)) output = Path.GetFullPath("../../dist/Arvito-Windows/Arvito.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = output, target = BuildTarget.StandaloneWindows64, options = BuildOptions.None });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Arvito Windows build failed: " + report.summary.result);
            var licenses = Path.Combine(Path.GetDirectoryName(output), "Licenses"); Directory.CreateDirectory(licenses);
            File.Copy("Assets/Resources/Arvito/Fonts/OFL-NotoSerifKR.txt", Path.Combine(licenses, "OFL-NotoSerifKR.txt"), true);
            if (File.Exists("Tools/PLAYTEST.txt")) File.Copy("Tools/PLAYTEST.txt", Path.Combine(Path.GetDirectoryName(output), "PLAYTEST.txt"), true);
            Debug.Log("Windows build created: " + output);
        }
    }
}
#endif
