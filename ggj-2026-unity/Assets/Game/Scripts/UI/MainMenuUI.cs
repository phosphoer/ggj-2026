using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : UIPageBase
{
  [SerializeField] private MenuItemUI _buttonNewSinglePlayerGame = null;
  [SerializeField] private MenuItemUI _buttonQuit = null;

  protected override void Awake()
  {
    base.Awake();
    _buttonNewSinglePlayerGame.Activated += OnPlayClicked;
    _buttonQuit.Activated += OnQuitGameClicked;
  }

  public void OnPlayClicked()
  {
    GameController.Instance.SetGameStage(GameController.GameStage.WaitingForPlayers);
  }

  public void OnQuitGameClicked()
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
}