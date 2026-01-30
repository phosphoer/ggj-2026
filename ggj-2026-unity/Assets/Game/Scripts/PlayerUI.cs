using UnityEngine;
using System.Collections.Generic;

public class GameUI : Singleton<GameUI>
{
  public Camera Camera => _uiCamera;
  public RectTransform PageRoot => _pageRoot;

  [SerializeField]
  private UIPageBase[] _pagePrefabs = null;

  [SerializeField]
  private RectTransform _pageRoot = null;

  [SerializeField]
  private Camera _uiCamera = null;

  private List<UIPageBase> _pages = new();

  public T GetPage<T>() where T : UIPageBase
  {
    for (int i = 0; i < _pages.Count; ++i)
    {
      if (_pages[i] is T)
        return _pages[i] as T;
    }

    for (int i = 0; i < _pagePrefabs.Length; ++i)
    {
      UIPageBase pagePrefab = _pagePrefabs[i];
      if (pagePrefab is T)
      {
        UIPageBase page = Instantiate(pagePrefab, _pageRoot);
        page.transform.SetIdentityTransformLocal();
        page.ZOrder = i;
        _pages.Add(page);
        SortPagesByZOrder();

        return page as T;
      }
    }

    Debug.LogError($"Failed to get UIPage of type {typeof(T).Name}, is it registered with PlayerUI?");
    return null;
  }

  private void Awake()
  {
    Instance = this;
  }

  private void SortPagesByZOrder()
  {
    _pages.Sort((a, b) =>
    {
      return a.ZOrder - b.ZOrder;
    });

    for (int i = 0; i < _pages.Count; ++i)
    {
      _pages[i].transform.SetSiblingIndex(i);
    }
  }
}