using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class InputIcon
{
  public string Comment;
  public int InputId;
  public Sprite IconSprite;
  public Color IconColor = Color.white;
  public string IconLabel;
  public Sprite IconLabelSprite;
  public Color IconLabelColor = Color.white;
  public Vector3 IconLabelOffset;
  public bool FlipX;
  public bool DisableMapping;
}

[CreateAssetMenu(fileName = "new-input-icon-mapping", menuName = "Input Icon Mapping")]
public class InputIconMapDefinition : ScriptableObject
{
  public bool UseTemplateIds = true;

  public InputIcon TiltIcon => _tiltIcon;

  [SerializeField] private InputIcon[] _inputIcons = null;
  [SerializeField] private ActionIconOverride[] _actionIconOverrides = null;
  [SerializeField] private InputIcon _tiltIcon = null;

#pragma warning disable CS0414
  [SerializeField] private Rewired.Data.Mapping.HardwareJoystickMap _hardwareMap = null;
#pragma warning restore CS0414

  private static List<Rewired.ControllerTemplateElementTarget> _elementTargets = new List<Rewired.ControllerTemplateElementTarget>();

  [System.Serializable]
  private struct ActionIconOverride
  {
    [Rewired.ActionIdProperty(typeof(RewiredConsts.Action))]
    public int ActionId;

    public InputIcon InputIcon;
  }

  // Get the id of the corresponding input element from the controller's template definition, if it exists
  public static int GetTemplatedInputId(Rewired.ActionElementMap elementMap)
  {
    Rewired.Controller controller = elementMap.controllerMap.controller;
    int inputId = elementMap.elementIdentifierId;
    if (controller.templateCount > 0)
    {
      _elementTargets.Clear();
      controller.Templates[0].GetElementTargets(elementMap, _elementTargets);
      if (_elementTargets.Count > 0)
      {
        inputId = _elementTargets[0].element.id;
      }
    }

    return inputId;
  }

  public InputIcon GetInputIcon(Rewired.ActionElementMap elementMap, Rewired.Controller controller)
  {
    int inputId = UseTemplateIds ? GetTemplatedInputId(elementMap) : elementMap.elementIdentifierId;
    return GetInputIcon(inputId, elementMap.actionId);
  }

  public InputIcon GetInputIcon(int inputId, int actionId)
  {
    if (_actionIconOverrides != null)
    {
      for (int i = 0; i < _actionIconOverrides.Length; ++i)
      {
        if (_actionIconOverrides[i].ActionId == actionId)
        {
          return _actionIconOverrides[i].InputIcon;
        }
      }
    }

    return GetInputIcon(inputId);
  }

  public InputIcon GetInputIcon(int inputId)
  {
    foreach (InputIcon inputIcon in _inputIcons)
    {
      if (inputIcon.InputId == inputId)
      {
        return inputIcon;
      }
    }

    return null;
  }

#if UNITY_EDITOR
  [ContextMenu("Load From Hardware Map")]
  private void LoadFromHardwareMap()
  {
    var oldInputIcons = _inputIcons;

    UnityEditor.Undo.RecordObject(this, "Load from hardware map");

    int index = 0;
    _inputIcons = new InputIcon[_hardwareMap.elementIdentifierCount];
    foreach (var elementIdentifier in _hardwareMap.ElementIdentifiers)
    {
      var icon = new InputIcon();
      icon.InputId = elementIdentifier.id;
      icon.Comment = elementIdentifier.name;
      icon.IconColor = Color.white;
      icon.IconLabelColor = Color.white;
      _inputIcons[index] = icon;
      ++index;
    }

    for (int i = 0; i < oldInputIcons.Length && i < _inputIcons.Length; ++i)
    {
      if (oldInputIcons[i].InputId == _inputIcons[i].InputId)
      {
        _inputIcons[i].IconSprite = oldInputIcons[i].IconSprite;
        _inputIcons[i].IconLabel = oldInputIcons[i].IconLabel;
        _inputIcons[i].IconLabelSprite = oldInputIcons[i].IconLabelSprite;
      }
    }

    UnityEditor.EditorUtility.SetDirty(this);
  }
#endif

  [ContextMenu("Generate Keyboard Map")]
  private void GenerateKeyboardMap()
  {
    Rewired.Keyboard keyboard = Rewired.ReInput.controllers.Keyboard;

    InputIcon firstIcon = _inputIcons[0];
    _inputIcons = new InputIcon[keyboard.ElementIdentifiers.Count];

    for (int i = 0; i < keyboard.ElementIdentifiers.Count; ++i)
    {
      var keyElement = keyboard.ElementIdentifiers[i];

      InputIcon keyIcon = new InputIcon();
      keyIcon.IconSprite = firstIcon.IconSprite;
      keyIcon.IconColor = firstIcon.IconColor;
      keyIcon.IconLabelColor = firstIcon.IconLabelColor;
      keyIcon.Comment = keyElement.name;
      keyIcon.IconLabel = keyElement.name;
      keyIcon.InputId = keyElement.id;
      _inputIcons[i] = keyIcon;
    }
  }

  [ContextMenu("Generate Mouse Map")]
  private void GenerateMouseMap()
  {
    Rewired.Mouse mouse = Rewired.ReInput.controllers.Mouse;

    InputIcon firstIcon = _inputIcons[0];
    _inputIcons = new InputIcon[mouse.ElementIdentifiers.Count];

    for (int i = 0; i < mouse.ElementIdentifiers.Count; ++i)
    {
      var keyElement = mouse.ElementIdentifiers[i];

      InputIcon keyIcon = new InputIcon();
      keyIcon.IconSprite = firstIcon.IconSprite;
      keyIcon.IconColor = firstIcon.IconColor;
      keyIcon.IconLabelColor = firstIcon.IconLabelColor;
      keyIcon.Comment = keyElement.name;
      keyIcon.IconLabel = keyElement.name;
      keyIcon.InputId = keyElement.id;
      _inputIcons[i] = keyIcon;
    }
  }

  [ContextMenu("Apply Item 0 Icon Color")]
  private void DebugAdjustColors()
  {
    for (int i = 1; i < _inputIcons.Length; ++i)
    {
      _inputIcons[i].IconColor = _inputIcons[0].IconColor;
    }
  }


  [ContextMenu("Apply Item 0 Icon")]
  private void DebugAdjustIcons()
  {
    for (int i = 1; i < _inputIcons.Length; ++i)
    {
      _inputIcons[i].IconSprite = _inputIcons[0].IconSprite;
    }
  }
}