#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
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
        const string NarrativeFontPath = "Assets/Resources/Arvito/Fonts/Arvito-YiSunShin.asset";
        const string NarrativeFallbackPath = "Assets/Resources/Arvito/Fonts/Arvito-YiSunShin-Dynamic.asset";
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
            if (!File.Exists(ScenePath) || !File.Exists(FontPath) || !File.Exists(NarrativeFontPath) || !File.Exists(NarrativeFallbackPath)) Prepare();
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
            ValidateArtImports();
            PlayerSettings.companyName = "Arvito";
            PlayerSettings.productName = "아르비토";
            PlayerSettings.bundleVersion = "0.3.2";
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
            var paper = (TextureImporter)AssetImporter.GetAtPath(PaperGrainImporter.Path);
            if (paper != null)
            {
                PaperGrainImporter.Configure(paper);
                paper.SaveAndReimport();
            }
            foreach (string id in new[] { "village", "forest", "ruins", "camp", "battle", "characters" })
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath("Assets/Resources/Arvito/Art/" + id + ".png");
                if (importer == null) throw new InvalidDataException("Missing texture importer: " + id + ".png. Project: " + Application.dataPath);
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
            ConfigureNarrativeFont();
        }
        static void ConfigureNarrativeFont()
        {
            var uiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            ClearDynamicDataOnBuild(uiFont);
            var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Arvito/Fonts/YiSunShin-Regular.ttf");
            if (source == null) throw new InvalidDataException("Missing YiSunShin-Regular.ttf. Pull the latest repository.");
            var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NarrativeFallbackPath);
            if (fallback == null)
            {
                fallback = TMP_FontAsset.CreateFontAsset(source, 48, 5, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                SaveFont(fallback, NarrativeFallbackPath, "Arvito-YiSunShin-Dynamic");
            }
            fallback.fallbackFontAssetTable = new List<TMP_FontAsset> { uiFont };
            ClearDynamicDataOnBuild(fallback);
            var narrative = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NarrativeFontPath);
            if (narrative == null)
            {
                narrative = TMP_FontAsset.CreateFontAsset(source, 48, 5, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                string characters = File.ReadAllText("Assets/Resources/Arvito/Fonts/YiSunShin-Characters.txt");
                if (!narrative.TryAddCharacters(characters, out string missing))
                    throw new InvalidDataException("YiSunShin atlas could not bake characters: " + missing);
                narrative.atlasPopulationMode = AtlasPopulationMode.Static;
                SaveFont(narrative, NarrativeFontPath, "Arvito-YiSunShin");
            }
            // Unbaked names/new text use the same face first; unsupported glyphs use bundled Noto.
            narrative.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };
            EditorUtility.SetDirty(narrative);
            EditorUtility.SetDirty(fallback);
        }
        static void SaveFont(TMP_FontAsset font, string path, string name)
        {
            font.name = name;
            AssetDatabase.CreateAsset(font, path);
            for (int i = 0; i < font.atlasTextures.Length; i++)
            {
                var atlas = font.atlasTextures[i]; atlas.name = name + " Atlas " + i;
                AssetDatabase.AddObjectToAsset(atlas, font);
            }
            font.material.name = name + " Material";
            AssetDatabase.AddObjectToAsset(font.material, font);
            EditorUtility.SetDirty(font);
        }
        static void ClearDynamicDataOnBuild(TMP_FontAsset font)
        {
            var serialized = new SerializedObject(font);
            serialized.FindProperty("m_ClearDynamicDataOnBuild").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        [MenuItem("Arvito/Repair Art Imports")]
        public static void RepairArtImports()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Stop Play mode before repairing art imports."); return; }
            foreach (string path in Directory.GetFiles("Assets/Resources/Arvito/Art", "*.png", SearchOption.AllDirectories))
                AssetDatabase.ImportAsset(path.Replace('\\', '/'), ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            ConfigureAssets();
            ValidateArtImports();
        }
        public static void ValidateArtImports()
        {
            int count = 0;
            foreach (string path in Directory.GetFiles("Assets/Resources/Arvito/Art", "*.png", SearchOption.AllDirectories))
            {
                string resource = path.Replace('\\', '/').Replace("Assets/Resources/", "");
                resource = resource.Substring(0, resource.Length - 4);
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(path.Replace('\\', '/')) == null || Resources.Load<Texture2D>(resource) == null)
                    throw new InvalidDataException("Cannot load illustration: " + path + ". Use Arvito > Repair Art Imports. Project: " + Application.dataPath);
                count++;
            }
            if (count < 506) throw new InvalidDataException("Art files are missing (" + count + "/506). Pull the latest repository. Project: " + Application.dataPath);
            Debug.Log("Arvito 0.3.2: " + count + " textures verified via Resources.Load. Project: " + Application.dataPath);
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
            File.Copy("Assets/Resources/Arvito/Fonts/LICENSE-YiSunShin.txt", Path.Combine(licenses, "LICENSE-YiSunShin.txt"), true);
            if (File.Exists("Tools/PLAYTEST.txt")) File.Copy("Tools/PLAYTEST.txt", Path.Combine(Path.GetDirectoryName(output), "PLAYTEST.txt"), true);
            Debug.Log("Windows build created: " + output);
        }
    }
}
#endif
