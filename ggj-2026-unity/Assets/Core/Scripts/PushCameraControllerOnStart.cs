using UnityEngine;

public class PushCameraControllerOnStart : MonoBehaviour
{
  public CameraControllerBase CameraController;

  private void Start()
  {
    MainCamera.Instance.CameraStack.PushController(CameraController);
  }
}