using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class PlayerActorController : MonoBehaviour
{
  public static IReadOnlyList<PlayerActorController> Instances => _instances;

  public float AnimIdleBobScale = 0.05f;
  public float AnimIdleBobSpeed = 3f;
  public float AnimIdleWiggleScale = 5;
  public float AnimIdleWiggleSpeed = 1;

  public float XPDamageMultiplier = 0.5f;

  public bool IsPossessing => _currentPossessable != null;
  public Rewired.Player PlayerInput => _playerInput;
  public int PlayerIndex => _playerIndex;
  public string PlayerColorName => _playerColorName;

  [SerializeField] private ObjectActorController _actor = null;
  [SerializeField] private PlayerAnimator _playerAnimator = null;
  [SerializeField] private Transform _playerVisualRoot = null;
  [SerializeField] private GameObject _playerVisual = null;
  [SerializeField] private FootIK _footIK = null;
  [SerializeField] private InteractionController _interaction = null;
  [SerializeField] private LegNoodleController _legPrefab = null;
  [SerializeField] private GameObject _footPrefab = null;
  [SerializeField] private MaskController _maskPrefab = null;
  [SerializeField] private Transform _maskRoot = null;
  [SerializeField] private Light _light = null;
  [SerializeField] private SkinnedMeshRenderer _bodyMesh = null;
  [SerializeField] private SkinnedMeshRenderer[] _faceMeshes;
  [SerializeField] private Spring _leanSpring = default;

  private Rewired.Player _playerInput;
  private int _playerIndex = -1;
  private string _playerColorName = "";
  private PossessableObject _currentPossessable;
  private List<LegNoodleController> _legs = new();
  private List<GameObject> _feet = new();
  private float _xp;
  private float _animTimer;
  private float _standHeightOffset;
  private float _attackCooldownTimer;
  private bool _isCharging;
  private float _chargeTimer;
  private float _leanAmount;
  private float _attackHitboxTimer;
  private Vector2 _chargeDirection;
  private ParticleSystem _spookAttackFx;
  private SpookHitBox _spookAttackHitbox;
  private MaskController _currentMask;
  private Transform _possessableOriginalParent;

  private static List<PlayerActorController> _instances = new();

  // Reset static state for editor without domain reload
#if UNITY_EDITOR
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
  private static void EditorInit()
  {
    _instances.Clear();
  }
#endif

  public void SetPlayerIndex(int playerIndex)
  {
    _playerIndex = playerIndex;
    _playerInput = Rewired.ReInput.players.GetPlayer(playerIndex);
  }

  public void SetPlayerColor(PlayerColors colorInfo)
  {
    _playerColorName = colorInfo.ColorName;

    _light.color = colorInfo.BodyColor.color;

    if (_bodyMesh != null)
    {
      _bodyMesh.material = colorInfo.BodyColor;
    }

    foreach (SkinnedMeshRenderer faceMesh in _faceMeshes)
    {
      faceMesh.material = colorInfo.FaceColor;
    }

    if (colorInfo.MaskPrefab)
    {
      SetPlayerMaskPrefab(colorInfo.MaskPrefab);
    }
  }

  public void SetPlayerMaskPrefab(MaskController maskPrefab)
  {
    if (_currentMask)
    {
      Destroy(_currentMask.gameObject);
      _currentMask = null;
    }

    _maskPrefab = maskPrefab;

    if (_maskPrefab)
    {
      _currentMask = Instantiate(_maskPrefab, _maskRoot);
      _currentMask.transform.SetIdentityTransformLocal();
    }
  }

  public void PossessObject(PossessableObject possessable)
  {
    Debug.Log($"Possessing object {possessable.name}");

    _interaction.enabled = false;

    ResetLegs();

    _playerAnimator.PlayPossess();

    // Assign new possessable
    _possessableOriginalParent = possessable.transform.parent;
    _currentPossessable = possessable;
    _currentPossessable.transform.parent = _playerVisualRoot;

    if (_maskPrefab)
    {
      _currentPossessable.EquipMask(_maskPrefab);
    }

    _actor.MoveSpeed = _currentPossessable.MoveSpeed;
    _actor.RotateSpeed = _currentPossessable.RotateSpeed;

    AnimIdleBobScale = _currentPossessable.AnimIdleBobScale;
    AnimIdleBobSpeed = _currentPossessable.AnimIdleBobSpeed;
    AnimIdleWiggleScale = _currentPossessable.AnimIdleWiggleScale;
    AnimIdleWiggleSpeed = _currentPossessable.AnimIdleWiggleSpeed;

    if (_currentPossessable.SFXPossess)
      AudioManager.Instance.PlaySound(gameObject, _currentPossessable.SFXPossess);

    Collider[] propColliders = _currentPossessable.GetComponentsInChildren<Collider>();
    foreach (var c in propColliders)
      c.enabled = false;

    Rigidbody rb = _currentPossessable.GetComponent<Rigidbody>();
    if (rb)
    {
      rb.isKinematic = true;
    }

    Interactable interactable = _currentPossessable.GetComponent<Interactable>();
    if (interactable)
    {
      interactable.enabled = false;
    }

    // Set up foot ik info
    _footIK.MaxSteppingFeet = _currentPossessable.FootStepCount;
    _footIK.FootStepDuration = new RangedFloat(_currentPossessable.FootStepDuration, _currentPossessable.FootStepDuration * 0.2f);
    _footIK.FootStepThreshold = new RangedFloat(_currentPossessable.FootStepThreshold * 0.5f, _currentPossessable.FootStepThreshold);
    _footIK.FootStepHeight = _currentPossessable.FootStepHeight;

    // Set up feet
    foreach (var legSocket in _currentPossessable.LegSockets)
    {
      GameObject footObj = Instantiate(_footPrefab, transform);
      footObj.transform.localPosition = _currentPossessable.transform.InverseTransformPoint(legSocket.position).WithY(0);
      footObj.transform.localScale = Vector3.one * _currentPossessable.FootSize;
      _feet.Add(footObj);

      FootIK.FootInfo footInfo = default;
      footInfo.Root = footObj.transform;
      footInfo.Name = $"foot-{_footIK.Feet.Count}";
      _footIK.AddFoot(footInfo);

      LegNoodleController leg = Instantiate(_legPrefab, _currentPossessable.transform);
      leg.transform.position = legSocket.position;
      leg.FootTarget = footInfo.Root;
      leg.LegThickness = _currentPossessable.LegThickness;
      leg.InitializeLeg();
      _legs.Add(leg);
    }

    // Hide player visual
    _playerVisual.SetActive(false);
  }

  public void StopPossessing()
  {
    if (_currentPossessable)
    {
      _interaction.enabled = true;
      ResetLegs();
      _playerVisual.SetActive(true);

      _playerAnimator.PlayDepossess();
      _currentPossessable.UnequipMask();

      if (_currentPossessable.SFXDepossess)
        AudioManager.Instance.PlaySound(gameObject, _currentPossessable.SFXDepossess);

      Collider[] propColliders = _currentPossessable.GetComponentsInChildren<Collider>();
      foreach (var c in propColliders)
        c.enabled = true;

      Rigidbody rb = _currentPossessable.GetComponent<Rigidbody>();
      if (rb)
      {
        rb.isKinematic = _currentPossessable.PostPossessKinematicState;
      }

      Interactable interactable = _currentPossessable.GetComponent<Interactable>();
      if (interactable)
      {
        interactable.enabled = true;
      }

      _currentPossessable.transform.parent = _possessableOriginalParent;
      _currentPossessable = null;
    }
  }

  public void DoSpookAttack()
  {
    if (_currentPossessable)
    {
      var attackParams = _currentPossessable.AttackParams;
      if (attackParams.SpookAttackFX && attackParams.SpookFXRoot)
      {
        _spookAttackFx = Instantiate(attackParams.SpookAttackFX, attackParams.SpookFXRoot);
      }

      if (attackParams.SpookAttackSFX)
      {
        AudioManager.Instance.PlaySound(gameObject, attackParams.SpookAttackSFX);
      }

      _attackCooldownTimer = 5;

      if (attackParams.Type == SpookAttackType.Charge)
      {
        StartCharge();
      }
      else if (attackParams.Type == SpookAttackType.Shoot)
      {
        Shoot();
      }
      else if (attackParams.Type == SpookAttackType.AOE)
      {
        Burst();
      }

      if (_spookAttackHitbox)
      {
        _spookAttackHitbox.Damage = attackParams.FearDamage;
        _spookAttackHitbox.DamageDealt += OnDamageDealt;
      }
    }
  }

  private void OnDamageDealt()
  {
    float xpAmount = _spookAttackHitbox.Damage * XPDamageMultiplier;
    _xp += xpAmount;
    Debug.Log($"Player {_playerIndex} got {xpAmount} and now has {_xp} total xp");
  }

  private void StartCharge()
  {
    var attackParams = _currentPossessable.AttackParams;
    _isCharging = true;
    _chargeTimer = attackParams.ChargeDuration;
    _actor.SprintSpeed = attackParams.ChargeSpeed;
    _chargeDirection = _actor.LookAxis.normalized;
    _actor.IsSprinting = true;

    _spookAttackHitbox = new GameObject("spook-attack-hitbox").AddComponent<SpookHitBox>();
    _spookAttackHitbox.transform.parent = attackParams.SpookAttackRoot;
    var collider = _spookAttackHitbox.gameObject.AddComponent<SphereCollider>();
    collider.isTrigger = true;
    collider.radius = attackParams.ChargeAttackRadius;
  }

  private void StopCharge()
  {
    Destroy(_spookAttackHitbox.gameObject);
    _spookAttackHitbox = null;

    _isCharging = false;
    _actor.IsSprinting = false;

    if (_spookAttackFx)
    {
      _spookAttackFx.DestroyOnStop();
      _spookAttackFx.Stop();
      _spookAttackFx = null;
    }
  }

  private void Shoot()
  {
    var attackParams = _currentPossessable.AttackParams;
    _spookAttackHitbox = new GameObject("spook-attack-hitbox").AddComponent<SpookHitBox>();
    _spookAttackHitbox.transform.parent = attackParams.SpookAttackRoot;
    _spookAttackHitbox.transform.SetIdentityTransformLocal();
    _attackHitboxTimer = 1;

    var collider = _spookAttackHitbox.gameObject.AddComponent<BoxCollider>();
    collider.isTrigger = true;
    collider.size = new Vector3(attackParams.ShootAttackWidth, attackParams.ShootAttackWidth, attackParams.ShootAttackRange);
    collider.center = Vector3.forward * attackParams.ShootAttackRange * 0.5f;

    _leanSpring.Velocity -= attackParams.ShootRecoil;
  }

  private void Burst()
  {
    var attackParams = _currentPossessable.AttackParams;
    _spookAttackHitbox = new GameObject("spook-attack-hitbox").AddComponent<SpookHitBox>();
    _spookAttackHitbox.transform.parent = attackParams.SpookAttackRoot;
    _spookAttackHitbox.transform.SetIdentityTransformLocal();
    _attackHitboxTimer = 1;

    if (_spookAttackFx)
    {
      var fxShape = _spookAttackFx.shape;
      fxShape.radius = attackParams.AOERadius;
    }

    var collider = _spookAttackHitbox.gameObject.AddComponent<SphereCollider>();
    collider.isTrigger = true;
    collider.radius = attackParams.AOERadius;

    _leanSpring.Velocity -= attackParams.ShootRecoil;
  }

  private bool CanInteract(Interactable interactable)
  {
    PossessableObject possessable = interactable.GetComponent<PossessableObject>();
    if (possessable)
    {
      return _xp >= possessable.RequiredXPThreshold;
    }

    return true;
  }

  private void Awake()
  {
    SetPlayerMaskPrefab(_maskPrefab);

    _interaction.SetInteractPredicate(CanInteract);
  }

  private void OnEnable()
  {
    _interaction.InteractionTriggered += OnInteraction;
    _instances.Add(this);
  }

  private void OnDisable()
  {
    _interaction.InteractionTriggered -= OnInteraction;
    _instances.Remove(this);
  }

  private void Update()
  {
    if (_playerIndex == -1)
    {
      // Local debugging case:
      // GameController hasn't assigned a player, so just pick the first one
      SetPlayerIndex(0);
    }

    float horizontalAxis = _playerInput.GetAxis(RewiredConsts.Action.MoveHorizontal);
    float forwardAxis = _playerInput.GetAxis(RewiredConsts.Action.MoveForward);

    Vector2 inputAxis = new Vector2(horizontalAxis, forwardAxis);
    Vector3 inputAxisCameraLocal = inputAxis.OnXZPlane();
    Vector3 inputAxisWorld = MainCamera.Instance.CachedTransform.TransformDirection(inputAxisCameraLocal);
    Vector2 inputAxis2D = inputAxisWorld.XZ();
    _actor.MoveAxis = Mathfx.Damp(_actor.MoveAxis, inputAxis2D, 0.25f, Time.deltaTime * 3);

    if (_isCharging)
    {
      _chargeTimer -= Time.deltaTime;
      _actor.MoveAxis = _chargeDirection;
      if (_chargeTimer <= 0)
      {
        StopCharge();
      }
    }
    else
    {
      _attackCooldownTimer -= Time.deltaTime;
    }

    if (_attackHitboxTimer > 0 && _spookAttackHitbox)
    {
      _attackHitboxTimer -= Time.deltaTime;
      if (_attackHitboxTimer <= 0)
      {
        Destroy(_spookAttackHitbox.gameObject);
        _spookAttackHitbox = null;
      }
    }

    _animTimer += Time.deltaTime;
    Vector3 posOffset = Vector3.up * _standHeightOffset;
    _playerVisualRoot.localPosition = Vector3.up * Mathf.Sin(_animTimer * AnimIdleBobSpeed) * AnimIdleBobScale + posOffset;

    _leanSpring = Spring.UpdateSpring(_leanSpring, Time.deltaTime);

    float leanScale = _currentPossessable != null ? _currentPossessable.AnimWalkLeanScale : 20;
    float targetRot = Mathf.Sin(_animTimer * AnimIdleWiggleSpeed) * AnimIdleWiggleScale;
    float targetLean = _actor.MoveAxis.magnitude * leanScale * (_isCharging ? 2 : 1);
    _leanAmount = Mathfx.Damp(_leanAmount, targetLean, 0.25f, Time.deltaTime) + _leanSpring.Value;
    _playerVisualRoot.localRotation = Quaternion.Euler(_leanAmount, targetRot, 0);

    if (_currentPossessable)
    {
      Transform possessableTransform = _currentPossessable.transform;
      possessableTransform.localPosition = Mathfx.Damp(possessableTransform.localPosition, Vector3.zero, 0.25f, Time.deltaTime);
      possessableTransform.localRotation = Mathfx.Damp(possessableTransform.localRotation, Quaternion.identity, 0.25f, Time.deltaTime);

      _standHeightOffset = Mathfx.Damp(_standHeightOffset, _currentPossessable.StandHeightOffset, 0.25f, Time.deltaTime);
    }
    else
    {
      _standHeightOffset = Mathfx.Damp(_standHeightOffset, 0, 0.25f, Time.deltaTime);
    }

    if (!_isCharging)
    {
      if (_playerInput.GetButtonDown(RewiredConsts.Action.Interact))
      {
        if (_interaction.ClosestInteractable)
        {
          _interaction.TriggerInteraction();
        }
        else if (_currentPossessable)
        {
          StopPossessing();
        }
      }

      if (_attackCooldownTimer <= 0 && _playerInput.GetButtonDown(RewiredConsts.Action.Attack))
      {
        DoSpookAttack();
      }
    }
  }

  public void EjectPossession()
  {
    if (_currentPossessable)
    {
      var possessableGO = _currentPossessable.gameObject;

      if (_currentPossessable.DestroyFX)
      {
        _spookAttackFx = Instantiate(_currentPossessable.DestroyFX, possessableGO.transform);
      }

      StopPossessing();

      // Gracefully shirink the object out of existence
      DespawnManager.Instance.AddObject(possessableGO, 0.0f, 0.25f);
    }
  }

  private void ResetLegs()
  {
    _footIK.ClearFeet();

    foreach (var foot in _feet)
      Destroy(foot.gameObject);

    foreach (var leg in _legs)
      Destroy(leg.gameObject);

    _feet.Clear();
    _legs.Clear();
  }

  private void OnInteraction(Interactable interactable)
  {
    PossessableObject possessable = interactable.GetComponentInParent<PossessableObject>();
    if (possessable)
    {
      PossessObject(possessable);
    }
  }
}