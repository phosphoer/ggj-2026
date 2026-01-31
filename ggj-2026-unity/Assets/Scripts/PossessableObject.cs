using UnityEngine;

public class PossessableObject : MonoBehaviour
{
  public Transform[] LegSockets => _legSockets;

  [SerializeField] private Transform[] _legSockets = null;
}