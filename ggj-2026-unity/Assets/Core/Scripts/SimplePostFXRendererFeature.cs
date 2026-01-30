using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class SimplePostFXRendererFeature : ScriptableRendererFeature
{
  [SerializeField] private Shader _shader = null;

  private Material _material;
  private SimplePostFXRenderPass _renderPass;

  public override void Create()
  {
    if (!_shader)
      return;

    _material = new Material(_shader);
    _material.hideFlags = HideFlags.DontSave;

    _renderPass = new SimplePostFXRenderPass(_material);
    _renderPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
  }

  public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
  {
    if (_renderPass == null)
      return;

    bool isEnabled = renderingData.cameraData.cameraType == CameraType.Game;

#if UNITY_EDITOR
    isEnabled |= renderingData.cameraData.cameraType == CameraType.SceneView && UnityEditor.SceneView.currentDrawingSceneView.sceneViewState.showImageEffects;
#endif

    if (isEnabled)
    {
      renderer.EnqueuePass(_renderPass);
    }
  }

  protected override void Dispose(bool disposing)
  {
    if (Application.isPlaying)
    {
      Destroy(_material);
      _renderPass = null;
    }
    else
    {
      DestroyImmediate(_material);
      _renderPass = null;
    }
  }
}

public class SimplePostFXRenderPass : ScriptableRenderPass
{
  private Material _material;
  private TextureHandle[] _bloomFilterDown;
  private TextureHandle[] _bloomFilterUp;
  private TextureHandle[] _blurFilterDown;
  private TextureHandle[] _blurFilterUp;
  private SimplePostFX _editorPostFX;

  private const int kPassBloomPrefilter = 0;
  private const int kPassBloomDown = 1;
  private const int kPassBloomUp = 2;
  private const int kPassBlurDown = 3;
  private const int kPassBlurUp = 4;
  private const int kPassFinalBlur = 5;
  private const int kPassFinal = 6;

  private static readonly int kMatBlurTex = Shader.PropertyToID("_BlurTex");
  private static readonly int kMatBloomTex = Shader.PropertyToID("_BloomTex");
  private static readonly int kMatBloomParams = Shader.PropertyToID("_BloomParams");
  private static readonly int kMatBlurParams = Shader.PropertyToID("_BlurParams");
  private static readonly int kMatColorParams = Shader.PropertyToID("_ColorParams");
  private static readonly int kMatColorBrightness = Shader.PropertyToID("_ColorBrightness");
  private static readonly int kMatChannelMixerRed = Shader.PropertyToID("_ChannelMixerRed");
  private static readonly int kMatChannelMixerGreen = Shader.PropertyToID("_ChannelMixerGreen");
  private static readonly int kMatChannelMixerBlue = Shader.PropertyToID("_ChannelMixerBlue");

  class MipPassData
  {
    public TextureHandle source;
    public TextureHandle destination;
    public Material material;
    public int shaderPass;
  }

  class CompositePassData
  {
    public TextureHandle source;
    public TextureHandle composite;
    public Material material;
    public bool compositeEnabled;
  }

  class CopyPassData
  {
    public TextureHandle source;
  }

  public SimplePostFXRenderPass(Material material)
  {
    _material = material;
    requiresIntermediateTexture = true;
  }

  public void ApplySettings(SimplePostFXDefinition.AllSettings settings, Material targetMaterial)
  {
    Vector4 bloomParams = new Vector4(
      settings.Bloom.BloomFilterSize,
      settings.Bloom.BloomThreshold,
      settings.Bloom.BloomIntensity,
      settings.Bloom.BloomThresholdSoft);

    Vector4 blurParams = new Vector4(settings.Blur.FilterSize, 0, 0, settings.Blur.Opacity);
    Vector4 colorParams = new Vector4(
      settings.SaturationContrast.ColorSaturation,
      settings.SaturationContrast.ColorContrast,
      settings.WhiteBalance.ColorTemperature,
      settings.WhiteBalance.ColorTint);

    targetMaterial.SetVector(kMatBloomParams, bloomParams);
    targetMaterial.SetVector(kMatBlurParams, blurParams);
    targetMaterial.SetVector(kMatColorParams, colorParams);
    targetMaterial.SetFloat(kMatColorBrightness, settings.SaturationContrast.ColorBrightness);
    targetMaterial.SetVector(kMatChannelMixerRed, settings.ChannelMixers.ChannelMixerRed);
    targetMaterial.SetVector(kMatChannelMixerGreen, settings.ChannelMixers.ChannelMixerGreen);
    targetMaterial.SetVector(kMatChannelMixerBlue, settings.ChannelMixers.ChannelMixerBlue);
  }

  public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
  {
    UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
    UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
    SimplePostFX postFx = cameraData.camera.GetComponent<SimplePostFX>();

#if UNITY_EDITOR
    if (cameraData.cameraType == CameraType.SceneView && UnityEditor.SceneView.currentDrawingSceneView.sceneViewState.showImageEffects)
    {
      if (!_editorPostFX)
      {
        SimplePostFX[] postFxList = GameObject.FindObjectsByType<SimplePostFX>(FindObjectsSortMode.None);
        foreach (var postfx in postFxList)
        {
          if (postfx.IsPrimary)
            _editorPostFX = postfx;
        }
      }

      if (_editorPostFX)
        postFx = _editorPostFX;
    }
#endif

    if (!postFx)
      return;

    // The following line ensures that the render pass doesn't blit
    // from the back buffer.
    if (resourceData.isActiveTargetBackBuffer)
      return;

    if (_bloomFilterDown == null || _bloomFilterDown.Length != postFx.BloomIterations)
    {
      _bloomFilterDown = new TextureHandle[postFx.BloomIterations];
      _bloomFilterUp = new TextureHandle[postFx.BloomIterations];
    }

    if (_blurFilterDown == null || _blurFilterDown.Length != postFx.BlurIterations)
    {
      _blurFilterDown = new TextureHandle[postFx.BlurIterations];
      _blurFilterUp = new TextureHandle[postFx.BlurIterations];
    }

    // This check is to avoid an error from the material preview in the scene
    TextureHandle cameraSourceTex = resourceData.activeColorTexture;
    TextureDesc cameraSourceDesc = cameraSourceTex.GetDescriptor(renderGraph);
    if (!cameraSourceTex.IsValid())
      return;

    // Get settings from post fx component
    var postFxSettings = postFx.GatherLayerSettings();

    // Create bloom mips
    if (postFxSettings.Bloom.Enabled)
    {
      int bloomMipCount = 0;
      TextureDesc bloomMipDesc = cameraSourceDesc;
      bloomMipDesc.depthBufferBits = 0;
      bloomMipDesc.clearColor = Color.black;
      TextureHandle bloomSource = cameraSourceTex;
      for (int i = 0; i < postFx.BloomIterations && bloomMipDesc.width > 2 && bloomMipDesc.height > 2; ++i)
      {
        bloomMipDesc.width = Mathf.Max(2, cameraSourceDesc.width >> (i + 1));
        bloomMipDesc.height = Mathf.Max(2, cameraSourceDesc.height >> (i + 1));
        bloomMipDesc.name = $"PostFX Bloom Down Mip {i}";
        _bloomFilterDown[i] = renderGraph.CreateTexture(bloomMipDesc);
        bloomMipCount += 1;

        using (var builder = renderGraph.AddRasterRenderPass<MipPassData>($"PostFX Bloom Mip Down {i}", out var passData))
        {
          passData.source = bloomSource;
          passData.destination = _bloomFilterDown[i];
          passData.material = _material;
          passData.shaderPass = i == 0 ? kPassBloomPrefilter : kPassBloomDown;
          bloomSource = passData.destination;

          builder.UseTexture(passData.source, AccessFlags.Read);
          builder.SetRenderAttachment(passData.destination, 0, AccessFlags.Write);
          builder.SetRenderFunc((MipPassData data, RasterGraphContext context) =>
          {
            ApplySettings(postFxSettings, data.material);
            Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, data.shaderPass);
          });
        }
      }

      // Now create the bloom mips going upwards
      for (int i = bloomMipCount - 2; i >= 0; --i)
      {
        bloomMipDesc.width = Mathf.Max(2, cameraSourceDesc.width >> (i + 1));
        bloomMipDesc.height = Mathf.Max(2, cameraSourceDesc.height >> (i + 1));
        bloomMipDesc.name = $"PostFX Bloom Up Mip {i}";
        _bloomFilterUp[i] = renderGraph.CreateTexture(bloomMipDesc);

        using (var builder = renderGraph.AddRasterRenderPass<MipPassData>($"PostFX Bloom Mip Up {i}", out var passData))
        {
          passData.source = bloomSource;
          passData.destination = _bloomFilterUp[i];
          passData.material = _material;
          passData.shaderPass = kPassBloomUp;
          bloomSource = passData.destination;

          builder.UseTexture(passData.source, AccessFlags.Read);
          builder.SetRenderAttachment(passData.destination, 0, AccessFlags.Write);
          builder.SetRenderFunc((MipPassData data, RasterGraphContext context) =>
          {
            ApplySettings(postFxSettings, data.material);
            Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, data.shaderPass);
          });
        }
      }
    }

    // Intermediate texture to render to
    TextureDesc intermediateTexDesc = cameraSourceDesc;
    intermediateTexDesc.name = "PostFX Intermediate Tex";
    intermediateTexDesc.depthBufferBits = 0;
    TextureHandle intermediateTexture = renderGraph.CreateTexture(intermediateTexDesc);

    // Final texture to render to
    TextureDesc finalTexDesc = cameraSourceDesc;
    finalTexDesc.name = "PostFX Final Tex";
    finalTexDesc.depthBufferBits = 0;
    TextureHandle finalTexture = renderGraph.CreateTexture(finalTexDesc);

    // Main post FX pass 
    using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>("PostFX Main Pass", out var passData))
    {
      passData.source = resourceData.activeColorTexture;
      passData.material = _material;
      passData.composite = _bloomFilterUp[0];
      passData.compositeEnabled = postFxSettings.Bloom.Enabled;

      builder.UseTexture(passData.source, AccessFlags.Read);

      if (passData.compositeEnabled)
        builder.UseTexture(passData.composite, AccessFlags.Read);

      builder.SetRenderAttachment(intermediateTexture, 0, AccessFlags.Write);
      builder.SetRenderFunc((CompositePassData data, RasterGraphContext context) =>
      {
        if (data.compositeEnabled)
          data.material.SetTexture(kMatBloomTex, data.composite);

        ApplySettings(postFxSettings, data.material);
        Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), _material, kPassFinal);
      });
    }

    // Create blur mips
    if (postFxSettings.Blur.Enabled)
    {
      int blurMipCount = 0;
      TextureDesc blurMipDesc = cameraSourceDesc;
      blurMipDesc.depthBufferBits = 0;
      blurMipDesc.clearColor = Color.black;
      TextureHandle blurSource = intermediateTexture;
      for (int i = 0; i < postFx.BlurIterations && blurMipDesc.width > 2 && blurMipDesc.height > 2; ++i)
      {
        blurMipDesc.width = Mathf.Max(2, cameraSourceDesc.width >> (i + 1));
        blurMipDesc.height = Mathf.Max(2, cameraSourceDesc.height >> (i + 1));
        blurMipDesc.name = $"Blur Down Mip {i}";
        _blurFilterDown[i] = renderGraph.CreateTexture(blurMipDesc);
        blurMipCount += 1;

        using (var builder = renderGraph.AddRasterRenderPass<MipPassData>($"Blur Mip Down {i}", out var passData))
        {
          passData.source = blurSource;
          passData.destination = _blurFilterDown[i];
          passData.material = _material;
          passData.shaderPass = kPassBlurDown;
          blurSource = passData.destination;

          builder.UseTexture(passData.source, AccessFlags.Read);
          builder.SetRenderAttachment(passData.destination, 0, AccessFlags.Write);
          builder.SetRenderFunc((MipPassData data, RasterGraphContext context) =>
          {
            ApplySettings(postFxSettings, data.material);
            Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, data.shaderPass);
          });
        }
      }

      // Blur up mips
      for (int i = blurMipCount - 2; i >= 0; --i)
      {
        blurMipDesc.width = Mathf.Max(2, cameraSourceDesc.width >> (i + 1));
        blurMipDesc.height = Mathf.Max(2, cameraSourceDesc.height >> (i + 1));
        blurMipDesc.name = $"Blur Up Mip {i}";
        _blurFilterUp[i] = renderGraph.CreateTexture(blurMipDesc);

        using (var builder = renderGraph.AddRasterRenderPass<MipPassData>($"Blur Mip Up {i}", out var passData))
        {
          passData.source = blurSource;
          passData.destination = _blurFilterUp[i];
          passData.material = _material;
          passData.shaderPass = kPassBlurUp;
          blurSource = passData.destination;

          builder.UseTexture(passData.source, AccessFlags.Read);
          builder.SetRenderAttachment(passData.destination, 0, AccessFlags.Write);
          builder.SetRenderFunc((MipPassData data, RasterGraphContext context) =>
          {
            ApplySettings(postFxSettings, data.material);
            Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, data.shaderPass);
          });
        }
      }

      // Blur composite pass
      using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>("PostFX Final Composite Blit", out var passData))
      {
        passData.source = intermediateTexture;
        passData.material = _material;
        passData.composite = _blurFilterUp[0];

        builder.UseTexture(passData.source, AccessFlags.Read);
        builder.UseTexture(passData.composite, AccessFlags.Read);
        builder.SetRenderAttachment(finalTexture, 0, AccessFlags.Write);
        builder.SetRenderFunc((CompositePassData data, RasterGraphContext context) =>
        {
          data.material.SetTexture(kMatBlurTex, data.composite);
          ApplySettings(postFxSettings, data.material);
          Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), _material, kPassFinalBlur);
        });
      }
    }
    // If no blur, just copy the intermediate buffer back to the camera
    else
    {
      using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("PostFX Final Blit", out var passData))
      {
        passData.source = intermediateTexture;

        builder.UseTexture(passData.source, AccessFlags.Read);
        builder.SetRenderAttachment(finalTexture, 0, AccessFlags.Write);
        builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) =>
        {
          ApplySettings(postFxSettings, _material);
          Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), 0, true);
        });
      }
    }

    resourceData.cameraColor = finalTexture;
  }
}
