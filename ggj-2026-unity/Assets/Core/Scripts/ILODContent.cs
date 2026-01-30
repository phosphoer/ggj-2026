using UnityEngine;

public struct LODRange
{
  public float Min;
  public float Max;
}

public interface ILODContent
{
  LODRange[] LODRanges { get; }
  public int CurrentLODLevel { get; }

  float GetLODDistance(Vector3 fromPoint);
  void SetLOD(int lodLevel);
}