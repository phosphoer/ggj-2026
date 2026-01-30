using UnityEngine;

public class ScrollRectTransform : MonoBehaviour
{
  public Vector2 ScrollPosition
  {
    get => _content.anchoredPosition;
    set => SetScrollPosition(value);
  }

  public bool EnableHorizontal = true;
  public bool EnableVertical = true;

  [SerializeField]
  private RectTransform _content = null;

  // Calculate the absolute scroll position that will bring targetTransform
  // directly to the center of the scroll view
  public Vector2 GetCenterScrollPos(RectTransform targetTransform)
  {
    RectTransform viewport = transform as RectTransform;
    Bounds elementBounds = targetTransform.TransformBoundsTo(viewport);
    float offsetX = EnableHorizontal ? viewport.rect.center.x - elementBounds.center.x : 0;
    float offsetY = EnableVertical ? viewport.rect.center.y - elementBounds.center.y : 0;

    return new Vector2(offsetX, offsetY) + _content.anchoredPosition;
  }

  // Calculate the absolute scroll position such that the target transform 
  // is brought into view with optional padding with the minimal movement necessary
  public Vector2 GetClampedScrollPos(RectTransform targetTransform, float padding = 0)
  {
    RectTransform viewport = transform as RectTransform;
    Rect viewportRect = viewport.rect;

    Bounds elementBounds = targetTransform.TransformBoundsTo(viewport);
    elementBounds.Expand(padding);

    float offsetX = Mathf.Max(0, viewportRect.min.x - elementBounds.min.x)
                  - Mathf.Max(0, elementBounds.max.x - viewportRect.max.x);

    float offsetY = Mathf.Max(0, viewportRect.min.y - elementBounds.min.y)
                  - Mathf.Max(0, elementBounds.max.y - viewportRect.max.y);

    return new Vector2(EnableHorizontal ? offsetX : 0, EnableVertical ? offsetY : 0) + _content.anchoredPosition;
  }

  public Vector2 ClampScrollPosToRect(Vector2 scrollPos)
  {
    RectTransform viewport = transform as RectTransform;
    Rect viewportRect = viewport.rect;
    Rect contentRect = _content.rect;
    scrollPos.x = Mathf.Clamp(scrollPos.x, 0, Mathf.Max(0, contentRect.size.x - viewportRect.max.x));
    scrollPos.y = Mathf.Clamp(scrollPos.y, 0, Mathf.Max(0, contentRect.size.y - viewportRect.max.y));
    return scrollPos;
  }

  private void SetScrollPosition(Vector2 scrollPos)
  {
    _content.anchoredPosition = scrollPos;
  }
}
