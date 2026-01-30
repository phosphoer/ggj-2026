#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

public class SpriteImporter : AssetPostprocessor
{
  public void OnPreprocessTexture()
  {
    if (assetPath.Contains("ui-icon"))
    {
      TextureImporter textureImporter = (TextureImporter)assetImporter;
      textureImporter.textureType = TextureImporterType.Sprite;
      textureImporter.spriteImportMode = SpriteImportMode.Single;

      Debug.Log($"Imported {assetPath} as Sprite");
    }
  }
}

#endif