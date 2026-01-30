using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GamePlayUI : UIPageBase
{
  public float LowWarningFraction = 0.1f;
  public SoundBank LowTimerWarning;
  public Image TimerDial;
  public Transform TimerRoot;
  public Transform TimerRootCenter;
  public Transform TimerRootCorner;

  private float _previousRemainingFraction;

  protected override void Awake()
  {
    base.Awake();
    Shown += OnShown;
    PlayerManager.PlayerJoined += OnPlayerJoined;
  }

  private void OnShown()
  {
    TimerDial.fillAmount = 1.0f;
    _previousRemainingFraction = 1.0f;
  }

  private void OnPlayerJoined(PlayerCharacterController player)
  {
    if (PlayerManager.Instance.Players.Count == 1)
    {
      TimerRoot.SetParent(TimerRootCorner, worldPositionStays: false);
      TimerRoot.SetIdentityTransformLocal();
    }
    else
    {
      TimerRoot.SetParent(TimerRootCenter, worldPositionStays: false);
      TimerRoot.SetIdentityTransformLocal();
    }
  }

  private void Update()
  {
    float gameTimer = GameStateManager.Instance.TimeInState;
    float gameDuration = GameStateManager.Instance.GameplayDuration;
    float newRemainingFraction = Mathf.Clamp01((gameDuration - gameTimer) / gameDuration);

    TimerDial.fillAmount = newRemainingFraction;

    if (_previousRemainingFraction >= LowWarningFraction && newRemainingFraction < LowWarningFraction)
    {
      if (LowTimerWarning != null)
      {
        AudioManager.Instance.PlaySound(gameObject, LowTimerWarning);
      }
    }
  }
}
