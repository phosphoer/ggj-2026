using UnityEngine;
using System.Collections.Generic;

public class PoissonDisk
{
  public float MinimumDistance = 1;
  public int GridSize = 10;

  private List<Vector2> _points = new();
  private float _cellSize;
  private List<int> _grid = new();
  private System.Random _rand;

#if UNITY_EDITOR
  [UnityEditor.MenuItem("Sub Game/Test Poisson Disk")]
  public static void DebugTest()
  {
    PoissonDisk disk = new(1, 10);
    for (int i = 0; i < 30; ++i)
    {
      Vector2 p = disk.AddPoint();
      Debug.DrawRay(p.OnXZPlane(), Vector3.up, Color.white, 10);
    }
  }
#endif

  public PoissonDisk(float minDist, int gridSize, int seed = -1)
  {
    GridSize = gridSize;
    MinimumDistance = minDist;
    _cellSize = minDist / Mathf.Sqrt(2);
    _rand = new System.Random(seed >= 0 ? seed : Random.Range(0, 100000));

    // for (int i = 0; i < gridSize * gridSize; ++i)
    // _grid.Add(-1);
  }

  public void Clear()
  {
    _points.Clear();
    for (int i = 0; i < _grid.Count; ++i)
      _grid[i] = -1;
  }

  public Vector2 AddPoint()
  {
    if (_points.Count == 0)
    {
      Vector2 p = _rand.NextPointInsideCircle() * MinimumDistance;
      AddPointToGrid(p);
      return p;
    }

    for (int i = 0; i < Mathf.Max(10, _points.Count); ++i)
    {
      Vector2 startPoint = _points[_rand.NextIntRanged(0, _points.Count)];

      float radius = _rand.NextFloatRanged(MinimumDistance, MinimumDistance * 2);
      float angle = _rand.NextFloatRanged(0, Mathf.PI * 2);
      Vector2 point = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius) + startPoint;
      if (IsPointValid(point))
      {
        AddPointToGrid(point);
        return point;
      }
    }

    return Vector2.zero;
  }

  private bool IsPointValid(Vector2 point)
  {
    (int gridX, int gridY) = PointToGrid(point);
    if (gridX < 0 || gridY < 0 || gridX >= GridSize || gridY >= GridSize)
      return false;

    for (int x = gridX - 2; x < gridX + 2; ++x)
    {
      for (int y = gridY - 2; y < gridY + 2; ++y)
      {
        int pointIndex = GetGridIndex(x, y);
        if (pointIndex >= 0)
        {
          Vector2 neighborPoint = _points[pointIndex];
          if (Vector2.Distance(neighborPoint, point) < MinimumDistance)
            return false;
        }
      }
    }

    return true;
  }

  private (int, int) PointToGrid(Vector2 point)
  {
    int gridX = Mathf.FloorToInt(point.x / _cellSize) + GridSize / 2;
    int gridY = Mathf.FloorToInt(point.y / _cellSize) + GridSize / 2;
    return (gridX, gridY);
  }

  private int GetGridIndex(int x, int y)
  {
    if (x < 0 || y < 0)
      return -1;

    if (x >= GridSize || y >= GridSize)
      return -1;

    int gridIndex = x + y * GridSize;
    if (!_grid.IsIndexValid(gridIndex))
      return -1;

    return _grid[gridIndex];
  }

  private void AddPointToGrid(Vector2 point)
  {
    _points.Add(point);

    (int gridX, int gridY) = PointToGrid(point);
    int pointIndex = _points.Count - 1;
    int gridIndex = gridX + gridY * GridSize;
    for (int i = _grid.Count; i <= gridIndex; ++i)
      _grid.Add(-1);

    _grid[gridX + gridY * GridSize] = pointIndex;
  }
}