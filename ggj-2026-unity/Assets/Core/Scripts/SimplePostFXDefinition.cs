using UnityEngine;

[CreateAssetMenu(fileName = "new-postfx-settings", menuName = "PostFX Definition", order = 0)]
public class SimplePostFXDefinition : ScriptableObject
{
  [System.Serializable]
  public struct BloomSettings
  {
    public bool Enabled;
    public float BloomFilterSize;
    public float BloomThreshold;
    public float BloomThresholdSoft;
    public float BloomIntensity;
  }

  [System.Serializable]
  public struct SaturationContrastSettings
  {
    public bool Enabled;
    public float ColorSaturation;
    public float ColorContrast;
    public float ColorBrightness;
  }

  [System.Serializable]
  public struct WhiteBalanceSettings
  {
    public bool Enabled;
    public float ColorTemperature;
    public float ColorTint;
  }

  [System.Serializable]
  public struct ChannelMixerSettings
  {
    public bool Enabled;
    public Vector3 ChannelMixerRed;
    public Vector3 ChannelMixerGreen;
    public Vector3 ChannelMixerBlue;
  }

  [System.Serializable]
  public struct BlurSettings
  {
    public bool Enabled;
    public float FilterSize;
    [Range(0, 1)] public float Opacity;
  }

  public struct AllSettings
  {
    public BloomSettings Bloom;
    public BlurSettings Blur;
    public SaturationContrastSettings SaturationContrast;
    public WhiteBalanceSettings WhiteBalance;
    public ChannelMixerSettings ChannelMixers;
  }

  public static readonly BloomSettings BloomDefault = new BloomSettings() { BloomFilterSize = 1, BloomThreshold = 1, BloomThresholdSoft = 0, BloomIntensity = 0 };
  public static readonly SaturationContrastSettings SaturationContrastDefault = new SaturationContrastSettings() { ColorSaturation = 1, ColorContrast = 1, ColorBrightness = 1 };
  public static readonly WhiteBalanceSettings WhiteBalanceDefault = new WhiteBalanceSettings() { ColorTemperature = 0, ColorTint = 0 };
  public static readonly ChannelMixerSettings ChannelMixerDefault = new ChannelMixerSettings() { ChannelMixerRed = Vector3.right, ChannelMixerGreen = Vector3.up, ChannelMixerBlue = Vector3.forward };
  public static readonly BlurSettings BlurDefault = new BlurSettings() { Opacity = 0, FilterSize = 0 };

  public static BloomSettings Lerp(BloomSettings a, BloomSettings b, float t)
  {
    BloomSettings s = a;
    s.Enabled = a.Enabled || b.Enabled;
    s.BloomFilterSize = Mathf.Lerp(a.BloomFilterSize, b.BloomFilterSize, t);
    s.BloomThreshold = Mathf.Lerp(a.BloomThreshold, b.BloomThreshold, t);
    s.BloomThresholdSoft = Mathf.Lerp(a.BloomThresholdSoft, b.BloomThresholdSoft, t);
    s.BloomIntensity = Mathf.Lerp(a.BloomIntensity, b.BloomIntensity, t);
    return s;
  }

  public static SaturationContrastSettings Lerp(SaturationContrastSettings a, SaturationContrastSettings b, float t)
  {
    SaturationContrastSettings s = a;
    s.Enabled = a.Enabled || b.Enabled;
    s.ColorSaturation = Mathf.Lerp(a.ColorSaturation, b.ColorSaturation, t);
    s.ColorContrast = Mathf.Lerp(a.ColorContrast, b.ColorContrast, t);
    s.ColorBrightness = Mathf.Lerp(a.ColorBrightness, b.ColorBrightness, t);
    return s;
  }

  public static WhiteBalanceSettings Lerp(WhiteBalanceSettings a, WhiteBalanceSettings b, float t)
  {
    WhiteBalanceSettings s = a;
    s.Enabled = a.Enabled || b.Enabled;
    s.ColorTemperature = Mathf.Lerp(a.ColorTemperature, b.ColorTemperature, t);
    s.ColorTint = Mathf.Lerp(a.ColorTint, b.ColorTint, t);
    return s;
  }

  public static ChannelMixerSettings Lerp(ChannelMixerSettings a, ChannelMixerSettings b, float t)
  {
    ChannelMixerSettings s = a;
    s.Enabled = a.Enabled || b.Enabled;
    s.ChannelMixerRed = Vector3.Lerp(a.ChannelMixerRed, b.ChannelMixerRed, t);
    s.ChannelMixerGreen = Vector3.Lerp(a.ChannelMixerGreen, b.ChannelMixerGreen, t);
    s.ChannelMixerBlue = Vector3.Lerp(a.ChannelMixerBlue, b.ChannelMixerBlue, t);
    return s;
  }

  public static BlurSettings Lerp(BlurSettings a, BlurSettings b, float t)
  {
    BlurSettings s = a;
    s.Enabled = a.Enabled || b.Enabled;
    s.FilterSize = Mathf.Lerp(a.FilterSize, b.FilterSize, t);
    s.Opacity = Mathf.Lerp(a.Opacity, b.Opacity, t);
    return s;
  }


  public int Priority = 0;

  public BloomSettings Bloom = BloomDefault;
  public SaturationContrastSettings SaturationContrast = SaturationContrastDefault;
  public WhiteBalanceSettings WhiteBalance = WhiteBalanceDefault;
  public ChannelMixerSettings ChannelMixers = ChannelMixerDefault;
  public BlurSettings Blur = BlurDefault;
}