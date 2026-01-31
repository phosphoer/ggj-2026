using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GamePlayUI : UIPageBase
{
  protected override void Awake()
  {
    base.Awake();
    Shown += OnShown;
  }

  private void OnShown()
  {
  }

  private void Update()
  {
  }
}
