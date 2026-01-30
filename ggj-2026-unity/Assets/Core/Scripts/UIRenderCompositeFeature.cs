using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class UIRenderCompositeFeature : ScriptableRendererFeature
{
  [SerializeField] private Shader _shader = null;
  [SerializeField] private ReferenceAsset<RenderTexture> _renderTextureAsset;

  private Material _material;
  private UIRenderCompositePass _renderPass;

  public override void Create()
  {
    if (!_shader)
      return;

    _material = new Material(_shader);
    _material.hideFlags = HideFlags.DontSave;

    _renderPass = new UIRenderCompositePass(_material, _renderTextureAsset);
    _renderPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
  }

  public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
  {
    if (_renderPass == null)
      return;

    bool isEnabled = renderingData.cameraData.cameraType == CameraType.Game;
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

public class UIRenderCompositePass : ScriptableRenderPass
{
  private Material _material;
  private ReferenceAsset<RenderTexture> _textureAsset;

  class CompositePassData
  {
    public Material material;
    public TextureHandle sourceTexture;
    public RenderTexture compositeTexture;
  }

  public UIRenderCompositePass(Material material, ReferenceAsset<RenderTexture> textureAsset)
  {
    _material = material;
    _textureAsset = textureAsset;
    requiresIntermediateTexture = true;
  }

  public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
  {
    UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
    UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

    // The following line ensures that the render pass doesn't blit
    // from the back buffer.
    if (resourceData.isActiveTargetBackBuffer)
      return;

    // This check is to avoid an error from the material preview in the scene
    TextureHandle cameraSourceTex = resourceData.activeColorTexture;
    TextureDesc cameraSourceDesc = cameraSourceTex.GetDescriptor(renderGraph);
    if (!cameraSourceTex.IsValid() || _textureAsset.Value == null)
      return;

    // Final texture to render to
    TextureDesc finalTexDesc = cameraSourceDesc;
    finalTexDesc.name = "UI Composite Tex";
    finalTexDesc.depthBufferBits = 0;
    TextureHandle finalTexture = renderGraph.CreateTexture(finalTexDesc);

    using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>("UI Composite", out var passData))
    {
      passData.material = _material;
      passData.sourceTexture = resourceData.cameraColor;
      passData.compositeTexture = _textureAsset.Value;

      builder.UseTexture(passData.sourceTexture, AccessFlags.Read);
      builder.SetRenderAttachment(finalTexture, 0, AccessFlags.Write);
      builder.SetRenderFunc((CompositePassData data, RasterGraphContext context) =>
      {
        data.material.SetTexture("_UITex", data.compositeTexture);
        Blitter.BlitTexture(context.cmd, passData.sourceTexture, new Vector4(1, 1, 0, 0), data.material, 0);
      });
    }

    resourceData.cameraColor = finalTexture;
  }
}

