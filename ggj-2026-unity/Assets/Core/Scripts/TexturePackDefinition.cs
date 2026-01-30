// The MIT License (MIT)
// Copyright (c) 2019 David Evans @phosphoer
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

// Allows for easy creation of a texture with up to 4 single-channel textures packed into it
// Can be used at runtime with the public generate method or at edit time by saving to a file 
[CreateAssetMenu(fileName = "new-texture-pack", menuName = "Texture Pack Definition")]
public class TexturePackDefinition : ScriptableObject
{
  public Texture2D ChannelRedTex;
  public Texture2D ChannelGreenTex;
  public Texture2D ChannelBlueTex;
  public Texture2D ChannelAlphaTex;
  public int OutputTextureSize = 512;
  public string OutputFileName = "packed-texture";

  public Texture2D GeneratePackedTexture()
  {
    // Using a render texture to resize the input textures with 
    RenderTexture workingTexture = new RenderTexture(OutputTextureSize, OutputTextureSize, 0);
    workingTexture.Create();

    // Get the channel data of each texture 
    Color[] channelRed = GetColorArray(ChannelRedTex, workingTexture);
    Color[] channelGreen = GetColorArray(ChannelGreenTex, workingTexture);
    Color[] channelBlue = GetColorArray(ChannelBlueTex, workingTexture);
    Color[] channelAlpha = GetColorArray(ChannelAlphaTex, workingTexture);

    // Build an output color buffer and fill with the respective channels of each input texture
    Color[] packedColors = new Color[channelRed.Length];
    for (int i = 0; i < channelRed.Length; ++i)
    {
      Color packedColor = new Color();
      packedColor.r = channelRed != null ? channelRed[i].r : 0;
      packedColor.g = channelGreen != null ? channelGreen[i].r : 0;
      packedColor.b = channelBlue != null ? channelBlue[i].r : 0;
      packedColor.a = channelAlpha != null ? channelAlpha[i].r : 0;
      packedColors[i] = packedColor;
    }

    // Create the packed texture 
    Texture2D packedTexture = new Texture2D(OutputTextureSize, OutputTextureSize, TextureFormat.ARGB32, false, false);
    packedTexture.SetPixels(packedColors);
    packedTexture.Apply();

    workingTexture.Release();

    return packedTexture;
  }

  private Color[] GetColorArray(Texture2D fromTexture, RenderTexture workingTexture)
  {
    if (fromTexture == null)
      return null;

    // Blit the texture to a temp render texture and read it back 
    // to handle resizing
    Graphics.Blit(fromTexture, workingTexture);
    RenderTexture.active = workingTexture;
    Texture2D resizedTex = new Texture2D(workingTexture.width, workingTexture.height, TextureFormat.ARGB32, false, false);
    resizedTex.ReadPixels(new Rect(0, 0, workingTexture.width, workingTexture.height), 0, 0);
    resizedTex.Apply();
    RenderTexture.active = null;
    return resizedTex.GetPixels();
  }

#if UNITY_EDITOR
  [ContextMenu("Save Packed Texture")]
  public void SavePackedTexture()
  {
    // Create the packed texture and write it to a PNG file in the same directory 
    Texture2D packedTexture = GeneratePackedTexture();
    string assetPath = Path.Combine(Application.dataPath, "../", AssetDatabase.GetAssetPath(this));
    string assetDir = Path.GetDirectoryName(assetPath);
    string finalPath = Path.Combine(assetDir, OutputFileName + ".png");
    FileStream fs = null;
    try
    {
      fs = new FileStream(finalPath, FileMode.Create);
      using (BinaryWriter writer = new BinaryWriter(fs))
      {
        byte[] textureBytes = packedTexture.EncodeToPNG();
        writer.Write(textureBytes);
      }

      Debug.LogFormat("Wrote packed texture to {0}", finalPath);
    }
    catch (System.Exception e)
    {
      Debug.LogErrorFormat("Failed to write texture to {0}", finalPath);
      Debug.Log(e.Message);
    }

    AssetDatabase.Refresh();
  }

  [CustomEditor(typeof(TexturePackDefinition))]
  [CanEditMultipleObjects]
  public class TexturePackDefinitionEditor : Editor
  {
    public override void OnInspectorGUI()
    {
      DrawDefaultInspector();

      if (GUILayout.Button("Save Packed Texture"))
      {
        foreach (Object obj in targets)
        {
          TexturePackDefinition texturePack = obj as TexturePackDefinition;
          if (texturePack != null)
          {
            texturePack.SavePackedTexture();
          }
        }
      }
    }
  }

#endif
}