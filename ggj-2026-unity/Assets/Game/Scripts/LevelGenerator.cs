using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
  [SerializeField] private PlayerSpawnPoint[] _playerSpawns;
  [SerializeField] private FarmerSpawnPoint[] _farmerSpawns;
  [SerializeField] private PossessableSpawnPoint[] _possessableSpawns;
  [SerializeField] private int _desiredPossessableCount= 30;

  private List<PlayerSpawnPoint> _unusedPlayerSpawnPoints;
  private List<FarmerSpawnPoint> _unusedFarmerSpawnPoints;
  private List<PossessableSpawnPoint> _unusedPossessableSpawnPoints;
  private List<GameObject> _spawnedGameObjects;

  public void DestroyLevel(bool inEditor)
  {
    if (_spawnedGameObjects != null)
    {
      foreach (var go in _spawnedGameObjects)
      {
        if (go != null)
        {
          if (inEditor)
          {
            DestroyImmediate(go);
          }
          else
          {
            Destroy(go);
          }
        }
      }

      _spawnedGameObjects= null;
    }
  }

  public void GenerateLevel(bool inEditor)
  {
    DestroyLevel(inEditor);

    _spawnedGameObjects = new List<GameObject>();
    _unusedPlayerSpawnPoints = _playerSpawns.ToList();
    _unusedFarmerSpawnPoints = _farmerSpawns.ToList();
    _unusedPossessableSpawnPoints = _possessableSpawns.ToList();

    int spawnRemaining= _desiredPossessableCount;
    while (spawnRemaining > 0 && _unusedPossessableSpawnPoints.Count > 0)
    {
      var spawner= PickPossessableSpawnPoint();
      var possessableGO = 
        GameObject.Instantiate(
          spawner.PossessableTemplate,
          spawner.transform.position,
          spawner.transform.rotation);

      _spawnedGameObjects.Add(possessableGO);
      spawnRemaining--;
    }
  }

  public PlayerSpawnPoint PickPlayerSpawnPoint()
  {
    return PickSpawnPoint<PlayerSpawnPoint>(_unusedPlayerSpawnPoints);
  }

  public FarmerSpawnPoint PickFarmerSpawnPoint()
  {
    return PickSpawnPoint<FarmerSpawnPoint>(_unusedFarmerSpawnPoints);
  }

  public PossessableSpawnPoint PickPossessableSpawnPoint()
  {
    return PickSpawnPoint<PossessableSpawnPoint>(_unusedPossessableSpawnPoints);
  }

  public T PickSpawnPoint<T>(List<T> unusedSpawnPoints) where T : MonoBehaviour
  {
    if (unusedSpawnPoints != null && unusedSpawnPoints.Count > 0)
    {
      int randIndex = UnityEngine.Random.Range(0, unusedSpawnPoints.Count);
      T result = unusedSpawnPoints[randIndex];

      unusedSpawnPoints.RemoveAt(randIndex);
      return result;
    }

    return null;
  }

  [ContextMenu("Regenerate Possessables")]
  private void EditorGeneratePossessables()
  {
    GenerateLevel(true);
  }
}
