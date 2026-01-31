using UnityEngine;

public class LevelCameraController : CameraControllerDynamic
{
  private Vector3 _initialPosition = Vector3.zero;

  public void Awake()
  {
    _initialPosition = MountPoint.position;
  }

  void Update()
  {
  }

  public void Reset()
  {
    MountPoint.position = _initialPosition;
  }
}