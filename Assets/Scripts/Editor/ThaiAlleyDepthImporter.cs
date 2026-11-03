using UnityEditor;
using UnityEngine;
namespace MyArcade.EditorTools
{
	public sealed class ThaiAlleyDepthImporter : AssetPostprocessor
	{
		private void OnPreprocessTexture()
		{
			if (!assetPath.StartsWith("Assets/Resources/Environment/ThaiAlleyV4/")) return;
			var importer = (TextureImporter)assetImporter;
			if (assetPath.Contains("/Crowd/"))
			{
				importer.textureType = TextureImporterType.Sprite;
				importer.spriteImportMode = SpriteImportMode.Single;
				importer.spritePixelsPerUnit = 64;
				importer.filterMode = FilterMode.Point;
				importer.textureCompression = TextureImporterCompression.Uncompressed;
				importer.mipmapEnabled = false;
				importer.alphaIsTransparency = true;
				importer.npotScale = TextureImporterNPOTScale.None;
				var settings = new TextureImporterSettings();
				importer.ReadTextureSettings(settings);
				settings.spriteMeshType = SpriteMeshType.FullRect;
				settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
				importer.SetTextureSettings(settings);
				return;
			}
			importer.textureType = TextureImporterType.Default;
			importer.filterMode = FilterMode.Point;
			importer.wrapMode = TextureWrapMode.Repeat;
			importer.textureCompression = TextureImporterCompression.Uncompressed;
			importer.mipmapEnabled = true;
			importer.maxTextureSize = 2048;
			importer.npotScale = TextureImporterNPOTScale.None;
		}
	}
}
