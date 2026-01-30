using UnityEngine;
using System.Collections.Generic;

public class SpawnManager : Singleton<SpawnManager>
{
  private List<SpawnObject> _spawningObjects = new();

  private struct SpawnObject
  {
    public GameObject GameObject;
    public float FadeInDuration;
    public float FadeInTimer;
    public Vector3 StartScale;
  }

  public void AddObject(GameObject obj, float fadeInTime)
  {
    SpawnObject spawnObject = new();
    spawnObject.GameObject = obj;
    spawnObject.FadeInDuration = fadeInTime;
    spawnObject.StartScale = obj.transform.localScale;
    obj.transform.localScale = Vector3.one * 0.01f;
    _spawningObjects.Add(spawnObject);
  }

  private void Awake()
  {
    Instance = this;
  }

  private void Update()
  {
    float dt = Time.deltaTime;

    // Hydrate objects smoothly
    for (int i = 0; i < _spawningObjects.Count; ++i)
    {
      var obj = _spawningObjects[i];
      obj.FadeInTimer += dt;

      float fadeT = Mathf.Clamp01(obj.FadeInTimer / obj.FadeInDuration);
      fadeT = Mathf.SmoothStep(0.01f, 1, fadeT);

      if (obj.GameObject != null)
        obj.GameObject.transform.localScale = obj.StartScale * fadeT;

      _spawningObjects[i] = obj;

      if (fadeT >= 1)
      {
        _spawningObjects.RemoveAt(i);
        --i;
      }
    }
  }
}