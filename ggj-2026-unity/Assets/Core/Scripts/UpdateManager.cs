using UnityEngine;
using System.Collections.Generic;

public class UpdateManager : Singleton<UpdateManager>
{
  public static event System.Action OnUpdate;

  private struct DelayCallInfo
  {
    public System.Action Action;
    public float Timer;
  }

  private static List<DelayCallInfo> _delayCalls = new();


  // Reset static state for editor without domain reload
#if UNITY_EDITOR
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
  private static void EditorInit()
  {
    OnUpdate = null;
  }
#endif

  public static void DelayCall(float delaySeconds, System.Action action)
  {
    _delayCalls.Add(new DelayCallInfo() { Action = action, Timer = delaySeconds });
  }

  private void Awake()
  {
    Instance = this;
  }

  private void Update()
  {
    OnUpdate?.Invoke();

    for (int i = 0; i < _delayCalls.Count; ++i)
    {
      DelayCallInfo callInfo = _delayCalls[i];
      callInfo.Timer -= Time.deltaTime;
      _delayCalls[i] = callInfo;
      if (callInfo.Timer <= 0)
      {
        callInfo.Action?.Invoke();
        _delayCalls.RemoveAt(i);
        --i;
      }
    }
  }
}