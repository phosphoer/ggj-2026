using UnityEngine;
using System.Collections.Generic;

public class SimplePostFX : MonoBehaviour
{
  public SimplePostFXDefinition InitialSettings = null;
  public bool IsPrimary = false;

  [Range(0, 8)] public int BloomIterations = 4;
  [Range(0, 8)] public int BlurIterations = 4;

  private List<PostFXLayer> _layers = new List<PostFXLayer>();
  private List<LayerFade> _fadeInLayers = new List<LayerFade>();
  private List<LayerFade> _fadeOutLayers = new List<LayerFade>();

  [System.Serializable]
  public class PostFXLayer
  {
    public PostFXLayer(SimplePostFXDefinition settings, float weight)
    {
      Settings = settings;
      Weight = weight;
    }

    public SimplePostFXDefinition Settings = null;
    public float Weight = 1;
  }

  [System.Serializable]
  public struct LayerFade
  {
    public PostFXLayer Layer;
    public float Duration;
    public float Timer;
    public float StartWeight;
    public float EndWeight;
  }

  public void AddLayer(PostFXLayer layer)
  {
    _layers.Add(layer);
    _layers.Sort((a, b) => { return a.Settings.Priority - b.Settings.Priority; });
  }

  public void RemoveLayer(PostFXLayer layer)
  {
    _layers.Remove(layer);
  }

  public void FadeInLayer(PostFXLayer layer, float duration, float targetWeight = 1)
  {
    LayerFade layerFade = new()
    {
      Layer = layer,
      Duration = duration,
      Timer = 0,
      StartWeight = 0,
      EndWeight = targetWeight,
    };

    // If we were already fading out this layer, cancel that and fade in from there
    bool wasFadingOut = false;
    for (int i = 0; i < _fadeOutLayers.Count; ++i)
    {
      var fadeOutLayer = _fadeOutLayers[i];
      if (fadeOutLayer.Layer == layer)
      {
        _fadeOutLayers.RemoveAt(i);
        layerFade.StartWeight = fadeOutLayer.Layer.Weight;
        wasFadingOut = true;
      }
    }

    layer.Weight = layerFade.StartWeight;
    _fadeInLayers.Add(layerFade);

    if (!wasFadingOut)
      AddLayer(layer);
  }

  public void FadeOutLayer(PostFXLayer layer, float duration)
  {
    LayerFade layerFade = new()
    {
      Layer = layer,
      Duration = duration,
      Timer = 0,
      StartWeight = layer.Weight,
      EndWeight = 0,
    };

    // If we were already fading in this layer, cancel that and fade out
    for (int i = 0; i < _fadeInLayers.Count; ++i)
    {
      var fadeInLayer = _fadeInLayers[i];
      if (fadeInLayer.Layer == layer)
      {
        _fadeInLayers.RemoveAt(i);
        layerFade.StartWeight = fadeInLayer.Layer.Weight;
      }
    }

    _fadeOutLayers.Add(layerFade);
  }

  public SimplePostFXDefinition.AllSettings GatherLayerSettings()
  {
#if UNITY_EDITOR
    if (!Application.isPlaying && _layers.Count == 0 && InitialSettings)
    {
      AddInitialLayer();
    }
#endif

    SimplePostFXDefinition.AllSettings allSettings;
    allSettings.Bloom = SimplePostFXDefinition.BloomDefault;
    allSettings.SaturationContrast = SimplePostFXDefinition.SaturationContrastDefault;
    allSettings.WhiteBalance = SimplePostFXDefinition.WhiteBalanceDefault;
    allSettings.ChannelMixers = SimplePostFXDefinition.ChannelMixerDefault;
    allSettings.Blur = SimplePostFXDefinition.BlurDefault;

    for (int i = 0; i < _layers.Count; ++i)
    {
      PostFXLayer layer = _layers[i];
      if (layer.Settings.Bloom.Enabled)
      {
        allSettings.Bloom = SimplePostFXDefinition.Lerp(allSettings.Bloom, layer.Settings.Bloom, layer.Weight);
      }

      if (layer.Settings.SaturationContrast.Enabled)
      {
        allSettings.SaturationContrast = SimplePostFXDefinition.Lerp(allSettings.SaturationContrast, layer.Settings.SaturationContrast, layer.Weight);
      }

      if (layer.Settings.WhiteBalance.Enabled)
      {
        allSettings.WhiteBalance = SimplePostFXDefinition.Lerp(allSettings.WhiteBalance, layer.Settings.WhiteBalance, layer.Weight);
      }

      if (layer.Settings.ChannelMixers.Enabled)
      {
        allSettings.ChannelMixers = SimplePostFXDefinition.Lerp(allSettings.ChannelMixers, layer.Settings.ChannelMixers, layer.Weight);
      }

      if (layer.Settings.Blur.Enabled)
      {
        allSettings.Blur = SimplePostFXDefinition.Lerp(allSettings.Blur, layer.Settings.Blur, layer.Weight);
      }
    }

    return allSettings;
  }

  private void Start()
  {
    if (InitialSettings != null)
    {
      AddInitialLayer();
    }
    else
    {
      enabled = false;
    }
  }

  private void Update()
  {
    for (int i = 0; i < _fadeInLayers.Count; ++i)
    {
      LayerFade fadeInfo = _fadeInLayers[i];
      fadeInfo.Timer += Time.unscaledDeltaTime;
      fadeInfo.Layer.Weight = Mathf.SmoothStep(fadeInfo.StartWeight, fadeInfo.EndWeight, fadeInfo.Timer / fadeInfo.Duration);
      _fadeInLayers[i] = fadeInfo;

      if (fadeInfo.Timer >= fadeInfo.Duration)
      {
        _fadeInLayers.RemoveAt(i);
        --i;
      }
    }

    for (int i = 0; i < _fadeOutLayers.Count; ++i)
    {
      LayerFade fadeInfo = _fadeOutLayers[i];
      fadeInfo.Timer += Time.unscaledDeltaTime;
      fadeInfo.Layer.Weight = Mathf.SmoothStep(fadeInfo.StartWeight, fadeInfo.EndWeight, fadeInfo.Timer / fadeInfo.Duration);
      _fadeOutLayers[i] = fadeInfo;

      if (fadeInfo.Timer >= fadeInfo.Duration)
      {
        RemoveLayer(fadeInfo.Layer);
        _fadeOutLayers.RemoveAt(i);
        --i;
      }
    }
  }

  [ContextMenu("Add Initial Layer")]
  private void AddInitialLayer()
  {
    AddLayer(new PostFXLayer(InitialSettings, 1));
  }
}