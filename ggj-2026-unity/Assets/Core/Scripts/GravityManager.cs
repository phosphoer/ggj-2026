using UnityEngine;
using System.Collections.Generic;

public class GravityManager : Singleton<GravityManager>
{
  private struct GravityItem
  {
    public Rigidbody Rigidbody;
    public Vector3 Gravity;
  }

  private List<GravityItem> _gravityItems = new();

  public void AddGravityItem(Rigidbody rigidbody, Vector3 gravityForce)
  {
    _gravityItems.Add(new GravityItem()
    {
      Rigidbody = rigidbody,
      Gravity = gravityForce,
    });
  }

  public bool RemoveGravityItem(Rigidbody rigidbody)
  {
    for (int i = 0; i < _gravityItems.Count; ++i)
    {
      if (ReferenceEquals(_gravityItems[i].Rigidbody, rigidbody))
      {
        _gravityItems.RemoveAt(i);
        return true;
      }
    }

    return false;
  }

  private void Awake()
  {
    Instance = this;
  }

  private void FixedUpdate()
  {
    float dt = Time.fixedDeltaTime;
    for (int i = 0; i < _gravityItems.Count; ++i)
    {
      var gravityItem = _gravityItems[i];
      if (!gravityItem.Rigidbody)
        _gravityItems.RemoveAt(i);
      else
        gravityItem.Rigidbody.AddForce(gravityItem.Gravity * dt, ForceMode.Acceleration);
    }
  }
}