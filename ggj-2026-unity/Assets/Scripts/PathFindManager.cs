using UnityEngine;
using System.Collections.Generic;

public class PathFindManager : Singleton<PathFindManager>
{
  public bool TryGetTraversablePoint(Vector3 worldPoint, out Vector3 pointOnNavMesh, float maxDistance = 1.0f)
  {
    pointOnNavMesh = worldPoint;
    return true;
  }

  public bool IsPointTraversable(Vector3 worldPoint)
  {
    return true;
  }

  public bool CalculatePathToPoint(Vector3 fromPoint, Vector3 toPoint, List<Vector3> outPath)
  {
    outPath.Clear();
    outPath.Add(fromPoint);
    outPath.Add(toPoint);

    return true;
  }

  private void Awake()
  {
    Instance = this;
  }

  private void Start()
  {
  }
}