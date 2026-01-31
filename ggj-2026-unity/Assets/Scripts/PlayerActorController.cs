using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class PlayerActorController : MonoBehaviour
{
  public float AnimIdleBobScale = 0.05f;
  public float AnimIdleBobSpeed = 3f;
  public float AnimIdleWiggleScale = 5;
  public float AnimIdleWiggleSpeed = 1;

  public Rewired.Player PlayerInput => _playerInput;
  public int PlayerIndex => _playerIndex;
  public string PlayerColorName => _playerColorName;

  [SerializeField] private ObjectActorController _actor = null;
  [SerializeField] private Transform _playerVisualRoot = null;
  [SerializeField] private GameObject _playerVisual = null;
  [SerializeField] private FootIK _footIK = null;
  [SerializeField] private InteractionController _interaction = null;
  [SerializeField] private LegNoodleController _legPrefab = null;
  [SerializeField] private GameObject _footPrefab = null;

  private Rewired.Player _playerInput;
  private int _playerIndex = -1;
  private string _playerColorName = "";
  private PossessableObject _currentPossessable;
  private List<LegNoodleController> _legs = new();
  private List<GameObject> _feet = new();
  private float _animTimer;
  private float _standHeightOffset;
  private bool _possessableWasKinematic;
  private Transform _possessableOriginalParent;

  public void SetPlayerIndex(int playerIndex)
  {
    _playerIndex = playerIndex;
    _playerInput = Rewired.ReInput.players.GetPlayer(playerIndex);
  }

  public void SetPlayerColor(string colorName)
  {
    _playerColorName = colorName;
  }

  public void PossessObject(PossessableObject possessable)
  {
    Debug.Log($"Possessing object {possessable.name}");

    _interaction.enabled = false;

    ResetLegs();

    // Assign new possessable
    _possessableOriginalParent = possessable.transform.parent;
    _currentPossessable = possessable;
    _currentPossessable.transform.parent = _playerVisualRoot;

    _actor.MoveSpeed = _currentPossessable.MoveSpeed;
    _actor.RotateSpeed = _currentPossessable.RotateSpeed;

    AnimIdleBobScale = _currentPossessable.AnimIdleBobScale;
    AnimIdleBobSpeed = _currentPossessable.AnimIdleBobSpeed;
    AnimIdleWiggleScale = _currentPossessable.AnimIdleWiggleScale;
    AnimIdleWiggleSpeed = _currentPossessable.AnimIdleWiggleSpeed;

    Collider[] propColliders = _currentPossessable.GetComponentsInChildren<Collider>();
    foreach (var c in propColliders)
      c.enabled = false;

    Rigidbody rb = _currentPossessable.GetComponent<Rigidbody>();
    if (rb)
    {
      _possessableWasKinematic = rb.isKinematic;
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

      Collider[] propColliders = _currentPossessable.GetComponentsInChildren<Collider>();
      foreach (var c in propColliders)
        c.enabled = true;

      Rigidbody rb = _currentPossessable.GetComponent<Rigidbody>();
      if (rb)
      {
        rb.isKinematic = _possessableWasKinematic;
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

  private void OnEnable()
  {
    _interaction.InteractionTriggered += OnInteraction;
  }

  private void OnDisable()
  {
    _interaction.InteractionTriggered -= OnInteraction;
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

    _animTimer += Time.deltaTime;
    Vector3 posOffset = Vector3.up * _standHeightOffset;
    _playerVisualRoot.localPosition = Vector3.up * Mathf.Sin(_animTimer * AnimIdleBobSpeed) * AnimIdleBobScale + posOffset;

    float targetRot = Mathf.Sin(_animTimer * AnimIdleWiggleSpeed) * AnimIdleWiggleScale;
    _playerVisualRoot.localRotation = Quaternion.Euler(0, targetRot, 0);

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

    if (_playerInput.GetButtonDown(RewiredConsts.Action.Attack))
    {
      if (_currentPossessable.SpookAttackFX && _currentPossessable.SpookAttackRoot)
      {
        ParticleSystem spookFx = Instantiate(_currentPossessable.SpookAttackFX, _currentPossessable.SpookAttackRoot);
        spookFx.transform.SetIdentityTransformLocal();
      }
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