using UnityEngine;

public class FootIK : MonoBehaviour
{
  [SerializeField]
  private FootInfo[] _feet = null;

  [SerializeField]
  private float _footStepThreshold = 1.0f;

  [SerializeField]
  private float _footStepDuration = 1f;

  [SerializeField]
  private int _maxSteppingFeet = 1;

  [SerializeField]
  private AnimationCurve _footStepHeightCurve = null;

  [SerializeField]
  private float _footStepHeight = 1;

  [SerializeField]
  private float _minTimeBetweenSteps = 0.1f;

  [SerializeField]
  private LayerMask _footGroundMask = default;

  [SerializeField]
  private float _maxFootGroundSnapDist = 20;

  private int _steppingFeetCount = 0;
  private float _stepOffsetTimer;

  [System.Serializable]
  private struct FootInfo
  {
    [Header("Config")]
    [HideInInspector]
    public string Name;
    public Transform Root;

    [Header("Runtime")]
    public Vector3 RestPosLocal;
    public Quaternion RestRotLocal;
    public Vector3 WorldPos;
    public Quaternion WorldRot;
    public Vector3 StepStartPos;
    public Quaternion StepStartRot;
    public float StepTimer;
    public float StepT;
    public bool IsStepping;
  }

  private void Awake()
  {
    for (int i = 0; i < _feet.Length; ++i)
    {
      FootInfo footInfo = _feet[i];
      footInfo.RestPosLocal = footInfo.Root.localPosition;
      footInfo.RestRotLocal = footInfo.Root.localRotation;
      footInfo.WorldPos = SnapPositionToGround(footInfo.Root.position);
      footInfo.WorldRot = footInfo.Root.rotation;
      _feet[i] = footInfo;
    }
  }

  private void Update()
  {
    float dt = Time.deltaTime;
    _stepOffsetTimer -= dt;

    int nextFootStepIndex = -1;
    float biggestStepDistance = 0;
    for (int i = 0; i < _feet.Length; ++i)
    {
      FootInfo footInfo = _feet[i];

      Vector3 restPosWorld = SnapPositionToGround(footInfo.Root.parent.TransformPoint(footInfo.RestPosLocal));
      Quaternion restRotWorld = footInfo.Root.parent.rotation * footInfo.RestRotLocal;
      Vector3 toRestPos = restPosWorld - footInfo.WorldPos;
      float distToRestPos = toRestPos.magnitude;

      if (footInfo.IsStepping)
      {
        footInfo.StepTimer += dt;
        footInfo.StepT = Mathf.Clamp01(footInfo.StepTimer / _footStepDuration);

        float smoothT = Mathf.SmoothStep(0, 1, footInfo.StepT);
        float stepHeight = _footStepHeightCurve.Evaluate(smoothT) * _footStepHeight;
        footInfo.WorldPos = Vector3.Lerp(footInfo.StepStartPos, restPosWorld, smoothT) + Vector3.up * stepHeight;
        footInfo.WorldRot = Quaternion.Slerp(footInfo.StepStartRot, restRotWorld, smoothT);

        if (footInfo.StepT >= 1)
        {
          footInfo.IsStepping = false;
          _steppingFeetCount -= 1;
        }
      }
      else
      {
        if (distToRestPos > biggestStepDistance && distToRestPos > _footStepThreshold)
        {
          nextFootStepIndex = i;
          biggestStepDistance = distToRestPos;
        }
      }

      // Assign transform info to foot object
      footInfo.Root.position = footInfo.WorldPos;
      footInfo.Root.rotation = footInfo.WorldRot;

      _feet[i] = footInfo;
    }

    if (_steppingFeetCount < _maxSteppingFeet && nextFootStepIndex >= 0 && _stepOffsetTimer <= 0)
    {
      _stepOffsetTimer = _minTimeBetweenSteps;
      FootInfo footInfo = _feet[nextFootStepIndex];
      footInfo.IsStepping = true;
      footInfo.StepTimer = 0;
      footInfo.StepStartPos = footInfo.WorldPos;
      footInfo.StepStartRot = footInfo.WorldRot;
      _steppingFeetCount += 1;
      _feet[nextFootStepIndex] = footInfo;
    }
  }

  private void OnValidate()
  {
    for (int i = 0; _feet != null && i < _feet.Length; ++i)
    {
      FootInfo footInfo = _feet[i];
      footInfo.Name = footInfo.Root != null ? footInfo.Root.name : "Unassigned Foot";
      _feet[i] = footInfo;
    }
  }

  private Vector3 SnapPositionToGround(Vector3 worldPos)
  {
    if (Physics.Raycast(worldPos + transform.up * _maxFootGroundSnapDist * 0.5f, -transform.up, out RaycastHit hitInfo, _maxFootGroundSnapDist, _footGroundMask))
    {
      worldPos.y = hitInfo.point.y;
    }

    return worldPos;
  }
}