using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaitingForPlayersUI : UIPageBase
{
  protected override void Awake()
  {
    base.Awake();
    Shown += OnShown;    
  }

  private void OnShown()
  {
  }
}
