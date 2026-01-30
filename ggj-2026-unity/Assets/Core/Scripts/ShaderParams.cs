using UnityEngine;

public static class ShaderParams
{
  public static readonly int Color = Shader.PropertyToID("_Color");
  public static readonly int MainTex = Shader.PropertyToID("_MainTex");
  public static readonly int VertexColorGlow = Shader.PropertyToID("_VertexColorGlow");
  public static readonly int Glow = Shader.PropertyToID("_Glow");
  public static readonly int FresnelOpacity = Shader.PropertyToID("_FresnelOpacity");
  public static readonly int RadarColor = Shader.PropertyToID("_RadarColor");
}