using UnityEngine;

public class PossessableObject : MonoBehaviour
{
  public Transform[] LegSockets => _legSockets;

  [Header("Idle Animation")]
  public float AnimIdleBobScale = 0.05f;
  public float AnimIdleBobSpeed = 3f;
  public float AnimIdleWiggleScale = 5;
  public float AnimIdleWiggleSpeed = 1;

  [Header("Movement")]
  public float MoveSpeed = 2;
  public float RotateSpeed = 3;

  [Header("Feet/Leg Config")]
  public float LegThickness = 0.1f;
  public float FootSize = 0.1f;
  public int FootStepCount = 2;
  public float FootStepDuration = 0.5f;
  public float FootStepThreshold = 0.1f;
  public float FootStepHeight = 0.25f;

  [SerializeField] private Transform[] _legSockets = null;

  private void OnDrawGizmos()
  {
    Gizmos.color = Color.white;
    if (_legSockets != null)
    {
      foreach (var socket in _legSockets)
      {
        if (socket)
        {
          Gizmos.DrawWireSphere(socket.position, 0.05f);
        }
      }
    }
  }
}