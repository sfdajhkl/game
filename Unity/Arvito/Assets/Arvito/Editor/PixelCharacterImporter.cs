#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Arvito.Editor
{
    /// <summary>Apply on normal Git pull/import too, including projects already prepared.</summary>
    public sealed class PixelCharacterImporter : AssetPostprocessor
    {
        const string AtlasPath="Assets/Resources/Arvito/Art/characters.png";
        public override uint GetVersion() => 1;
        void OnPreprocessTexture()
        {
            if (assetPath==AtlasPath) Configure((TextureImporter)assetImporter);
        }
        internal static void Configure(TextureImporter importer)
        {
            importer.textureType=TextureImporterType.Default;
            importer.filterMode=FilterMode.Point;
            importer.wrapMode=TextureWrapMode.Clamp;
            importer.mipmapEnabled=false;
            importer.npotScale=TextureImporterNPOTScale.None;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.crunchedCompression=false;
            importer.alphaSource=TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency=false;
            importer.anisoLevel=0;
            importer.maxTextureSize=256;
            // Prevent a previous per-platform override from changing the fixed raster or palette.
            foreach (string platform in new[] { "Standalone", "Android", "iPhone", "WebGL" })
                importer.ClearPlatformTextureSettings(platform);
        }
    }
}
#endif
