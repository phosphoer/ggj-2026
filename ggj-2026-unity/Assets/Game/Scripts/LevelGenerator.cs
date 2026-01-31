using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
  [SerializeField] private PlayerSpawnPoint[] _playerSpawns;
  [SerializeField] private FarmerSpawnPoint[] _farmerSpawns;
  [SerializeField] private PossessableSpawnPoint[] _tinyPossessableSpawns;
  [SerializeField] private int _desiredTinyCount = 1;
  [SerializeField] private PossessableSpawnPoint[] _smallPossessableSpawns;
  [SerializeField] private int _desiredSmallCount = 1;
  [SerializeField] private PossessableSpawnPoint[] _mediumPossessableSpawns;
  [SerializeField] private int _desiredMediumCount = 1;
  [SerializeField] private PossessableSpawnPoint[] _largePossessableSpawns;
  [SerializeField] private int _desiredLargeCount= 1;

  private class ShuffledValues
  {
    private int[] _shuffledValues;
    private int _nextReadIndex;

    public ShuffledValues(int size)
    {
      _shuffledValues = new int[size];
      for (int i = 0; i < _shuffledValues.Length; i++)
      {
        _shuffledValues[i] = i;
      }

      if (size > 1)
      {
        // Fisher-Yates shuffle
        for (var i = 0; i < size - 1; ++i)
        {
          var r = UnityEngine.Random.Range(i, size);
          var tmp = _shuffledValues[i];
          _shuffledValues[i] = _shuffledValues[r];
          _shuffledValues[r] = tmp;
        }
      }

      _nextReadIndex = 0;
    }

    public int GetNextValue()
    {
      int nextIndex = -1;

      if (_shuffledValues.Length > 0)
      {
        nextIndex = _shuffledValues[_nextReadIndex];

        _nextReadIndex = (_nextReadIndex + 1) % _shuffledValues.Length;
      }

      return nextIndex;
    }
  }

  private List<PlayerSpawnPoint> _unusedPlayerSpawnPoints;
  private List<FarmerSpawnPoint> _unusedFarmerSpawnPoints;
  private ShuffledValues _shuffledTinyPossessableIndices;
  private ShuffledValues _shuffledSmallPossessableIndices;
  private ShuffledValues _shuffledMediumPossessableIndices;
  private ShuffledValues _shuffledLargePossessableIndices;
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

    // Generate shuffled lists if we haven't already
    if (_shuffledTinyPossessableIndices == null)
    {
      _shuffledTinyPossessableIndices = new ShuffledValues(_tinyPossessableSpawns.Length);
      _shuffledSmallPossessableIndices = new ShuffledValues(_smallPossessableSpawns.Length);
      _shuffledMediumPossessableIndices = new ShuffledValues(_mediumPossessableSpawns.Length);
      _shuffledLargePossessableIndices = new ShuffledValues(_largePossessableSpawns.Length);
    }

    SpawnPossessableObjects(_shuffledTinyPossessableIndices, _tinyPossessableSpawns, _desiredTinyCount);
    SpawnPossessableObjects(_shuffledSmallPossessableIndices, _smallPossessableSpawns, _desiredSmallCount);
    SpawnPossessableObjects(_shuffledMediumPossessableIndices, _mediumPossessableSpawns, _desiredMediumCount);
    SpawnPossessableObjects(_shuffledLargePossessableIndices, _largePossessableSpawns, _desiredLargeCount);
  }

  private void SpawnPossessableObjects(ShuffledValues shuffledValues, PossessableSpawnPoint[] possessableSpawns, int desiredCount)
  {
    int spawnRemaining = Math.Min(desiredCount, possessableSpawns.Length);
    while (spawnRemaining > 0)
    {
      int nextSpawnIndex= shuffledValues.GetNextValue();
      var spawner = possessableSpawns[nextSpawnIndex];
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

  [ContextMenu("Clear Possessables")]
  private void EditorClearPossessables()
  {
    DestroyLevel(true);
  }
}
