using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WinGameUI : UIPageBase
{
  public TMPro.TMP_Text WinLabel;

  protected override void Awake()
  {
    base.Awake();
    Shown += OnShown;    
  }

  private void OnShown()
  {
    WinLabel.text = string.Format("Player {0} killed the farmer!", GameStateManager.Instance.WinningPlayerID + 1);
  }
}
