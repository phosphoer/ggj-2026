using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct PlayerColors
{
  public string ColorName;
}

public class GameController : Singleton<GameController>
{
  public event System.Action MatchStarted;

  public eGameState GameState => _currentGameState;
  public LevelGenerator LevelManager => _levelManager;
  public List<PlayerActorController> SpawnedPlayers => _spawnedPlayers;

  public int WinningPlayerID { get; set; } = -1;
  public float WinningPlayerCountdownTimer { get; set; } = 0;

  public SoundBank MusicTitle;
  public SoundBank MusicGame;
  public SoundBank MusicEnd;
  public int WinCountDownTime = 10;
  public float StartMatchHeight = 5;
  public float SPTopSpeedupThreshold = 0.25f;

  [SerializeField] private eGameState _initialGameState = eGameState.Game;
  [SerializeField] private LevelGenerator _levelManager;
  [SerializeField] private LevelCameraController _cameraController;
  [SerializeField] private PlayerActorController _playerPrefab;
  [SerializeField] private PlayerColors[] _playerColors = null;
  [SerializeField] private AnimationCurve _riseRateCurve = default;

  private bool _isMatchStarted;
  private bool _isInCountdown;
  private bool _isSpawningAllowed;
  private float _extraRiseRate;
  private List<PlayerActorController> _spawnedPlayers = new List<PlayerActorController>();
  private eGameState _currentGameState = eGameState.None;

  public enum eGameState
  {
    None,
    Intro,
    Game,
    PostGame
  }

  public string GetPlayerColorName(int playerID)
  {
    foreach (var player in _spawnedPlayers)
    {
      if (player.PlayerIndex == playerID)
      {
        return player.PlayerColorName;
      }
    }

    return "";
  }

  public bool IsPlayerJoined(int playerId)
  {
    for (int i = 0; i < _spawnedPlayers.Count; ++i)
    {
      if (_spawnedPlayers[i].PlayerInput.id == playerId)
        return true;
    }

    return false;
  }

  private void Awake()
  {
    Instance = this;
  }

  private void Start()
  {
    Application.targetFrameRate = 60;

    SetGameState(_initialGameState);
  }

  private void Update()
  {
#if UNITY_EDITOR
    if (Input.GetKeyDown(KeyCode.P))
    {
      TriggerPostGame();
    }

    if (Input.GetKeyDown(KeyCode.L))
    {
      StartMatch();
    }
#endif

    // Win count down
    if (_isInCountdown)
    {
      WinningPlayerCountdownTimer -= Time.deltaTime;
      if (WinningPlayerCountdownTimer <= 0)
      {
        _isInCountdown = false;
        TriggerPostGame();
      }
    }

    for (int i = 0; i < _spawnedPlayers.Count; ++i)
    {
      if (!_isMatchStarted)
      {
        if (_spawnedPlayers[i].transform.position.y > StartMatchHeight)
        {
          StartMatch();
        }
      }
    }

    // Iterate over existing rewired players and spawn their character if they press a button
    if (_isSpawningAllowed && !MenuFocus.AnyFocusTaken)
    {
      for (int i = 0; i < Rewired.ReInput.players.playerCount; ++i)
      {
        Rewired.Player player = Rewired.ReInput.players.GetPlayer(i);
        if (!IsPlayerJoined(i) && player.GetAnyButtonDown())
        {
          SpawnPlayerAtSpawnPoint(player.id);
        }
      }
    }
  }

  public void SetGameState(eGameState newState)
  {
    if (newState != _currentGameState)
    {
      OnExitState(_currentGameState);
      OnEnterState(newState);

      _currentGameState = newState;
    }
  }

  void OnEnterState(eGameState newState)
  {
    switch (newState)
    {
    case eGameState.Intro:
      ShowUI<MainMenuUI>();
      AudioManager.Instance.PlaySound(MusicTitle);
      break;
    case eGameState.Game:
      SpawnLevel();
      AudioManager.Instance.PlaySound(MusicGame);
      break;
    case eGameState.PostGame:
      ShowUI<PostGameUI>();
      AudioManager.Instance.PlaySound(MusicEnd);
      break;
    }
  }

  void OnExitState(eGameState oldState)
  {
    switch (oldState)
    {
    case eGameState.Intro:
      HideUI<MainMenuUI>();
      AudioManager.Instance.StopSound(MusicTitle);
      break;
    case eGameState.Game:
      //HideUI<CountdownTimerUI>();
      AudioManager.Instance.StopSound(MusicGame);
      break;
    case eGameState.PostGame:
      ClearLevel();
      HideUI<PostGameUI>();
      AudioManager.Instance.StopSound(MusicEnd);
      break;
    }
  }

  private void OnDestroy()
  {
    GameController.Instance = null;
  }

  public void ShowUI<T>() where T : UIPageBase
  {
    PlayerUI playerUI = PlayerUI.Instance;
    if (playerUI != null)
    {
      var uiPage = playerUI.GetPage<T>();
      if (uiPage != null)
      {
        uiPage.Show();
      }
    }
  }

  public void HideUI<T>() where T : UIPageBase
  {
    PlayerUI playerUI = PlayerUI.Instance;
    if (playerUI != null)
    {
      var uiPage = playerUI.GetPage<T>();
      if (uiPage != null)
      {
        uiPage.Hide();
      }
    }
  }

  void SpawnLevel()
  {
    _isMatchStarted = false;
    _isSpawningAllowed = true;

    // Use the rising game camera
    if (MainCamera.Instance != null)
    {
      MainCamera.Instance.CameraStack.PushController(_cameraController);
    }

    // Spawn the level sections
    _levelManager.GenerateLevel(false);
  }

  void DespawnPlayers()
  {
    foreach (PlayerActorController player in _spawnedPlayers)
    {
      Destroy(player.gameObject);
    }

    _spawnedPlayers.Clear();
  }

  void SpawnPlayerAtSpawnPoint(int playerIndex)
  {
    if (_playerPrefab != null)
    {
      var playerSpawnPoint = _levelManager.PickPlayerSpawnPoint();

      if (playerSpawnPoint != null)
      {
        var spawnTransform= playerSpawnPoint.transform;

        SpawnPlayerAtLocation(playerIndex, spawnTransform.position, spawnTransform.rotation);
      }
    }
  }

  void SpawnPlayerAtLocation(int playerIndex, Vector3 position, Quaternion rotation)
  {
    var playerGO = Instantiate(_playerPrefab.gameObject, position, rotation);
    var playerController = playerGO.GetComponent<PlayerActorController>();
    playerController.SetPlayerIndex(playerIndex);
    playerController.SetPlayerColor(_playerColors[playerIndex].ColorName);

    _spawnedPlayers.Add(playerController);
  }

  void DespawnPlayer(PlayerActorController playerController)
  {
    _spawnedPlayers.Remove(playerController);

    Destroy(playerController.gameObject);
  }

  private void TriggerCountDown()
  {
    _isInCountdown = true;
    WinningPlayerCountdownTimer = WinCountDownTime;

    //ShowUI<CountdownTimerUI>();
  }

  private void StartMatch()
  {
    _isMatchStarted = true;
    _isSpawningAllowed = false;
    MatchStarted?.Invoke();
  }

  private void TriggerPostGame()
  {
    SetGameState(eGameState.PostGame);
  }

  void ClearLevel()
  {
    DespawnPlayers();
    _cameraController.Reset();
    _levelManager.DestroyLevel(false);
    _isInCountdown = false;
    MainCamera.Instance.CameraStack.PopController(_cameraController);
  }

  private void OnDrawGizmos()
  {
    Gizmos.color = Color.white;
    Gizmos.DrawLine(new Vector3(-100, StartMatchHeight, 0), new Vector3(100, StartMatchHeight, 0));
  }
}