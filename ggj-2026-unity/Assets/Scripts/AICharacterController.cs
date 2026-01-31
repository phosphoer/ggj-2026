using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class AICharacterController : MonoBehaviour
{
  public enum BehaviorState
  {
    Wander = 0,
    Idle,
    Attack,
    Dead
  };

  public CharacterMovementController Character => _characterMovement;
  public AIPerceptionComponent Perception => _perceptionComponent;
  public AIAnimatorController AIAnimator => _aiAnimation;

  [SerializeField]
  private AIPerceptionComponent _perceptionComponent = null;

  [SerializeField]
  private CharacterMovementController _characterMovement = null;

  [SerializeField]
  private AIAnimatorController _aiAnimation = null;

  [SerializeField]
  private GameObject _deathFX = null;

  [SerializeField]
  private SoundBank _deathSound = null;

  // Behavior State
  private BehaviorState _behaviorState = BehaviorState.Idle;
  private float _timeInBehavior = 0.0f;
  //-- Idle --
  public float IdleMinDuration = 0.5f;
  public float IdleMaxDuration = 3.0f;
  private float _idleDuration = 0.0f;
  //-- Wander --
  public float WanderRange = 10.0f;
  //-- Chase --
  public float ChaseTimeOut = 4.0f;
  public float MaxChaseDistance = 30.0f;
  //-- Attack --
  public float AttackRange = 2.0f;
  public float AttackDuration = 2.0f;
  public float AttackTurnSpeed = 5.0f;
  public float AttackCooldown = 5.0f;
  public float _timeSinceAttack = -1.0f;
  public bool HasAttackedRecently
  {
    get { return (_timeSinceAttack >= 0 && _timeSinceAttack < AttackCooldown); }
  }
  //-- Cower --
  public float CowerDuration = 2.0f;

  // Path Finding State
  public float WaypointTolerance = 1.0f;
  public bool DebugDrawPath = false;
  List<Vector3> _lastPath = new List<Vector3>();
  float _pathRefreshPeriod = -1.0f;
  float _pathRefreshTimer = 0.0f;
  int _pathWaypointIndex = 0;
  float _pathfollowingStuckTimer = 0.0f;
  public bool IsPathFinished
  {
    // Hit end of the path
    get { return (_pathWaypointIndex >= _lastPath.Count); }
  }
  public bool CantMakePathProgress
  {
    // Got stuck on some geomtry following current path
    get { return _pathfollowingStuckTimer > 1.0f; }
  }
  public bool IsPathStale
  {
    get
    {
      return
      (_pathRefreshPeriod >= 0.0f && _pathRefreshTimer <= 0.0f) || // time for a refresh
      IsPathFinished || // Hit end of the path
      CantMakePathProgress; // Got stuck on some geomtry following current path
    }
  }

  Vector3 _spawnLocation = Vector3.zero;

  // Throttle State
  private Vector3 _throttleTarget = Vector3.zero;
  private float _throttleUrgency = 0.5f;
  private bool _hasValidThrottleTarget = false;

  private void Awake()
  {
  }

  private void OnEnable()
  {
  }

  private void OnDisable()
  {
  }

  private void Start()
  {
    _spawnLocation = this.transform.position;
  }

  private void OnDestroy()
  {

  }

  private void Update()
  {
    if (_behaviorState == BehaviorState.Dead)
      return;

    UpdateBehavior();
    UpdatePathRefreshTimer();
    UpdatePathFollowing();
    UpdateMoveVector();
    UpdateAnimationParameters();

    if (DebugDrawPath)
    {
      DrawPath();
    }
  }

  void UpdateBehavior()
  {
    BehaviorState nextBehavior = _behaviorState;

    // Used for attack cooldown
    if (_behaviorState != BehaviorState.Attack && _timeSinceAttack >= 0)
    {
      _timeSinceAttack += Time.deltaTime;
    }

    switch (_behaviorState)
    {
      case BehaviorState.Idle:
        nextBehavior = UpdateBehavior_Idle();
        break;
      case BehaviorState.Wander:
        nextBehavior = UpdateBehavior_Wander();
        break;
      case BehaviorState.Dead:
        break;
    }

    SetBehaviorState(nextBehavior);
  }

  void SetBehaviorState(BehaviorState nextBehavior)
  {
    if (nextBehavior != _behaviorState)
    {
      OnBehaviorStateExited(_behaviorState);
      OnBehaviorStateEntered(nextBehavior);
      _behaviorState = nextBehavior;

      _timeInBehavior = 0.0f;
    }
    else
    {
      _timeInBehavior += Time.deltaTime;
    }
  }

  BehaviorState UpdateBehavior_Idle()
  {
    BehaviorState nextBehavior = BehaviorState.Idle;

    // Give player some extra time if they are sneaking behind us
    float idleTimeout = _perceptionComponent.IsPlayerNearbyBehind ? IdleMaxDuration : _idleDuration;

    // Spotted the player
    if (_perceptionComponent.CanSeePlayer)
    {
      // Do something?
      //nextBehavior = BehaviorState.Chase;
    }
    // Been in idle too long, go somewhere else
    else if (_timeInBehavior >= idleTimeout)
    {
      nextBehavior = BehaviorState.Wander;
    }

    return nextBehavior;
  }

  BehaviorState UpdateBehavior_Wander()
  {
    BehaviorState nextBehavior = BehaviorState.Wander;

    // Spotted the player
    if (_perceptionComponent.CanSeePlayer)
    {
      nextBehavior = BehaviorState.Attack;
    }
    // Have we reached our path destination, chill for a bit
    else if (IsPathFinished || CantMakePathProgress)
    {
      nextBehavior = BehaviorState.Idle;
    }

    return nextBehavior;
  }

  BehaviorState UpdateBehavior_Attack()
  {
    BehaviorState nextBehavior = BehaviorState.Attack;

    if (_timeInBehavior > AttackDuration)
    {
      nextBehavior = BehaviorState.Idle;
    }
    else
    {
      FaceTowardTarget(GetCurrentPlayerLocation(), AttackTurnSpeed);
    }

    return nextBehavior;
  }

  void OnBehaviorStateExited(BehaviorState oldBehavior)
  {
    switch (oldBehavior)
    {
      case BehaviorState.Idle:
        break;
      case BehaviorState.Wander:
        break;
      case BehaviorState.Attack:
        break;
      case BehaviorState.Dead:
        break;
    }
  }

  void OnBehaviorStateEntered(BehaviorState newBehavior)
  {
    switch (newBehavior)
    {
      case BehaviorState.Idle:
        _throttleUrgency = 0.0f; // stop
        _pathRefreshPeriod = -1.0f; // no refresh
        _idleDuration = Random.Range(IdleMinDuration, IdleMaxDuration);
        break;
      case BehaviorState.Wander:
        _throttleUrgency = 0.5f; // half speed
        _pathRefreshPeriod = -1.0f; // manual refresh
        // Pick a path to a wander target
        {
          Vector2 offset = Random.insideUnitCircle * WanderRange;
          Vector3 wanderTarget = _spawnLocation + Vector3.left * offset.x + Vector3.forward * offset.y;
          RecomputePathTo(wanderTarget);
        }
        break;
      case BehaviorState.Attack:
        _throttleUrgency = 0.0f; // Stop and attack in place
        _pathRefreshPeriod = -1.0f; // manual refresh

        _aiAnimation.PlayEmote(AIAnimatorController.EmoteState.Attack);
        _timeSinceAttack = 0.0f; // We just attacked

        break;
      case BehaviorState.Dead:
        // Play death effects to cover the transition
        if (_deathFX != null)
        {
          Instantiate(_deathFX, transform.position, Quaternion.identity);
        }

        AudioManager.Instance.PlaySound(gameObject, _deathSound);

        // Clean ourselves up after a moment
        Destroy(this, 0.1f);
        break;
    }
  }

  void UpdatePathRefreshTimer()
  {
    if (_pathRefreshPeriod >= 0)
    {
      _pathRefreshTimer -= Time.deltaTime;
      // Behavior decides where to recompute path too
    }
  }

  bool RecomputePathTo(Vector3 worldTarget)
  {
    _pathRefreshTimer = _pathRefreshPeriod;
    _pathWaypointIndex = 0;
    _pathfollowingStuckTimer = 0.0f;
    return PathFindManager.Instance.CalculatePathToPoint(transform.position, worldTarget, _lastPath);
  }

  void UpdatePathFollowing()
  {
    if (_pathWaypointIndex < _lastPath.Count)
    {
      // Always throttle at the next waypoint
      Vector3 waypoint = _lastPath[_pathWaypointIndex];
      Vector3 throttleTarget2d = Vector3.ProjectOnPlane(waypoint, Vector3.up);
      Vector3 position2d = Vector3.ProjectOnPlane(this.transform.position, Vector3.up);

      // Advance to the next waypoint 
      if (IsWithingDistanceToTarget2D(throttleTarget2d, WaypointTolerance))
      {
        _pathfollowingStuckTimer = 0.0f;
        _pathWaypointIndex++;
      }
      else
      {
        // If we aren't making progress toward the waypoint, increment the stuck timer
        if (_characterMovement.CurrentVelocity.magnitude < 0.01f)
        {
          _pathfollowingStuckTimer += Time.deltaTime;
        }
        else
        {
          _pathfollowingStuckTimer = 0.0f;
        }
      }
    }

    // Throttle at next waypoint
    if (_pathWaypointIndex < _lastPath.Count)
    {
      Vector3 waypoint = _lastPath[_pathWaypointIndex];

      SetThrottleTarget(waypoint);
    }
    else
    {
      ClearThrottleTarget();
    }
  }

  void DrawPath()
  {
    if (_lastPath.Count <= 0)
      return;

    Vector3 PrevPathPoint = _lastPath[0];
    for (int pathIndex = 1; pathIndex < _lastPath.Count; ++pathIndex)
    {
      Vector3 NextPathPoint = _lastPath[pathIndex];
      Debug.DrawLine(PrevPathPoint, NextPathPoint, Color.red);
      PrevPathPoint = NextPathPoint;
    }
  }

  bool IsWithingDistanceToTarget2D(Vector3 target, float distance)
  {
    Vector3 target2d = Vector3.ProjectOnPlane(target, Vector3.up);
    Vector3 position2d = Vector3.ProjectOnPlane(this.transform.position, Vector3.up);

    return Vector3.Distance(target2d, position2d) <= distance;
  }

  void SetThrottleTarget(Vector3 target)
  {
    _throttleTarget = target;
    _hasValidThrottleTarget = true;
  }

  void ClearThrottleTarget()
  {
    _hasValidThrottleTarget = false;
  }

  void UpdateMoveVector()
  {
    Vector3 throttleDirection = Vector3.zero;

    if (_hasValidThrottleTarget)
    {
      throttleDirection = _throttleTarget - this.transform.position;
      throttleDirection.y = 0;
      throttleDirection = Vector3.Normalize(throttleDirection);
    }

    _characterMovement.MoveVector = throttleDirection * _throttleUrgency;
  }

  void UpdateAnimationParameters()
  {
    if (_characterMovement.CurrentVelocity.magnitude > 0.01f)
    {
      _aiAnimation.CurrentLocomotionSpeed = _characterMovement.CurrentVelocity.magnitude;
      _aiAnimation.CurrentLocomotionState = AIAnimatorController.LocomotionState.Move;
    }
    else
    {
      _aiAnimation.CurrentLocomotionSpeed = 0;
      _aiAnimation.CurrentLocomotionState = AIAnimatorController.LocomotionState.Idle;
    }
  }

  void FaceTowardTarget(Vector3 faceTarget, float faceAnimationSpeed)
  {
    Vector3 targetFoward = faceTarget - transform.position;
    Vector3 target2d = Vector3.ProjectOnPlane(targetFoward, Vector3.up);

    Quaternion desiredForwardRot = Quaternion.LookRotation(target2d);
    transform.rotation = Mathfx.Damp(transform.rotation, desiredForwardRot, 0.25f, Time.deltaTime * faceAnimationSpeed);
  }

  Vector3 GetCurrentPlayerLocation()
  {
    return Perception.LastSeenPlayerLocation;
  }
}