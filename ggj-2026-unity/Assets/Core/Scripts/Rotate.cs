using UnityEngine;

public class Rotate : MonoBehaviour
{
  public Transform Target;
  public Vector3 RotateAxis = Vector3.up;
  public Space Space;
  public float RotateSpeed = 1.0f;

  private void Awake()
  {
    if (Target == null)
      Target = transform;
  }

  private void Update()
  {
    Target.Rotate(RotateAxis * (RotateSpeed * Time.deltaTime), Space);
  }
}