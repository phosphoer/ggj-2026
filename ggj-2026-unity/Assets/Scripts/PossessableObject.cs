using UnityEngine;

public class PossessableObject : MonoBehaviour
{
  public Transform[] LegSockets => _legSockets;
  public int FootStepCount = 2;
  public float FootStepDuration = 0.5f;
  public float FootStepThreshold = 0.1f;

  [SerializeField] private Transform[] _legSockets = null;
}