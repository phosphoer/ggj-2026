using UnityEngine;
using System.Collections.Generic;

public class PlayerManager : Singleton<PlayerManager>
{
  public static event System.Action<PlayerCharacterController> PlayerJoined;
  public static event System.Action<PlayerCharacterController> PlayerWon;

  public IReadOnlyList<PlayerCharacterController> Players => _players;

  [SerializeField]
  private PlayerCharacterController[] _playerPrefabs = null;

  [SerializeField]
  private Transform[] _spawnPoints = null;

  private bool _canSpawnPlayers = false;
  private List<PlayerCharacterController> _players = new List<PlayerCharacterController>();
  private List<bool> _playerJoinedStates = new List<bool>();
  private int _nextSpawnIndex = 0;
  private int _nextPlayerPrefabIndex = 0;

  public void SetCanSpawnPlayers(bool newCanSpawnPlayers)
  {
    // Despawn any existing players if we are going back to disallowing players to exist
    if (_canSpawnPlayers && !newCanSpawnPlayers)
    {
      DespawnPlayers();
    }

    _canSpawnPlayers = newCanSpawnPlayers;
  }

  public bool IsPlayerJoined(int playerId)
  {
    return _playerJoinedStates.Count > playerId && _playerJoinedStates[playerId];
  }

  public void DespawnPlayers()
  {
    for (int i = 0; i < _players.Count; ++i)
    {
      PlayerCharacterController player = _players[i];

      Destroy(player.gameObject);
    }

    _players.Clear();
    _playerJoinedStates.Clear();
    _nextSpawnIndex = 0;
  }

  public void LockAllPlayers()
  {
    foreach (PlayerCharacterController player in _players)
    {
      player.SetIsAllowedToMove(false);
    }
  }

  private void Awake()
  {
    Instance = this;
  }

  private void Update()
  {
    if (!Rewired.ReInput.isReady || !_canSpawnPlayers)
    {
      return;
    }

    // Iterate over existing rewired players and spawn their character if they press a button
    for (int i = 0; i < Rewired.ReInput.players.playerCount; ++i)
    {
      Rewired.Player player = Rewired.ReInput.players.GetPlayer(i);
      if (!IsPlayerJoined(i) && player.GetAnyButton())
      {
        AddPlayer(player);
      }
    }
  }

  private PlayerCharacterController AddPlayer(Rewired.Player rewiredPlayer)
  {
    int playerId = _nextSpawnIndex;
    Transform spawnPoint = _spawnPoints[_nextSpawnIndex];
    PlayerCharacterController playerPrefab = _playerPrefabs[_nextPlayerPrefabIndex];
    PlayerCharacterController player = Instantiate(playerPrefab, transform);
    player.RewiredPlayerId = Rewired.ReInput.players.GetPlayers().IndexOf(rewiredPlayer);
    player.transform.position = spawnPoint.transform.position;
    player.transform.rotation = Quaternion.Euler(0, Random.value * 360, 0);
    player.AssignPlayerId(playerId);
    _players.Add(player);

    _nextSpawnIndex = (_nextSpawnIndex + 1) % _spawnPoints.Length;
    _nextPlayerPrefabIndex = (_nextPlayerPrefabIndex + 1) % _playerPrefabs.Length;

    // Set joined state
    if (rewiredPlayer != null)
    {
      while (_playerJoinedStates.Count <= rewiredPlayer.id)
        _playerJoinedStates.Add(false);
      _playerJoinedStates[rewiredPlayer.id] = true;
    }

    PlayerJoined?.Invoke(player);

    return player;
  }

#if UNITY_EDITOR
  [UnityEditor.MenuItem("Game/Add Debug Player")]
  private static void DebugAddPlayer()
  {
    var player = Instance.AddPlayer(null);
    player.RewiredPlayerId = Instance._players.Count - 1;
  }

  [UnityEditor.MenuItem("Game/Debug Mark All Ready")]
  private static void DebugMarkAllReady()
  {
    foreach (PlayerCharacterController player in Instance._players)
    {
      player.SetReadyFlag();
    }
  }
#endif
}