using UnityEngine;

public enum SpookAttackType
{
  Shoot,
  Charge,
  AOE,
}

[System.Serializable]
public class SpookAttackParams
{
  public SpookAttackType Type;
  public ParticleSystem SpookAttackFX;
  public Transform SpookFXRoot;
  public Transform SpookAttackRoot;

  [Header("Shoot")]
  public float ShootAttackWidth = 1;
  public float ShootAttackRange = 3;
  public float ShootRecoil = 5;

  [Header("Charge")]
  public float ChargeSpeed = 5;
  public float ChargeDuration = 2;
  public float ChargeAttackRadius = 1;

  [Header("AOE")]
  public float AOERadius = 3;
}

public class PossessableObject : MonoBehaviour
{
  public Transform[] LegSockets => _legSockets;

  [Header("Spook Attack")]
  public SpookAttackParams AttackParams = null;

  [Header("Idle Animation")]
  public float StandHeightOffset = 0;
  public float AnimIdleBobScale = 0.05f;
  public float AnimIdleBobSpeed = 3f;
  public float AnimIdleWiggleScale = 5;
  public float AnimIdleWiggleSpeed = 1;
  public float AnimWalkLeanScale = 20;

  [Header("Movement")]
  public float MoveSpeed = 2;
  public float RotateSpeed = 3;
  public bool PostPossessKinematicState = false;

  [Header("Feet/Leg Config")]
  public float LegThickness = 0.1f;
  public float FootSize = 0.1f;
  public int FootStepCount = 2;
  public float FootStepDuration = 0.5f;
  public float FootStepThreshold = 0.1f;
  public float FootStepHeight = 0.25f;
  [SerializeField] private Transform[] _legSockets = null;

  [Header("SFX")]
  public SoundBank SFXPossess;
  public SoundBank SFXDepossess;


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

    if (AttackParams != null && AttackParams.Type == SpookAttackType.Charge && AttackParams.SpookAttackRoot)
    {
      Gizmos.color = Color.red;
      Gizmos.DrawWireSphere(AttackParams.SpookAttackRoot.position, AttackParams.ChargeAttackRadius);
    }

    if (AttackParams != null && AttackParams.Type == SpookAttackType.Shoot && AttackParams.SpookAttackRoot)
    {
      Gizmos.color = Color.red;
      Gizmos.matrix = AttackParams.SpookAttackRoot.localToWorldMatrix;
      Gizmos.DrawWireCube(Vector3.forward * AttackParams.ShootAttackRange * 0.5f, new Vector3(AttackParams.ShootAttackWidth, AttackParams.ShootAttackWidth, AttackParams.ShootAttackRange));
      Gizmos.matrix = Matrix4x4.identity;
    }

    if (AttackParams != null && AttackParams.Type == SpookAttackType.AOE && AttackParams.SpookAttackRoot)
    {
      Gizmos.color = Color.red;
      Gizmos.matrix = AttackParams.SpookAttackRoot.localToWorldMatrix;
      GizmosEx.DrawCircle(Vector3.zero, Vector3.up, AttackParams.AOERadius);
      Gizmos.matrix = Matrix4x4.identity;
    }
  }
}