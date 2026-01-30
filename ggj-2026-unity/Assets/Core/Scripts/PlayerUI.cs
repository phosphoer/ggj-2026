using UnityEngine;
using System.Collections.Generic;

public class PlayerUI : UIPageGroup
{
  public static PlayerUI Instance { get; private set; }

  public Camera Camera => _uiCamera;

  [SerializeField]
  private Camera _uiCamera = null;

  protected virtual void Awake()
  {
    Instance = this;
  }
}