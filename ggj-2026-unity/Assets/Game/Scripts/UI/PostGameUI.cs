
using UnityEngine;

public class PostGameUI : UIPageBase
{
  [SerializeField] private RectTransform _menuRectTransform = null;
  [SerializeField] private MenuItemUI _buttonOk = null;

  public TMPro.TMP_Text WinLabel;
  public float SlideInDuration = 1;

  private float _initialYPosition;
  private float _slideTimer;

  private enum eAnimationState
  {
    slidingIn,
    ready
  }
  private eAnimationState _state; 

  protected override void Awake()
  {
    base.Awake();
    _initialYPosition= _menuRectTransform.localPosition.y;

    Shown += OnShown;
    _buttonOk.Activated += OnOkClicked;
  }

  protected void Update()
  {
    float dt= Time.deltaTime;

    if (_state == eAnimationState.slidingIn)
    {
      _slideTimer+= dt;

      float u = Mathf.Clamp01(_slideTimer / SlideInDuration);
      float targetY= Mathf.Lerp(_initialYPosition, 0, u);

      SetMenuYPosition(targetY);

      if (_slideTimer >= SlideInDuration)
      {
        _buttonOk.SetDisabled(false);
        _state = eAnimationState.ready;
      }
    }
  }

  public void OnOkClicked()
  {
    GameController.Instance.SetGameStage(GameController.GameStage.MainMenu);
  }

  private void OnShown()
  {
    int playerIndex= GameController.Instance.WinningPlayerID;

    if (playerIndex >= 0)
    {
      //string colorName = GameController.Instance.GetPlayerColorName(playerIndex);
      //WinLabel.text = string.Format("{0} Player Wins!",colorName);
      WinLabel.text = string.Format("Player {0} Wins!",playerIndex+1);
    }
    else
    {
      WinLabel.text = string.Format("No players survived!");
    }

    _state= eAnimationState.slidingIn;
    _slideTimer= 0;
    SetMenuYPosition(_initialYPosition);

    _buttonOk.SetDisabled(true);
  }

  private void SetMenuYPosition(float newYPosition)
  {
    _menuRectTransform.localPosition = new Vector3(_menuRectTransform.localPosition.x, newYPosition, _menuRectTransform.localPosition.z);
  }
}
