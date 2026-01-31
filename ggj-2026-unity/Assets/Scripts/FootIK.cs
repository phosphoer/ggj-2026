using UnityEngine;
using System.Collections.Generic;

public class FootIK : MonoBehaviour
{
  public int MaxSteppingFeet
  {
    get => _maxSteppingFeet;
    set => _maxSteppingFeet = value;
  }

  public RangedFloat FootStepDuration
  {
    get => _footStepDurationRange;
    set => _footStepDurationRange = value;
  }

  public RangedFloat FootStepThreshold
  {
    get => _footStepThresholdRange;
    set => _footStepThresholdRange = value;
  }

  public Vector3 SmoothVelocity => _smoothVelocity;
  public float AverageStepT => _stepTAverage;
  public float TotalStepT => _totalStepCount + _stepTAverage;
  public int TotalStepCount => _totalStepCount;
  public int CurrentStepSide => _currentStepSide;
  public float LeftSideLift => _leftSideLift;
  public float RightSideLift => _rightSideLift;
  public IReadOnlyList<FootInfo> Feet => _feet;

  [SerializeField] private List<FootInfo> _feet = new();
  [SerializeField] private RangedFloat _footStepThresholdRange = new RangedFloat(0.2f, 0.4f);
  [SerializeField] private RangedFloat _footStepDurationRange = new RangedFloat(0.2f, 0.5f);
  [SerializeField] private int _maxSteppingFeet = 1;
  [SerializeField] private AnimationCurve _footStepHeightCurve = null;
  [SerializeField] private float _footStepHeight = 1;
  [SerializeField] private float _footHeightOffset = 0;
  [SerializeField] private float _minTimeBetweenSteps = 0.1f;
  [SerializeField] private LayerMask _footGroundMask = default;
  [SerializeField] private float _maxFootGroundSnapDist = 20;
  [SerializeField] private float _maxStrideSpeed = 2;
  [SerializeField] private float _footVelocityOffsetScale = 0.2f;

  private int _steppingFeetCount = 0;
  private float _stepOffsetTimer;
  private float _stepTAverage;
  private int _totalStepCount;
  private int _currentStepSide;
  private float _smoothStrideT;
  private float _leftSideLift;
  private float _rightSideLift;
  private Vector3 _lastPosition;
  private Vector3 _smoothVelocity;

  [System.Serializable]
  public struct FootInfo
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

  public void AddFoot(FootInfo footInfo)
  {
    footInfo.RestPosLocal = footInfo.Root.localPosition;
    footInfo.RestRotLocal = footInfo.Root.localRotation;
    footInfo.WorldPos = SnapPositionToGround(footInfo.Root.position);
    footInfo.WorldRot = footInfo.Root.rotation;
    _feet.Add(footInfo);
  }

  public void ClearFeet()
  {
    _feet.Clear();
  }

  private void Awake()
  {
    for (int i = 0; i < _feet.Count; ++i)
    {
      FootInfo footInfo = _feet[i];
      footInfo.RestPosLocal = footInfo.Root.localPosition;
      footInfo.RestRotLocal = footInfo.Root.localRotation;
      footInfo.WorldPos = SnapPositionToGround(footInfo.Root.position);
      footInfo.WorldRot = footInfo.Root.rotation;
      _feet[i] = footInfo;
    }
  }

  private void Start()
  {
    _lastPosition = transform.position;
  }

  private void LateUpdate()
  {
    float dt = Time.deltaTime;
    _stepOffsetTimer -= dt;
    _stepTAverage = 0;

    float invDt = dt < Mathf.Epsilon ? 0 : 1 / dt;
    Vector3 posDelta = transform.position - _lastPosition;
    _smoothVelocity = Mathfx.Damp(_smoothVelocity, posDelta * invDt, 0.25f, dt * 10);
    _lastPosition = transform.position;
    _smoothStrideT = Mathf.Clamp01(_smoothVelocity.magnitude / _maxStrideSpeed);

    _leftSideLift = 0;
    _rightSideLift = 0;

    int nextFootStepIndex = -1;
    float biggestStepDistance = 0;
    int currentSteppingCount = 0;
    for (int i = 0; i < _feet.Count; ++i)
    {
      FootInfo footInfo = _feet[i];

      Vector3 restPosLocal = footInfo.RestPosLocal + transform.InverseTransformDirection(_smoothVelocity.normalized) * _smoothStrideT * _footVelocityOffsetScale;
      Vector3 restPosWorld = footInfo.Root.parent.TransformPoint(restPosLocal);
      Vector3 restPosWorldSnapped = SnapPositionToGround(restPosWorld);
      Quaternion restRotWorld = footInfo.Root.parent.rotation * footInfo.RestRotLocal;
      Vector3 toRestPos = restPosWorldSnapped - footInfo.WorldPos;
      float distToRestPos = toRestPos.magnitude;

      if (footInfo.IsStepping)
      {
        float stepDuration = _footStepDurationRange.Lerp(_smoothStrideT);
        footInfo.StepTimer += dt;
        footInfo.StepT = Mathf.Clamp01(footInfo.StepTimer / stepDuration);

        float smoothT = Mathf.SmoothStep(0, 1, footInfo.StepT);
        float stepHeight = _footStepHeightCurve.Evaluate(smoothT) * _footStepHeight;
        footInfo.WorldPos = Vector3.Lerp(footInfo.StepStartPos, restPosWorldSnapped, smoothT) + Vector3.up * stepHeight;
        footInfo.WorldRot = Quaternion.Slerp(footInfo.StepStartRot, restRotWorld, smoothT);

        if (footInfo.RestPosLocal.x < 0)
          _leftSideLift = Mathf.Max(_leftSideLift, stepHeight);
        else
          _rightSideLift = Mathf.Max(_rightSideLift, stepHeight);

        _stepTAverage += footInfo.StepT;
        currentSteppingCount += 1;

        if (footInfo.StepT >= 1)
        {
          footInfo.IsStepping = false;
          _steppingFeetCount -= 1;
          _totalStepCount += 1;
        }
      }
      else
      {
        float stepThreshold = _footStepThresholdRange.Lerp(_smoothStrideT);
        if (distToRestPos > biggestStepDistance && distToRestPos > stepThreshold)
        {
          nextFootStepIndex = i;
          biggestStepDistance = distToRestPos;
        }
      }

      // Assign transform info to foot object
      footInfo.Root.position = footInfo.WorldPos + Vector3.up * _footHeightOffset;
      footInfo.Root.rotation = footInfo.WorldRot;

      _feet[i] = footInfo;
    }

    if (currentSteppingCount > 0)
    {
      _stepTAverage /= currentSteppingCount;
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
      _currentStepSide = (int)Mathf.Sign(footInfo.RestPosLocal.x);
      _feet[nextFootStepIndex] = footInfo;
    }
  }

  private void OnValidate()
  {
    for (int i = 0; _feet != null && i < _feet.Count; ++i)
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