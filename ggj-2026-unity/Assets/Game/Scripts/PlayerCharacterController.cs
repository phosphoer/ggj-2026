using UnityEngine;
using System.Collections;
using Rewired;

public class PlayerCharacterController : MonoBehaviour
{
  public event System.Action<PlayerCharacterController> PlayerReady;

  public CameraControllerStack CameraStack => _cameraStack;
  public CameraControllerPlayer CameraController => _cameraController;
  public int PlayerID => _playerID;
  public Transform PlayerHudUIAnchor => _playerHudUIAnchor;
  public InteractableUI PlayerHudPrefab => _playerHudPrefab;

  public int RewiredPlayerId = 0;
  public GameCharacterController Character = null;
  public CameraControllerStack CameraStackPrefab = null;
  public CameraControllerPlayer CameraControllerPrefab = null;
  public float PlayerHudHeight = 0;

  [SerializeField]
  private Transform _playerHudUIAnchor;

  [SerializeField]
  private InteractableUI _playerHudPrefab = null;

  [SerializeField]
  private LayerMask _playerWorldLayerMask = default;

  [SerializeField]
  private LayerMask _playerUILayerMask = default;

  private CameraControllerStack _cameraStack;
  private CameraControllerPlayer _cameraController;
  private int _playerID = -1;
  private InteractableUI _hudMessageUI;
  private bool _isReady = true;
  private bool _isAllowedToMove = true;
  private Vector3 _startPos;

  private void Awake()
  {
    _cameraStack = Instantiate(CameraStackPrefab);
    _cameraController = Instantiate(CameraControllerPrefab, Character.CameraRoot);
    _cameraController.transform.SetIdentityTransformLocal();

    CameraStack.PushController(_cameraController);
    CameraStack.SnapTransformToTarget();

    //_playerUI = Instantiate(PlayerUIPrefab);
    //_playerUI.Canvas.worldCamera = _cameraStack.UICamera;

    _cameraStack.Camera.cullingMask = _playerWorldLayerMask;
    _cameraStack.UICamera.cullingMask = _playerUILayerMask;

    Character.Slappable.Slapped += OnSlapped;
    Character.OutOfBounds += OnOutOfBounds;

    SplitscreenLayout.LayoutUpdated += OnLayoutUpdated;
    OnLayoutUpdated();
  }

  private void Start()
  {
    _startPos = transform.position;
  }

  private void OnDestroy()
  {
    SplitscreenLayout.LayoutUpdated -= OnLayoutUpdated;
    Character.Slappable.Slapped -= OnSlapped;
  }

  private void OnSlapped(GameCharacterController fromCharacter)
  {
    _cameraStack.CameraShake(1, 0.5f);
  }

  private void OnLayoutUpdated()
  {
    _cameraStack.UICamera.rect = _cameraStack.Camera.rect;
  }

  private void OnOutOfBounds()
  {
    transform.position = _startPos;
  }

  private IEnumerator RespawnAsync()
  {
    yield return Tween.WaitForTime(1);
  }

  public bool GetIsReady()
  {
    return _isReady;
  }

  public void ClearReadyFlag()
  {
    _isReady = false;
  }

  public void SetReadyFlag()
  {
    if (!_isReady)
    {
      _isReady = true;
      PlayerReady?.Invoke(this);
    }
  }

  public void SetIsAllowedToMove(bool flag)
  {
    _isAllowedToMove = flag;
    Cursor.lockState = flag ? CursorLockMode.Locked : CursorLockMode.None;
  }

  public void AssignPlayerId(int playerID)
  {
    _playerID = playerID;
    //_playerUI.BorderUI.BindPlayerID(playerID);
  }

  // public void AssignPirate(PirateController pirate)
  // {
  //   _assignedPirate = pirate;
  //   _playerUI.PirateUI.BindPirate(pirate);
  // }

  private void Update()
  {
    var rewiredPlayer = ReInput.players.GetPlayer(RewiredPlayerId);
    if (rewiredPlayer != null)
    {
      if (!_isReady)
      {
        if (rewiredPlayer.GetButtonDown(RewiredConsts.Action.Interact))
        {
          SetReadyFlag();
        }
      }
      else if (_isAllowedToMove)
      {
        //Character.MoveAxis = rewiredPlayer.GetAxis(RewiredConsts.Action.MoveForward);
        //Character.StrafeAxis = rewiredPlayer.GetAxis(RewiredConsts.Action.Strafe);
        //Character.LookHorizontalAxis = rewiredPlayer.GetAxis(RewiredConsts.Action.LookHorizontal);
        //Character.LookVerticalAxis = rewiredPlayer.GetAxis(RewiredConsts.Action.LookVertical);

        if (rewiredPlayer.GetButtonDown(RewiredConsts.Action.Interact))
        {
          Character.Interact();
        }

        //if (rewiredPlayer.GetButtonDown(RewiredConsts.Action.Attack))
        //{
        //  Character.Attack();
        //}
      }
    }
  }

  public void ShowHudMessage(string message)
  {
    ClearHudMessage();

    //var uiRoot = PlayerUI.OnScreenUI.ShowItem(PlayerHudUIAnchor, Vector3.up * PlayerHudHeight);
    //_hudMessageUI = Instantiate(_playerHudPrefab, uiRoot);
    //_hudMessageUI.transform.SetIdentityTransformLocal();
    //_hudMessageUI.InteractionText = message;
  }

  public void ClearHudMessage()
  {

  }
}