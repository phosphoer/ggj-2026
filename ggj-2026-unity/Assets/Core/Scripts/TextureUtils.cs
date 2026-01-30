using UnityEngine;

public static class TextureUtils
{
  public static void CopyRenderTextureToTexture(RenderTexture sourceRenderTex, Texture2D destTexture)
  {
    RenderTexture.active = sourceRenderTex;
    destTexture.ReadPixels(new Rect(0, 0, sourceRenderTex.width, sourceRenderTex.height), 0, 0);
    RenderTexture.active = null;
  }

  public static bool TextureFromBase64(ref string base64Data, Texture2D destTex)
  {
    // Convert base64 data to image bytes
    byte[] imageBytes = System.Convert.FromBase64String(base64Data);
    if (imageBytes != null)
    {
      ImageConversion.LoadImage(destTex, imageBytes, false);
      return true;
    }

    return false;
  }

  public static string TextureToBase64(Texture2D texture)
  {
    return System.Convert.ToBase64String(texture.EncodeToPNG());
  }
}