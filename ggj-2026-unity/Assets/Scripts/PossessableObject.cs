using UnityEngine;

public class PossessableObject : MonoBehaviour
{
  public Transform[] LegSockets => _legSockets;
  public int FootStepCount = 2;
  public float FootStepDuration = 0.5f;
  public float FootStepThreshold = 0.1f;

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