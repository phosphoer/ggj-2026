// The MIT License (MIT)
// Copyright (c) 2021 David Evans @phosphoer
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SpriteAtlasAsset))]
public class SpriteAtlasAssetEditor : Editor
{
  public override void OnInspectorGUI()
  {
    DrawDefaultInspector();

    if (GUILayout.Button("Generate Atlas"))
    {
      Generate();
    }
  }

  private void Generate()
  {
    var atlasAsset = target as SpriteAtlasAsset;
    // Re-import all the atlas textures with no compression
    TextureImporterCompression[] oldCompressionSettings = new TextureImporterCompression[atlasAsset.Textures.Length];
    if (atlasAsset.DisableCompression)
    {
      for (int i = 0; i < atlasAsset.Textures.Length; ++i)
      {
        string path = AssetDatabase.GetAssetPath(atlasAsset.Textures[i]);
        TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(path);
        oldCompressionSettings[i] = importer.textureCompression;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        AssetDatabase.ImportAsset(path);
      }
    }

    // Create a render texture to draw the atlas in
    RenderTexture atlasTexture = new RenderTexture(atlasAsset.AtlasWidth, atlasAsset.AtlasHeight, 0, RenderTextureFormat.ARGB32);
    atlasTexture.Create();

    // Figure out column/row count based on sizes
    int columnCount = atlasAsset.AtlasWidth / atlasAsset.CellWidth;
    int currentColumn = 0;
    int currentRow = 0;

    // Render each texture to the atlas in a loop
    RenderTexture.active = atlasTexture;
    GL.PushMatrix();
    GL.LoadIdentity();
    GL.LoadProjectionMatrix(Matrix4x4.Ortho(0, atlasAsset.AtlasWidth, atlasAsset.AtlasHeight, 0, -1, 1));
    for (int i = 0; i < atlasAsset.Textures.Length; ++i)
    {
      // Draw the texture to a particular cell in the atlas
      Texture2D tex = atlasAsset.Textures[i];
      Graphics.DrawTexture(new Rect(currentColumn * atlasAsset.CellWidth, currentRow * atlasAsset.CellHeight, atlasAsset.CellWidth, atlasAsset.CellHeight), tex);

      // Track row and column
      ++currentColumn;
      if (currentColumn >= columnCount)
      {
        currentColumn = 0;
        currentRow += 1;
      }
    }
    GL.PopMatrix();

    // Create the final texture and read from the rendertex
    Texture2D savedTex = new Texture2D(atlasTexture.width, atlasTexture.height, TextureFormat.ARGB32, true);
    savedTex.ReadPixels(new Rect(0, 0, atlasTexture.width, atlasTexture.height), 0, 0);
    savedTex.Apply();

    // Write the texture to file
    string currentPath = UnityEditor.AssetDatabase.GetAssetPath(atlasAsset);
    currentPath = System.IO.Path.GetDirectoryName(currentPath);
    string atlasPath = System.IO.Path.Combine($"{currentPath}", $"{atlasAsset.AtlasName}.png");
    byte[] fileBytes = savedTex.EncodeToPNG();
    System.IO.File.WriteAllBytes(atlasPath, fileBytes);

    // Reset source textures to old compression settings
    if (atlasAsset.DisableCompression)
    {
      for (int i = 0; i < atlasAsset.Textures.Length; ++i)
      {
        string path = AssetDatabase.GetAssetPath(atlasAsset.Textures[i]);
        TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(path);
        importer.textureCompression = oldCompressionSettings[i];

        AssetDatabase.ImportAsset(path);
      }
    }

    // Refresh asset database to show the new texture 
    UnityEditor.AssetDatabase.Refresh();
  }
}