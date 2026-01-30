using UnityEngine;

public class GameCharacterController : MonoBehaviour
{
  public event System.Action OutOfBounds;

  public Transform CameraRoot => _cameraRoot;
  public Transform HeldItemRoot => _heldItemRoot;
  public ItemController HeldItem => _heldItem;
  public InteractionController InteractionController => _interactionController;
  public Slappable Slappable => _slappable;

  [Range(-1, 1)]
  public float MoveAxis = 0.0f;

  [Range(-1, 1)]
  public float StrafeAxis = 0.0f;

  [Range(-1, 1)]
  public float LookHorizontalAxis = 0.0f;

  [Range(-1, 1)]
  public float LookVerticalAxis = 0.0f;

  public RangedFloat LookVerticalRange = new RangedFloat(-45, 45);

  [SerializeField]
  private InteractionController _interactionController = null;

  [SerializeField]
  private Slappable _slappable = null;

  [SerializeField]
  private Animator _animator = null;

  [SerializeField]
  private AnimatorCallbacks _animatorCallback = null;

  [SerializeField]
  private Transform _cameraRoot = null;

  [SerializeField]
  private Transform _heldItemRoot = null;

  [SerializeField]
  private Transform _attackPos = null;

  [SerializeField]
  private LayerMask _groundLayer = default(LayerMask);

  [SerializeField]
  private LayerMask _obstacleLayer = default(LayerMask);

  [SerializeField]
  private LayerMask _attackLayer = default(LayerMask);

  [SerializeField]
  private float _groundRaycastRadius = 0.4f;

  [SerializeField]
  private float _obstacleRaycastRadius = 0.7f;

  [SerializeField]
  private float _minDistToObstacle = 0.8f;

  [SerializeField]
  private float _raycastUpStartOffset = 1.0f;

  [SerializeField]
  private float _terrainAlignmentSpeed = 3.0f;

  [SerializeField]
  private float _moveSpeed = 1.0f;

  [SerializeField]
  private float _turnSpeed = 90.0f;

  [SerializeField]
  private float _gravity = 5;

  [SerializeField]
  private float _slapCooldownTime = 1;

  [SerializeField]
  private SoundBank _dropItemSound = null;

  [SerializeField]
  private SoundBank _collectToothSound = null;

  [SerializeField]
  private SoundBank _collectFishSound = null;

  [SerializeField]
  private SoundBank _attackSound = null;

  [SerializeField]
  private SoundBank _hitSound = null;

  private RaycastHit _groundRaycast;
  private RaycastHit _obstacleRaycast;
  private Vector3 _lastGroundPos;
  private ItemController _heldItem;
  private float _holdItemBlend;
  private float _slapCooldownTimer;
  private bool _isOutOfBounds;
  private Vector3 _slapPushVec;
  private Collider[] _overlapColliders = new Collider[10];

  private Vector3 _raycastStartPos => transform.position + transform.up * _raycastUpStartOffset;

  private static int kAnimMoveSpeed = Animator.StringToHash("MoveSpeed");
  private static int kAnimHoldItemBlend = Animator.StringToHash("HoldItemBlend");
  private static int kAnimIsMoving = Animator.StringToHash("IsMoving");
  private static int kAnimIsStunned = Animator.StringToHash("IsStunned");
  private static int kAnimAttack = Animator.StringToHash("Attack");
  private static int kAnimRecoil = Animator.StringToHash("Recoil");

  public void Interact()
  {
    if (_interactionController.ClosestInteractable != null)
    {
      _interactionController.TriggerInteraction();
    }
    else if (_heldItem != null)
    {
      DropItem(true);
    }
    else
    {
      Attack();
    }
  }

  public void Attack()
  {
    if (_slapCooldownTimer <= 0)
    {
      _slapCooldownTimer = _slapCooldownTime;
      SetAnimatorTrigger(kAnimAttack);

      if (_attackSound != null)
      {
        AudioManager.Instance.PlaySound(_attackSound);
      }
    }
  }

  private void OnEnable()
  {
    _interactionController.InteractionTriggered += OnInteractionTriggered;
    _slappable.Slapped += OnSlapped;
    _animatorCallback.AddCallback("OnAttackFrame", OnAttackFrame);
  }

  private void OnDisable()
  {
    _interactionController.InteractionTriggered -= OnInteractionTriggered;
    _slappable.Slapped -= OnSlapped;
    _animatorCallback.RemoveCallback("OnAttackFrame", OnAttackFrame);
  }

  private void Update()
  {
    _slapCooldownTimer -= Time.deltaTime;
    if (transform.position.y < -5 && !_isOutOfBounds)
    {
      OutOfBounds?.Invoke();
    }

    // Calculate next position based on movement
    float moveAxisTotal = Mathf.Clamp01(Mathf.Abs(MoveAxis) + Mathf.Abs(StrafeAxis));
    Vector3 moveVec = (transform.forward * MoveAxis + transform.right.WithY(0) * StrafeAxis).NormalizedSafe() * moveAxisTotal;
    Vector3 newPosition = transform.position + moveVec * _moveSpeed * Time.deltaTime;

    newPosition += _slapPushVec * Time.deltaTime;
    _slapPushVec = Mathfx.Damp(_slapPushVec, Vector3.zero, 0.25f, Time.deltaTime * 3);

    // Snap and align to ground
    Vector3 raycastDir = -transform.up + (transform.forward * MoveAxis + transform.right * StrafeAxis) * 0.5f;
    if (Physics.SphereCast(_raycastStartPos, _groundRaycastRadius, raycastDir, out _groundRaycast, 3.0f, _groundLayer))
    {
      _lastGroundPos = _groundRaycast.point;

      Vector3 toGroundPoint = _groundRaycast.point - newPosition;
      newPosition += Vector3.ClampMagnitude(toGroundPoint, 1f) * Time.deltaTime * _gravity;

      Quaternion desiredRot = Quaternion.FromToRotation(transform.up, _groundRaycast.normal) * transform.rotation;
      transform.rotation = Mathfx.Damp(transform.rotation, desiredRot, 0.25f, Time.deltaTime * _terrainAlignmentSpeed);
    }
    // If no ground, go towards where it was last
    else
    {
      Vector3 fallDir = Vector3.down;
      Quaternion desiredRot = Quaternion.FromToRotation(transform.up, -fallDir) * transform.rotation;

      newPosition += fallDir * Time.deltaTime * _gravity;
      transform.rotation = Mathfx.Damp(transform.rotation, desiredRot, 0.25f, Time.deltaTime * _terrainAlignmentSpeed);
    }

    // Collide with obstacles
    Vector3 velocity = newPosition - transform.position;
    if (Physics.SphereCast(transform.position, _obstacleRaycastRadius, velocity.NormalizedSafe(), out _obstacleRaycast, _minDistToObstacle + 1, _obstacleLayer))
    {
      // Find the plane representing the point + normal we hit
      Plane hitPlane = new Plane(_obstacleRaycast.normal, _obstacleRaycast.point);

      // Now project our position onto that plane and use the vector from
      // the projected point to our pos as the adjusted normal
      Vector3 closestPoint = hitPlane.ClosestPointOnPlane(newPosition);
      Vector3 closestPointToPos = newPosition - closestPoint;

      // "Clamp" our distance from the plane to a min distance
      float planeDist = closestPointToPos.magnitude;
      float adjustedDist = Mathf.Max(planeDist, _minDistToObstacle);
      newPosition = closestPoint + closestPointToPos.normalized * adjustedDist;
    }

    // Update animation
    float moveDir = Mathf.Sign(MoveAxis);
    SetAnimatorFloat(kAnimMoveSpeed, moveAxisTotal * moveDir);
    SetAnimatorBool(kAnimIsMoving, moveAxisTotal > 0);

    float holdItemBlendTarget = _heldItem != null ? 1 : 0;
    _holdItemBlend = Mathfx.Damp(_holdItemBlend, holdItemBlendTarget, 0.25f, Time.deltaTime * 3);
    SetAnimatorFloat(kAnimHoldItemBlend, _holdItemBlend);


    // Apply movement
    transform.position = newPosition;
    transform.Rotate(Vector3.up, LookHorizontalAxis * _turnSpeed * Time.deltaTime, Space.Self);
    _cameraRoot.Rotate(Vector3.right, -LookVerticalAxis * _turnSpeed * Time.deltaTime, Space.Self);

    // Clamp camera look
    float verticalAngle = Vector3.SignedAngle(transform.forward, _cameraRoot.forward, transform.right);
    float delta = 0;
    if (verticalAngle < LookVerticalRange.MinValue)
      delta = verticalAngle - LookVerticalRange.MinValue;
    else if (verticalAngle > LookVerticalRange.MaxValue)
      delta = verticalAngle - LookVerticalRange.MaxValue;

    _cameraRoot.Rotate(Vector3.right, -delta, Space.Self);
  }

  private void SetAnimatorTrigger(int triggerId)
  {
    _animator.SetTrigger(triggerId);
  }

  private void SetAnimatorFloat(int triggerId, float value)
  {
    _animator.SetFloat(triggerId, value);
  }

  private void SetAnimatorBool(int triggerId, bool value)
  {
    _animator.SetBool(triggerId, value);
  }

  private void OnSlapped(GameCharacterController fromCharacter)
  {
    Vector3 slapDir = (transform.position - fromCharacter.transform.position).normalized;
    slapDir.y += 1;
    _slapPushVec = slapDir * 10;

    if (fromCharacter._heldItem != null)
    {
      _slapPushVec *= 3;
    }

    SetAnimatorTrigger(kAnimRecoil);
    DropItem(false);

    if (_hitSound != null)
    {
      AudioManager.Instance.PlaySound(_hitSound);
    }
  }

  private void OnAttackFrame()
  {
    if (_attackSound != null)
    {
      AudioManager.Instance.PlaySound(_attackSound);
    }

    int overlapCount = Physics.OverlapSphereNonAlloc(_attackPos.position, 0.25f, _overlapColliders, _attackLayer, QueryTriggerInteraction.Collide);
    for (int i = 0; i < overlapCount; ++i)
    {
      Collider c = _overlapColliders[i];
      Slappable slappable = c.GetComponentInParent<Slappable>();
      if (slappable != null && slappable != _slappable)
      {
        slappable.ReceiveSlap(fromCharacter: this);
      }
    }
  }

  private void PickupItem(ItemController item)
  {
    DropItem(false);
    item.transform.parent = _heldItemRoot;
    item.transform.SetIdentityTransformLocal();
    item.SetInteractable(false);
    _heldItem = item;

    // TODO: Play sound
  }

  private void DropItem(bool playSound)
  {
    if (_heldItem != null)
    {
      _heldItem.SetInteractable(true);
      _heldItem.transform.parent = null;
      _heldItem.transform.localScale = Vector3.one;
      _heldItem = null;

      if (playSound && _dropItemSound)
      {
        AudioManager.Instance.PlaySound(_dropItemSound);
      }
    }
  }

  private void OnInteractionTriggered(Interactable interactable)
  {
    Debug.Log($"{name} interacted with {interactable.name}");
    ItemController item = interactable.GetComponent<ItemController>();
    if (item != null)
    {
      PickupItem(item);
    }
  }
}