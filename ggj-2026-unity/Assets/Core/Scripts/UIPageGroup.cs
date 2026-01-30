using UnityEngine;
using System.Collections.Generic;

public class UIPageGroup : MonoBehaviour
{
  public event System.Action<UIPageBase> PageShown;
  public event System.Action<UIPageBase> PageHidden;

  public IReadOnlyList<UIPageBase> Pages => _pages;
  public RectTransform PageRoot => _pageRoot;

  [SerializeField]
  private List<UIPageBase> _pagePrefabs = new();

  [SerializeField]
  private RectTransform _pageRoot = null;

  private List<UIPageBase> _pages = new();
  private Dictionary<UIPageBase, UIPageBase> _pagesByPrefab = new();
  private Dictionary<UIPageBase, UIPageBase> _prefabsByPage = new();

  public T GetPage<T>() where T : UIPageBase
  {
    for (int i = 0; i < _pagePrefabs.Count; ++i)
    {
      UIPageBase pagePrefab = _pagePrefabs[i];
      if (pagePrefab is T)
      {
        return GetPage(pagePrefab) as T;
      }
    }

    Debug.LogError($"Failed to get UIPage of type {typeof(T).Name}, is it registered with PlayerUI?");
    return null;
  }

  public UIPageBase GetPage(UIPageBase pagePrefab)
  {
    if (_pagesByPrefab.TryGetValue(pagePrefab, out UIPageBase existingPage))
    {
      return existingPage;
    }

    UIPageBase page = Instantiate(pagePrefab, _pageRoot);
    page.transform.SetIdentityTransformLocal();
    page.ZOrder = _pagePrefabs.IndexOf(pagePrefab);
    page.ParentGroup = this;
    page.gameObject.SetActive(false);
    page.Shown += () => OnPageShown(page);
    page.Hidden += () => OnPageHidden(page);

    _pages.Add(page);
    _pagesByPrefab[pagePrefab] = page;
    _prefabsByPage[page] = pagePrefab;
    SortPagesByZOrder();

    return page;
  }

  public void AddPagePrefab(UIPageBase pagePrefab)
  {
    _pagePrefabs.Add(pagePrefab);
  }

  public UIPageBase GetPagePrefab(UIPageBase page)
  {
    if (_prefabsByPage.TryGetValue(page, out UIPageBase pagePrefab))
      return pagePrefab;

    return null;
  }

  public void HideAllPages()
  {
    foreach (var page in _pages)
      page.Hide();
  }

  private void Start()
  {
    SortPagesByZOrder();
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

  private void OnPageShown(UIPageBase page)
  {
    PageShown?.Invoke(page);
  }

  private void OnPageHidden(UIPageBase page)
  {
    PageHidden?.Invoke(page);
  }
}