using UnityEngine;

public class FaceCamera : MonoBehaviour
{
  public bool FlipZ;

  private void LateUpdate()
  {
    Vector3 pos = Camera.main.transform.position;
    Vector3 toCamera = pos - transform.position;

    transform.rotation = Quaternion.LookRotation(toCamera.normalized * (FlipZ ? 1.0f : -1.0f), Vector3.up);
  }
}