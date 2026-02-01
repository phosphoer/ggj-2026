using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GamePlayUI : UIPageBase
{
  public HeartRateMonitorDisplay HeartRateUI => _heartRateMonitor;
  [SerializeField] private HeartRateMonitorDisplay _heartRateMonitor = null;

  protected override void Awake()
  {
    base.Awake();
    Shown += OnShown;
  }

  private void OnShown()
  {
    if (_heartRateMonitor != null)
      _heartRateMonitor.StartMonitoring();
  }

  private void Update()
  {
  }
}
