using UnityEngine;
using System.Collections.Generic;

public static class GameObjectExtensions
{
  private static List<MeshRenderer> _cachedRenderers = new List<MeshRenderer>();

  public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
  {
    T component;
    if (!gameObject.TryGetComponent<T>(out component))
      component = gameObject.AddComponent<T>();

    return component;
  }

  public static void SetLayerRecursively(this GameObject gameObject, int layer)
  {
    gameObject.layer = layer;
    foreach (Transform t in gameObject.transform)
    {
      SetLayerRecursively(t.gameObject, layer);
    }
  }


  public static Bounds GetHierarchyBounds(this GameObject gameObject)
  {
    return gameObject.GetHierarchyBounds(Physics.AllLayers);
  }

  public static Bounds GetHierarchyBounds(this GameObject gameObject, LayerMask layerMask = default)
  {
    _cachedRenderers.Clear();
    gameObject.GetComponentsInChildren<MeshRenderer>(_cachedRenderers);

    Bounds bounds = _cachedRenderers.Count > 0 ? _cachedRenderers[0].bounds : new Bounds(gameObject.transform.position, Vector3.zero);
    foreach (Renderer r in _cachedRenderers)
    {
      if (layerMask.ContainsLayer(r.gameObject.layer))
        bounds.Encapsulate(r.bounds);
    }

    return bounds;
  }

  public static void SetIdentityTransformLocal(this Transform transform)
  {
    transform.localScale = Vector3.one;
    transform.localPosition = Vector3.zero;
    transform.localRotation = Quaternion.identity;
  }

  public static Vector3 GetLocalAxis(this Transform transform, Mathfx.Axis axis)
  {
    return axis switch
    {
      Mathfx.Axis.X => transform.right,
      Mathfx.Axis.Y => transform.up,
      Mathfx.Axis.Z => transform.forward,
      _ => Vector3.zero,
    };
  }

  public static void SetLocalAxis(this Transform transform, Mathfx.Axis axis, Vector3 axisValue)
  {
    switch (axis)
    {
      case Mathfx.Axis.X:
        transform.right = axisValue;
        break;
      case Mathfx.Axis.Y:
        transform.up = axisValue;
        break;
      case Mathfx.Axis.Z:
        transform.forward = axisValue;
        break;
    }
  }

  public static Transform GetRootParent(this Transform transform)
  {
    if (ReferenceEquals(transform.parent, null))
      return transform;

    Transform node = transform.parent;
    while (!ReferenceEquals(node.parent, null))
      node = node.parent;

    return node;
  }

  public static void DestroyAllChildren(this Transform transform)
  {
    for (int i = transform.childCount - 1; i >= 0; --i)
    {
      GameObject.Destroy(transform.GetChild(i).gameObject);
    }
  }

  public static void DestroyAllChildrenImmediate(this Transform transform)
  {
    while (transform.childCount > 0)
    {
      GameObject.DestroyImmediate(transform.GetChild(0).gameObject);
    }
  }

  public static void UnparentAndDestroyOnStop(this ParticleSystem ps)
  {
    ps.transform.parent = null;
    var settings = ps.main;
    settings.stopAction = ParticleSystemStopAction.Destroy;
  }

  public static void DestroyOnStop(this ParticleSystem ps)
  {
    var settings = ps.main;
    settings.stopAction = ParticleSystemStopAction.Destroy;
  }

#if UNITY_EDITOR
  [UnityEditor.MenuItem("GameObject/Mirror Global X")]
  public static void MirrorSelectedGlobalX()
  {
    foreach (var obj in UnityEditor.Selection.gameObjects)
    {
      obj.transform.localPosition = obj.transform.localPosition.WithX(-obj.transform.localPosition.x);
      obj.transform.rotation = Quaternion.LookRotation(Vector3.Reflect(obj.transform.forward, Vector3.left), obj.transform.up);
    }
  }

  [UnityEditor.MenuItem("GameObject/Randomize Rotation")]
  public static void RandomizeRotation()
  {
    foreach (var obj in UnityEditor.Selection.gameObjects)
    {
      obj.transform.rotation = Random.rotationUniform;
    }
  }

  [UnityEditor.MenuItem("GameObject/Randomize Child Order")]
  public static void RandomizeChildOrder()
  {
    foreach (var obj in UnityEditor.Selection.gameObjects)
    {
      obj.transform.SetSiblingIndex(Random.Range(0, obj.transform.parent.childCount));
    }
  }
#endif
}