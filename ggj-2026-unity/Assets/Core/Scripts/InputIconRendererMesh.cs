using UnityEngine;
using TMPro;

public class InputIconRendererMesh : InputIconRendererBase
{
  [SerializeField] private Renderer _renderer = null;
  [SerializeField] private Renderer _rendererLabel = null;
  [SerializeField] private TMP_Text _iconText = null;
  [SerializeField] private string _materialTextureProperty = "_MainTex";
  [SerializeField] private float _offsetScale = 0.01f;

  private Material _materialInstance;
  private Material _materialLabelInstance;

  protected override void RefreshIconDisplay()
  {
    _materialInstance.SetTexture(_materialTextureProperty, _inputIcons[0].IconSprite.texture);
    _materialInstance.color = _inputIcons[0].IconColor;
    _materialLabelInstance.color = _inputIcons[0].IconLabelColor;
    _rendererLabel.enabled = _inputIcons[0].IconLabelSprite != null;

    if (_inputIcons[0].IconLabelSprite != null)
      _materialLabelInstance.SetTexture(_materialTextureProperty, _inputIcons[0].IconLabelSprite.texture);

    if (_iconText != null)
    {
      _iconText.transform.localPosition = _inputIcons[0].IconLabelOffset * _offsetScale;
      _iconText.text = _inputIcons[0].IconLabel;
      _iconText.color = _inputIcons[0].IconLabelColor;
    }
  }

  private void Awake()
  {
    _materialInstance = Instantiate(_renderer.sharedMaterial);
    _materialLabelInstance = Instantiate(_rendererLabel.sharedMaterial);
    _renderer.sharedMaterial = _materialInstance;
    _rendererLabel.sharedMaterial = _materialLabelInstance;
  }

  private void OnDestroy()
  {
    Destroy(_materialInstance);
    Destroy(_materialLabelInstance);
  }
}