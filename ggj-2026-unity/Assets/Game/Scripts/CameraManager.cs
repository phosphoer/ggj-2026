using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : Singleton<CameraManager>
{
  public enum eScreenLayout
  {
    Invalid,
    MenuCamera,
    MultiCamera,
    WinCamera,
    LoseCamera
  }

  public CameraControllerStack MenuCameraStack => _menuCameraStack;
  public SplitscreenLayout SplitscreenLayout => _splitscreenLayout;

  [SerializeField]
  private Camera _menuCamera = null;

  [SerializeField]
  private CameraControllerStack _menuCameraStack = null;

  [SerializeField]
  private CameraControllerDynamic _menuCameraController = null;

  [SerializeField]
  private CameraControllerDynamic[] _winCameraControllers = null;

  [SerializeField]
  private CameraControllerDynamic _loseCameraControllers = null;

  [SerializeField]
  private SplitscreenLayout _splitscreenLayout = null;

  private eScreenLayout _cameraLayout = eScreenLayout.Invalid;
  public eScreenLayout CameraLayout => _cameraLayout;

  private void Awake()
  {
    Instance = this;
  }

  public void SetScreenLayout(eScreenLayout targetLayout)
  {
    if (targetLayout != _cameraLayout)
    {
      // Clear up the previous camera layout
      switch (_cameraLayout)
      {
      case eScreenLayout.MenuCamera:
        _menuCameraStack.PopController(_menuCameraController);
        _menuCamera.enabled = false;
        break;
      case eScreenLayout.MultiCamera:
        _splitscreenLayout.SetEnabled(false);
        break;
      case eScreenLayout.WinCamera:
      {
        // Show the corresponding winners camera
        int winnerIndex = GameStateManager.Instance.WinningPlayerID;
        if (_winCameraControllers.IsIndexValid(winnerIndex))
        {
          _menuCameraStack.PopController(_winCameraControllers[winnerIndex]);
        }
        _menuCamera.enabled = false;
      }
      break;
      case eScreenLayout.LoseCamera:
        _menuCameraStack.PopController(_menuCameraController);
        _menuCamera.enabled = false;
        break;
      }

      // Then enable the one we want
      switch (targetLayout)
      {
      case eScreenLayout.MenuCamera:
        _menuCameraStack.PushController(_menuCameraController);
        _menuCamera.enabled = true;
        break;
      case eScreenLayout.MultiCamera:
        _splitscreenLayout.SetEnabled(true);
        break;
      case eScreenLayout.WinCamera:
      {
        // Show the corresponding winners camera
        int winnerIndex = GameStateManager.Instance.WinningPlayerID;
        if (_winCameraControllers.IsIndexValid(winnerIndex))
        {
          _menuCameraStack.PushController(_winCameraControllers[winnerIndex]);
        }
        _menuCamera.enabled = true;
      }
      break;
      case eScreenLayout.LoseCamera:
        _menuCameraStack.PushController(_loseCameraControllers);
        _menuCamera.enabled = true;
        break;
      }

      _cameraLayout = targetLayout;
    }
  }

  public void ShakeActiveCameras(float magnitide, float duration)
  {
    if (_cameraLayout == eScreenLayout.MultiCamera)
    {
      foreach (PlayerCharacterController player in PlayerManager.Instance.Players)
      {
        player.CameraStack.CameraShake(magnitide, duration);
      }
    }
    else
    {
      _menuCameraStack.CameraShake(magnitide, duration);
    }
  }
}