using UnityEngine;
using System.Collections.Generic;

public class LegNoodleController : MonoBehaviour
{
  public Transform FootTarget
  {
    get => _footTarget;
    set => _footTarget = value;
  }

  [SerializeField] private Transform _footTarget = null;
  [SerializeField] private Transform[] _bones = null;
  [SerializeField] private Mathfx.Axis _forwardAxis = Mathfx.Axis.Z;
  [SerializeField] private LineRenderer _lineRenderer = null;

  private List<Vector3> _bonePositions = new();
  private List<float> _boneLengths = new();

  public void InitializeLeg()
  {
    Vector3 boneStartPos = transform.position;
    Vector3 boneEndPos = _footTarget.position;
    for (int i = 0; i < _bones.Length; ++i)
    {
      float armT = i / (_bones.Length - 1f);
      Vector3 bonePos = Vector3.Lerp(boneStartPos, boneEndPos, armT);
      _bonePositions.Add(transform.InverseTransformPoint(bonePos));
      _bones[i].parent = transform;
    }

    for (int i = 0; i < _bones.Length - 1; ++i)
    {
      Vector3 boneAPos = _bones[i].position;
      Vector3 boneBPos = _bones[i + 1].position;
      float boneLength = (boneBPos - boneAPos).magnitude;
      _boneLengths.Add(boneLength);
    }

    _lineRenderer.positionCount = _bones.Length + 1;
  }

  private void Start()
  {
    if (_footTarget)
      InitializeLeg();
  }

  private void LateUpdate()
  {
    float dt = Time.deltaTime;

    if (!_footTarget)
      return;

    Vector3 shoulderPos = transform.position;
    for (int i = 1; i < _bonePositions.Count; ++i)
    {
      float armT = i / (_bonePositions.Count - 1f);
      float smoothing = 1f - armT;

      Vector3 bonePosLocal = _bonePositions[i];
      Vector3 targetBonePosWorld = Vector3.Lerp(shoulderPos, _footTarget.position, armT);
      Vector3 targetBonePosLocal = transform.InverseTransformPoint(targetBonePosWorld);
      Vector3 newBonePosLocal = Mathfx.Damp(bonePosLocal, targetBonePosLocal, smoothing, dt * 10);
      _bonePositions[i] = newBonePosLocal;
    }

    _lineRenderer.SetPosition(_bones.Length, _footTarget.position);

    for (int i = 0; i < _bones.Length; ++i)
    {
      Transform boneA = _bones[i];
      Vector3 boneAPos = transform.TransformPoint(_bonePositions[i]);
      Vector3 boneForward = Mathfx.AxisToVector(_forwardAxis);
      boneA.position = boneAPos;
      _lineRenderer.SetPosition(i, boneAPos);

      if (i + 1 < _bones.Length)
      {
        Vector3 boneBPos = transform.TransformPoint(_bonePositions[i + 1]);
        Vector3 toBonePos = boneBPos - boneAPos;
        boneA.rotation = Quaternion.LookRotation(toBonePos, -transform.forward) * Quaternion.FromToRotation(boneForward, Vector3.forward);

        float boneLengthOriginal = _boneLengths[i];
        float stretchedBoneLength = toBonePos.magnitude;
        boneA.localScale = Vector3.one.WithComponent((int)_forwardAxis, stretchedBoneLength / boneLengthOriginal);
      }
      else
      {
        boneA.rotation = Quaternion.LookRotation(_footTarget.forward, _footTarget.up) * Quaternion.FromToRotation(boneForward, Vector3.forward);
      }
    }
  }

  private void OnDrawGizmos()
  {
    if (_footTarget)
    {
      Gizmos.color = Color.blue;
      Gizmos.DrawWireSphere(_footTarget.position, 0.05f);
    }

    Gizmos.color = Color.white;
    for (int i = 1; _bonePositions != null && i < _bonePositions.Count; ++i)
    {
      Vector3 boneAPos = transform.TransformPoint(_bonePositions[i - 1]);
      Vector3 boneBPos = transform.TransformPoint(_bonePositions[i]);
      Gizmos.DrawLine(boneAPos, boneBPos);
    }

    for (int i = 0; _bonePositions != null && i < _bonePositions.Count; ++i)
    {
      Vector3 bonePos = transform.TransformPoint(_bonePositions[i]);
      Gizmos.DrawWireSphere(bonePos, 0.05f);
    }
  }
}