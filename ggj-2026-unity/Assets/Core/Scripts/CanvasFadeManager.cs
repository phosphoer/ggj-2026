using UnityEngine;
using System.Collections.Generic;

public class CanvasFadeManager : Singleton<CanvasFadeManager>
{
  private List<FadeInfo> _activeFades = new();

  private struct FadeInfo
  {
    public CanvasGroup CanvasGroup;
    public float Duration;
    public float Timer;
    public float TargetAlpha;
    public float StartAlpha;
    public System.Action OnComplete;
  }

  public void Add(CanvasGroup canvas, float toAlpha, float fadeTime, System.Action onComplete = null)
  {
    // Remove any active fades using this canvas
    _activeFades.RemoveAll(fade => fade.CanvasGroup == canvas);

    // Start fading this canvas
    FadeInfo fadeInfo = default;
    fadeInfo.CanvasGroup = canvas;
    fadeInfo.Duration = fadeTime;
    fadeInfo.TargetAlpha = toAlpha;
    fadeInfo.StartAlpha = canvas.alpha;
    fadeInfo.OnComplete = onComplete;
    _activeFades.Add(fadeInfo);
  }

  private void Awake()
  {
    Instance = this;
  }

  private void Update()
  {
    float dt = Time.unscaledDeltaTime;

    // Fade canvas groups
    for (int i = 0; i < _activeFades.Count; ++i)
    {
      var fadeInfo = _activeFades[i];
      fadeInfo.Timer += dt;
      _activeFades[i] = fadeInfo;

      float fadeT = Mathf.Clamp01(fadeInfo.Timer / fadeInfo.Duration);

      if (fadeInfo.CanvasGroup != null)
        fadeInfo.CanvasGroup.alpha = Mathf.Lerp(fadeInfo.StartAlpha, fadeInfo.TargetAlpha, fadeT);

      if (fadeT >= 1)
      {
        fadeInfo.OnComplete?.Invoke();
        _activeFades.RemoveAt(i);
        --i;
      }
    }
  }
}