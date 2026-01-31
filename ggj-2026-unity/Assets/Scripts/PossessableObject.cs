using UnityEngine;

public class PossessableObject : MonoBehaviour
{
  public Transform[] LegSockets => _legSockets;
  public int FootStepCount = 2;

  [SerializeField] private Transform[] _legSockets = null;
}