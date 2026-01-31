using UnityEngine;

public enum SpookAttackType
{
  Shoot,
  Charge,
}

[System.Serializable]
public class SpookAttackParams
{
  public SpookAttackType Type;

  [Header("Shoot")]
  public float ShootAttackWidth = 1;
  public float ShootAttackRange = 3;

  [Header("Charge")]
  public float ChargeSpeed = 5;
  public float ChargeDuration = 2;
}

public class PossessableObject : MonoBehaviour
{
  public Transform[] LegSockets => _legSockets;

  [Header("Spook Attack")]
  public SpookAttackParams AttackParams = null;
  public ParticleSystem SpookAttackFX = null;
  public Transform SpookAttackRoot = null;

  [Header("Idle Animation")]
  public float StandHeightOffset = 0;
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