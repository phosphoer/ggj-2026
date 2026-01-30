using UnityEngine;
using System.Collections.Generic;

public class DespawnManager : Singleton<DespawnManager>
{
  private List<DespawnObject> _pendingObjects = new();
  private List<DespawnObject> _despawningObjects = new();

  private struct DespawnObject
  {
    public GameObject GameObject;
    public float WaitDuration;
    public float WaitTimer;
    public float FadeOutDuration;
    public float FadeOutTimer;
    public Vector3 StartScale;
  }

  public void AddObject(GameObject obj, float waitTime, float fadeOutTime)
  {
    DespawnObject despawnObject = new();
    despawnObject.GameObject = obj;
    despawnObject.WaitDuration = waitTime;
    despawnObject.FadeOutDuration = fadeOutTime;
    despawnObject.StartScale = obj.transform.localScale;
    _pendingObjects.Add(despawnObject);
  }

  private void Awake()
  {
    Instance = this;
  }

  private void Update()
  {
    float dt = Time.deltaTime;

    // Count down timers for objects pending despawn
    for (int i = 0; i < _pendingObjects.Count; ++i)
    {
      var obj = _pendingObjects[i];
      obj.WaitTimer += dt;
      _pendingObjects[i] = obj;
      if (obj.WaitTimer > obj.WaitDuration)
      {
        _pendingObjects.RemoveAt(i);
        _despawningObjects.Add(obj);
        --i;
      }
    }

    // Dehydrate objects smoothly and then destroy them
    for (int i = 0; i < _despawningObjects.Count; ++i)
    {
      var obj = _despawningObjects[i];
      obj.FadeOutTimer += dt;
      _despawningObjects[i] = obj;

      float fadeT = 1 - Mathf.Clamp01(obj.FadeOutTimer / obj.FadeOutDuration);

      if (obj.GameObject != null)
        obj.GameObject.transform.localScale = obj.StartScale * fadeT;

      if (fadeT <= 0)
      {
        if (obj.GameObject != null)
          Destroy(obj.GameObject);
        _despawningObjects.RemoveAt(i);
        --i;
      }
    }
  }
}