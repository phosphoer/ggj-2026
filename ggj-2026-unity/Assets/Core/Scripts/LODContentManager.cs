using UnityEngine;
using System.Collections.Generic;

public class LODContentManager : Singleton<LODContentManager>
{
  private static List<ILODContent> _lodItems = new();

  private int _updateIndex = 0;

  // Reset static state for editor without domain reload
#if UNITY_EDITOR
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
  private static void EditorInit()
  {
    _lodItems.Clear();
  }
#endif

  public static void AddLODContent(ILODContent lodContent)
  {
    _lodItems.Add(lodContent);
  }

  public static void RemoveLODContent(ILODContent lodContent)
  {
    _lodItems.Remove(lodContent);
  }

  public static void UpdateLODContent(ILODContent lodContent)
  {
    if (MainCamera.Instance == null)
      return;

    Vector3 cameraPos = MainCamera.Instance.CachedTransform.position;
    float distToCamera = Mathf.Max(0, lodContent.GetLODDistance(cameraPos));

    int lodLevel = lodContent.LODRanges.Length - 1;
    for (int i = 0; i < lodContent.LODRanges.Length; ++i)
    {
      LODRange lodRange = lodContent.LODRanges[i];
      if (distToCamera < lodRange.Max && distToCamera >= lodRange.Min)
      {
        lodLevel = i;
        break;
      }
    }

    if (lodContent.CurrentLODLevel != lodLevel)
      lodContent.SetLOD(lodLevel);
  }

  private void Awake()
  {
    Instance = this;
  }

  private void Update()
  {
    if (_updateIndex < _lodItems.Count)
    {
      ILODContent lodContent = _lodItems[_updateIndex];
      UpdateLODContent(lodContent);
    }

    _updateIndex += 1;
    if (_updateIndex >= _lodItems.Count)
      _updateIndex = 0;
  }
}