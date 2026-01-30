using UnityEngine;
using UnityEngine.Rendering.Universal;

public class UIRenderCompositor : MonoBehaviour
{
  public RenderTexture RenderTexture => _uiRenderTex;

  [SerializeField] private Camera _camera = null;
  [SerializeField] private ReferenceAsset<RenderTexture> _textureAsset;

  private RenderTexture _uiRenderTex;
  private int _lastUIWidth;
  private int _lastUIHeight;

  private void Update()
  {
    if (_lastUIWidth != Screen.width || _lastUIHeight != Screen.height)
      UpdateRenderTexture();
  }

  private void UpdateRenderTexture()
  {
    _lastUIWidth = Screen.width;
    _lastUIHeight = Screen.height;

    Debug.LogFormat($"Creating UI texture with resolution {_lastUIWidth}x{_lastUIHeight}");

    if (_uiRenderTex != null)
    {
      _uiRenderTex.Release();
      _uiRenderTex.width = Screen.width;
      _uiRenderTex.height = Screen.height;
      _uiRenderTex.Create();

      var oldTex = RenderTexture.active;
      RenderTexture.active = _uiRenderTex;
      GL.Clear(true, true, Color.clear);
      RenderTexture.active = oldTex;

      return;
    }

    _uiRenderTex = new(Screen.width, Screen.height, 24, RenderTextureFormat.DefaultHDR);
    _uiRenderTex.Create();
    _textureAsset.Value = _uiRenderTex;

    _camera.targetTexture = _uiRenderTex;
  }
}