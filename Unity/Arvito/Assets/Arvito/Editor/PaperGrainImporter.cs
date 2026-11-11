#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Arvito.Editor
{
    public sealed class PaperGrainImporter : AssetPostprocessor
    {
        internal const string Path = "Assets/Resources/Arvito/UI/paper-grain.png";
        void OnPreprocessTexture()
        {
            if (assetPath == Path) Configure((TextureImporter)assetImporter);
        }
        internal static void Configure(TextureImporter paper)
        {
            paper.textureType = TextureImporterType.Default; paper.filterMode = FilterMode.Bilinear;
            paper.wrapMode = TextureWrapMode.Repeat; paper.mipmapEnabled = false;
            paper.textureCompression = TextureImporterCompression.Uncompressed; paper.maxTextureSize = 512;
            paper.npotScale = TextureImporterNPOTScale.None;
        }
    }
}
#endif
