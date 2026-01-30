using UnityEngine;
using System.Collections.Generic;

public abstract class InputIconRendererBase : MonoBehaviour
{
  public IReadOnlyList<InputIcon> InputIcons => _inputIcons;

  [SerializeField, Rewired.ActionIdProperty(typeof(RewiredConsts.Action))]
  protected int _actionId = 0;

  [SerializeField] private ControllerIconMapDefinition _controllerMap = null;

  protected List<InputIcon> _inputIcons = new List<InputIcon>();
  protected string _actionDescription;

  public void SetAction(int actionId)
  {
    _actionId = actionId;
    GetActionIcons();
  }

  private void OnEnable()
  {
    PlayerMenuInput.PlayerControllerChanged += OnPlayerControllerChanged;
    GetActionIcons();
  }

  private void OnDisable()
  {
    PlayerMenuInput.PlayerControllerChanged -= OnPlayerControllerChanged;
  }

  private void OnPlayerControllerChanged()
  {
    GetActionIcons();
  }

  protected abstract void RefreshIconDisplay();

  protected void GetActionIcons()
  {
    if (!Rewired.ReInput.isReady)
      return;

    Rewired.InputAction action = Rewired.ReInput.mapping.GetAction(_actionId);
    _actionDescription = action.descriptiveName;

    _inputIcons.Clear();
    _controllerMap.GetIconsForAction(_actionId, Rewired.ReInput.players.Players[0], _inputIcons);

    if (_inputIcons.Count > 0)
      RefreshIconDisplay();
  }
}