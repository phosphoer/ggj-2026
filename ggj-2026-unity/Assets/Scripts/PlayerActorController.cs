using UnityEngine;
using System.Collections.Generic;

public class PlayerActorController : MonoBehaviour
{
  public float AnimIdleBobScale = 0.05f;
  public float AnimIdleBobSpeed = 3f;

  [SerializeField] private ObjectActorController _actor = null;
  [SerializeField] private Transform _playerVisualRoot = null;
  [SerializeField] private GameObject _playerVisual = null;
  [SerializeField] private FootIK _footIK = null;
  [SerializeField] private LegNoodleController _legPrefab = null;
  [SerializeField] private GameObject _footPrefab = null;

  private PossessableObject _currentPossessable;
  private List<LegNoodleController> _legs = new();
  private float _animTimer;

  public void PossessObject(PossessableObject possessable)
  {
    Debug.Log($"Possessing object {possessable.name}");

    // Clear existing possessable
    _footIK.ClearFeet();

    foreach (var leg in _legs)
      Destroy(leg.gameObject);

    _legs.Clear();

    // Assign new possessable
    _currentPossessable = possessable;
    _currentPossessable.transform.parent = _playerVisualRoot;

    // Set up foot ik info
    _footIK.MaxSteppingFeet = _currentPossessable.FootStepCount;
    _footIK.FootStepDuration = new RangedFloat(_currentPossessable.FootStepDuration, _currentPossessable.FootStepDuration * 0.2f);
    _footIK.FootStepThreshold = new RangedFloat(_currentPossessable.FootStepThreshold * 0.5f, _currentPossessable.FootStepThreshold);

    // Set up feet
    foreach (var legSocket in _currentPossessable.LegSockets)
    {
      GameObject footObj = Instantiate(_footPrefab, transform);
      footObj.transform.localPosition = _currentPossessable.transform.InverseTransformPoint(legSocket.position).WithY(0);

      FootIK.FootInfo footInfo = default;
      footInfo.Root = footObj.transform;
      footInfo.Name = $"foot-{_footIK.Feet.Count}";
      _footIK.AddFoot(footInfo);

      LegNoodleController leg = Instantiate(_legPrefab, _currentPossessable.transform);
      leg.transform.position = legSocket.position;
      leg.FootTarget = footInfo.Root;
      leg.InitializeLeg();
    }

    // Hide player visual
    _playerVisual.SetActive(false);
  }

  private void Update()
  {
    var rewiredPlayer = Rewired.ReInput.players.GetPlayer(0);
    float horizontalAxis = rewiredPlayer.GetAxis(RewiredConsts.Action.MoveHorizontal);
    float forwardAxis = rewiredPlayer.GetAxis(RewiredConsts.Action.MoveForward);

    Vector2 inputAxis = new Vector2(horizontalAxis, forwardAxis);
    Vector3 inputAxisCameraLocal = inputAxis.OnXZPlane();
    Vector3 inputAxisWorld = MainCamera.Instance.CachedTransform.TransformDirection(inputAxisCameraLocal);
    Vector2 inputAxis2D = inputAxisWorld.XZ();
    _actor.MoveAxis = Mathfx.Damp(_actor.MoveAxis, inputAxis2D, 0.25f, Time.deltaTime * 3);

    _animTimer += Time.deltaTime;
    _playerVisualRoot.localPosition = Vector3.up * Mathf.Sin(_animTimer * AnimIdleBobSpeed) * AnimIdleBobScale;

    float targetRot = (_footIK.LeftSideLift + _footIK.RightSideLift) * _footIK.CurrentStepSide;
    _playerVisualRoot.localRotation = Mathfx.Damp(_playerVisualRoot.localRotation, Quaternion.Euler(0, targetRot * 150, 0), 0.25f, Time.deltaTime * 1);

    if (_currentPossessable)
    {
      Transform possessableTransform = _currentPossessable.transform;
      possessableTransform.localPosition = Mathfx.Damp(possessableTransform.localPosition, Vector3.zero, 0.25f, Time.deltaTime);
      possessableTransform.localRotation = Mathfx.Damp(possessableTransform.localRotation, Quaternion.identity, 0.25f, Time.deltaTime);
    }
  }

  private void OnTriggerEnter(Collider collider)
  {
    if (_currentPossessable)
      return;

    PossessableObject possessable = collider.GetComponentInParent<PossessableObject>();
    if (possessable)
    {
      PossessObject(possessable);
    }
  }
}