using UnityEditor;
using UnityEngine;

namespace MyArcade.EditorTools
{
	public class SpriteImporter : AssetPostprocessor
	{
		private const string Folder = "Assets/Resources/Sprites/";

		private void OnPreprocessTexture()
		{
			if (!assetPath.StartsWith(Folder))
			{
				return;
			}
			var importer = (TextureImporter)assetImporter;
			importer.textureType = TextureImporterType.Sprite;
			importer.spriteImportMode = SpriteImportMode.Single;
			importer.spritePixelsPerUnit = 50f;
			importer.filterMode = FilterMode.Point;
			importer.textureCompression = TextureImporterCompression.Uncompressed;
			importer.alphaIsTransparency = true;
			importer.mipmapEnabled = false;
			importer.npotScale = TextureImporterNPOTScale.None;
			var settings = new TextureImporterSettings();
			importer.ReadTextureSettings(settings);
			settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
			settings.spriteMeshType = SpriteMeshType.FullRect;
			importer.SetTextureSettings(settings);
		}
	}
}
