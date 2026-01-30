using UnityEngine;
using System.Collections.Generic;

public class TimeManager : Singleton<TimeManager>
{
  public static event System.Action TimeScaleChanged;

  public float TimeScale => _timeScale;

  private float _timeScale = 1.0f;
  private List<TimeScaleState> _timeScaleStack = new List<TimeScaleState>();

  private static readonly int kShaderUnscaledTime = Shader.PropertyToID("_GlobalUnscaledTime");

  public class TimeScaleState
  {
    public TimeScaleState(float scale, string key)
    {
      Scale = scale;
      Key = key;
    }

    public float Scale;
    public readonly string Key;
  }


  // Reset static state for editor without domain reload
#if UNITY_EDITOR
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
  private static void EditorInit()
  {
    TimeScaleChanged = null;
  }
#endif

  public void TogglePause(string key)
  {
    ToggleTimeScale(0, key);
  }

  public void ToggleTimeScale(float timeScale, string key)
  {
    if (!PopTimeScale(key))
    {
      PushTimeScale(timeScale, key);
    }
  }

  public void PushSlowMotionPause(string key)
  {
    PushTimeScale(0, key);
  }

  public TimeScaleState PushTimeScale(float timeScale, string key)
  {
    TimeScaleState state = new TimeScaleState(timeScale, key);
    _timeScaleStack.Add(state);
    UpdateTimeScale();
    TimeScaleChanged?.Invoke();
    return state;
  }

  public bool PopTimeScale(string key)
  {
    for (int i = 0; i < _timeScaleStack.Count; ++i)
    {
      if (_timeScaleStack[i].Key == key)
      {
        _timeScaleStack.RemoveAt(i);
        UpdateTimeScale();
        TimeScaleChanged?.Invoke();
        return true;
      }
    }

    return false;
  }

  public void ClearTimeScaleStack()
  {
    _timeScaleStack.Clear();
    UpdateTimeScale();
    TimeScaleChanged?.Invoke();
  }

  private void Awake()
  {
    Instance = this;
    UpdateTimeScale();
  }

  private void Update()
  {
    UpdateTimeScale();

    Time.timeScale = Mathfx.Damp(Time.timeScale, _timeScale, 0.5f, Time.unscaledDeltaTime * 10.0f);
    Shader.SetGlobalFloat(kShaderUnscaledTime, Time.unscaledTime);
  }

  private void UpdateTimeScale()
  {
    if (_timeScaleStack.Count == 0)
    {
      _timeScale = 1.0f;
    }
    else
    {
      float minTimeScale = Mathf.Infinity;
      for (int i = 0; i < _timeScaleStack.Count; ++i)
      {
        if (_timeScaleStack[i].Scale < minTimeScale)
          minTimeScale = _timeScaleStack[i].Scale;
      }

      _timeScale = minTimeScale;
    }
  }
}