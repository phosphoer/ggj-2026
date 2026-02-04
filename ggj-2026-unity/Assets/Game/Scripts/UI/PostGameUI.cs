
using UnityEngine;

public class PostGameUI : UIPageBase
{
  [SerializeField] private RectTransform _menuRectTransform = null;
  [SerializeField] private MenuItemUI _buttonRestart = null;
  [SerializeField] private MenuItemUI _buttonQuit = null;

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
    _initialYPosition = _menuRectTransform.localPosition.y;

    Shown += OnShown;
    _buttonRestart.Activated += OnRestartClicked;
    _buttonQuit.Activated += OnQuitClicked;
  }

  protected void Update()
  {
    float dt = Time.deltaTime;

    if (_state == eAnimationState.slidingIn)
    {
      _slideTimer += dt;

      float u = Mathf.Clamp01(_slideTimer / SlideInDuration);
      float targetY = Mathf.Lerp(_initialYPosition, 0, u);

      SetMenuYPosition(targetY);

      if (_slideTimer >= SlideInDuration)
      {
        _buttonRestart.SetDisabled(false);
        _buttonQuit.SetDisabled(false);
        _state = eAnimationState.ready;
      }
    }
  }

  public void OnRestartClicked()
  {
    UnityEngine.SceneManagement.SceneManager.LoadScene(0, UnityEngine.SceneManagement.LoadSceneMode.Single);
  }

  public void OnQuitClicked()
  {
    //If we are running in a standalone build of the game
#if UNITY_STANDALONE
    //Quit the application
    Application.Quit();
#endif

    //If we are running in the editor
#if UNITY_EDITOR
    //Stop playing the scene
    UnityEditor.EditorApplication.isPlaying = false;
#endif
  }

  private void OnShown()
  {
    _state = eAnimationState.slidingIn;
    _slideTimer = 0;
    SetMenuYPosition(_initialYPosition);

    _buttonRestart.SetDisabled(true);
    _buttonQuit.SetDisabled(true);
  }

  private void SetMenuYPosition(float newYPosition)
  {
    _menuRectTransform.localPosition = new Vector3(_menuRectTransform.localPosition.x, newYPosition, _menuRectTransform.localPosition.z);
  }
}
