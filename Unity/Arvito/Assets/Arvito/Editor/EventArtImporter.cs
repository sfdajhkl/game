#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Arvito.Editor
{
    /// <summary>Keep the authored 192x128 raster and palette intact on import and every platform.</summary>
    public sealed class EventArtImporter : AssetPostprocessor
    {
        const string Folder = "Assets/Resources/Arvito/Art/Events/";
        public override uint GetVersion() => 1;
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder, System.StringComparison.Ordinal)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.isReadable = false;
            importer.anisoLevel = 0;
            importer.maxTextureSize = 256;
            foreach (string platform in new[] { "Standalone", "Android", "iPhone", "WebGL" })
                importer.ClearPlatformTextureSettings(platform);
        }
    }
}
#endif
