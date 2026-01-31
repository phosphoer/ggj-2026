using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

public class PlayerUI : UIPageGroup
{
  public static PlayerUI Instance { get; private set; }

  public Camera Camera => _uiCamera;

  [SerializeField]
  private Camera _uiCamera = null;

  [SerializeField]
  private bool _addUICameraAsOverlay = true;

  protected virtual void Awake()
  {
    Instance = this;
  }

  private void Start()
  {
    if (MainCamera.Instance)
    {
      var cameraData = MainCamera.Instance.Camera.GetUniversalAdditionalCameraData();
      cameraData.cameraStack.Add(_uiCamera);
    }
  }
}