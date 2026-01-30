using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class UIPageBase : MonoBehaviour
{
  public event System.Action Shown;
  public event System.Action Hidden;

  public bool IsVisible => _isVisible;

  public bool IsAnyPartAnimating
  {
    get
    {
      bool animating = false;
      foreach (UIHydrate anim in _hydrateOnShow)
        animating |= anim.IsAnimating;

      return animating;
    }
  }

  public int ZOrder { get; set; }
  public UIPageGroup ParentGroup { get; set; }

  [FormerlySerializedAs("IsModal")] public bool ShowCursor = false;
  public float FadeDuration = 0.5f;

  [SerializeField] private UIHydrate[] _hydrateOnShow = null;
  [SerializeField] private CanvasGroup[] _fadeOnShow = null;

  private bool _isVisible = false;
  private int _visibleStack = 0;
  private int _dehydrateRefCount = 0;

  // Not using default parameter here just make [ContextMenu] work 
  [ContextMenu("Show")]
  public void Show()
  {
    Show(true);
  }

  public void Show(bool playAnim)
  {
    if (!_isVisible)
    {
      _isVisible = true;
      gameObject.SetActive(true);

      // Debug.Log($"{name} showing");

      if (playAnim)
      {
        foreach (UIHydrate hydrate in _hydrateOnShow)
        {
          hydrate.Hydrate();
        }

        foreach (CanvasGroup canvasGroup in _fadeOnShow)
        {
          canvasGroup.alpha = 0;
          CanvasFadeManager.Instance.Add(canvasGroup, 1, FadeDuration);
        }
      }

      if (ShowCursor)
      {
        CanvasCursor.PushVisible();
      }

      // It's nice for ui components to be working with fully hydrated windows 
      // in case they want to do size calculations
      foreach (UIHydrate hydrate in _hydrateOnShow)
        hydrate.SetFullScale();

      Shown?.Invoke();

      foreach (UIHydrate hydrate in _hydrateOnShow)
        hydrate.SetZeroScale();
    }
  }

  [ContextMenu("Hide")]
  public void Hide()
  {
    Hide(true);
  }

  public void Hide(bool playAnim)
  {
    if (_isVisible)
    {
      _isVisible = false;
      // Debug.Log($"{name} hiding");

      _dehydrateRefCount = 0;
      if (playAnim)
      {
        foreach (UIHydrate hydrate in _hydrateOnShow)
        {
          if (hydrate.IsHydrated && hydrate.gameObject.activeSelf)
          {
            hydrate.Dehydrate(OnDehydrateComplete);
            _dehydrateRefCount += 1;
          }
        }

        _dehydrateRefCount += _fadeOnShow.Length;
        foreach (CanvasGroup canvasGroup in _fadeOnShow)
          CanvasFadeManager.Instance.Add(canvasGroup, 0, FadeDuration, OnDehydrateComplete);
      }

      if (!playAnim || _hydrateOnShow.Length == 0)
      {
        OnDehydrateComplete();
      }

      if (ShowCursor)
      {
        CanvasCursor.PopVisible();
      }

      Hidden?.Invoke();
    }
  }

  public void PushVisible()
  {
    _visibleStack += 1;
    if (_visibleStack > 0)
      Show();
  }

  public void PopVisible()
  {
    _visibleStack -= 1;
    if (_visibleStack <= 0)
      Hide();
  }

  public void Toggle()
  {
    if (_isVisible)
    {
      Hide();
    }
    else
    {
      Show();
    }
  }

  protected virtual void Awake()
  {
  }

  private void OnDehydrateComplete()
  {
    --_dehydrateRefCount;
    if (_dehydrateRefCount <= 0)
    {
      gameObject.SetActive(false);
    }
  }

#if UNITY_EDITOR
  [ContextMenu("Gather Hydrate-ables")]
  private void GatherHydrates()
  {
    UnityEditor.Undo.RecordObject(this, "Gather Hydrate-ables");
    _hydrateOnShow = GetComponentsInChildren<UIHydrate>(includeInactive: true);
  }
#endif
}