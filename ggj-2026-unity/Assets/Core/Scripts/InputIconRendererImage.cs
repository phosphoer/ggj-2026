using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InputIconRendererImage : InputIconRendererBase
{
  [SerializeField] private Image _iconImage = null;
  [SerializeField] private Image _iconTextImage = null;
  [SerializeField] private TMPro.TMP_Text _iconText = null;
  [SerializeField] private TMPro.TMP_Text _actionLabelText = null;

  protected override void RefreshIconDisplay()
  {
    if (_inputIcons[0] == null)
    {
      Debug.LogWarning($"Got null input icon for {_actionDescription}, id {_actionId}", gameObject);
      return;
    }

    _iconImage.sprite = _inputIcons[0].IconSprite;
    _iconImage.color = _inputIcons[0].IconColor;

    if (_iconTextImage)
    {
      _iconTextImage.sprite = _inputIcons[0].IconLabelSprite;
      _iconTextImage.color = _inputIcons[0].IconLabelColor;
      _iconTextImage.enabled = _iconTextImage.sprite != null;
    }

    if (_iconText)
    {
      _iconText.transform.localPosition = _inputIcons[0].IconLabelOffset;
      _iconText.enabled = !string.IsNullOrEmpty(_inputIcons[0].IconLabel);
      _iconText.text = _inputIcons[0].IconLabel;
      _iconText.color = _inputIcons[0].IconLabelColor;
      _iconText.gameObject.SetActive(!string.IsNullOrEmpty(_iconText.text));
    }

    if (_actionLabelText != null)
    {
      _actionLabelText.text = _actionDescription;
    }
  }
}