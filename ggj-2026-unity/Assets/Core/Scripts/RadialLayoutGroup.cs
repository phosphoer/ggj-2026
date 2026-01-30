using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class RadialLayoutGroup : LayoutGroup
{
  [Header("Radial Layout Settings")]
  [Tooltip("How large of an arc, in degrees, the items are arranged along")]
  public float ArcAngleSize = 360f;

  [Tooltip("Whether to use ArcAngleStep or calculate automatically")]
  public bool EnableArcAngleStep = false;

  [Tooltip("Explicit angle step if enabled")]
  public float ArcAngleStep = 45f;

  [Tooltip("Starting angle in degrees")] public float ArcAngleOffset = 0f;

  [Tooltip("Arrange children clockwise?")]
  public bool Clockwise = true;

  [Tooltip("Center children around the radius")]
  public bool CenterAngle = false;

  [Tooltip("Whether the last child should end at ArcAngleSize or one segment before")]
  public bool UseFullArc = true;

  public override void CalculateLayoutInputHorizontal()
  {
  }

  public override void CalculateLayoutInputVertical()
  {
  }

  public override void SetLayoutHorizontal()
  {
    ArrangeChildren();
  }

  public override void SetLayoutVertical()
  {
    ArrangeChildren();
  }

  public void ArrangeChildren()
  {
    if (transform.childCount == 0 || Mathf.Abs(transform.lossyScale.x) < 0.001 || Mathf.Abs(transform.lossyScale.y) < 0.001)
      return;

    RectTransform rectTransform = transform as RectTransform;
    Vector3 rectSize = Mathfx.GetRectTransformWorldSize(rectTransform);
    rectSize.x = rectSize.x - padding.left - padding.right;
    rectSize.y = rectSize.y - padding.top - padding.bottom;
    rectSize.x /= transform.lossyScale.x;
    rectSize.y /= transform.lossyScale.y;

    Vector3 pivotOffset = new Vector3(
      (rectTransform.pivot.x - 0.5f) * rectSize.x,
      (rectTransform.pivot.y - 0.5f) * rectSize.y,
      0);

    Vector3 paddingOffset = new Vector3(
      padding.left - (padding.left + padding.right) * rectTransform.pivot.x,
      padding.bottom - (padding.bottom + padding.top) * rectTransform.pivot.y,
      0f
    );

    int childCount = transform.childCount;
    int childCountArc = UseFullArc ? childCount - 1 : childCount;
    float clockwiseSign = Clockwise ? -1 : 1;
    float angleStep = ArcAngleSize / childCountArc * clockwiseSign;
    if (EnableArcAngleStep)
      angleStep = ArcAngleStep * clockwiseSign;

    float startAngle = 90 + ArcAngleOffset;
    if (CenterAngle)
      startAngle += angleStep * clockwiseSign * childCountArc * 0.5f;

    float currentAngle = startAngle;
    for (int i = 0; i < childCount; i++)
    {
      RectTransform child = transform.GetChild(i) as RectTransform;

      if (child == null || !child.gameObject.activeSelf)
        continue;

      Vector3 childPos = new Vector3(
        Mathf.Cos(currentAngle * Mathf.Deg2Rad) * rectSize.x * 0.5f,
        Mathf.Sin(currentAngle * Mathf.Deg2Rad) * rectSize.y * 0.5f,
        0);

      childPos -= pivotOffset;
      childPos += paddingOffset;

      child.localPosition = childPos;
      child.anchorMin = child.anchorMax = new Vector2(0.5f, 0.5f);
      child.pivot = new Vector2(0.5f, 0.5f);

      currentAngle += angleStep;
    }
  }

#if UNITY_EDITOR
  // Automatically update layout in the editor or when hierarchy changes
  protected override void OnValidate()
  {
    base.OnValidate();
    ArrangeChildren();
  }
#endif

  protected override void OnTransformChildrenChanged()
  {
    base.OnTransformChildrenChanged();
    ArrangeChildren();
  }

  protected override void OnCanvasHierarchyChanged()
  {
    base.OnCanvasHierarchyChanged();
    ArrangeChildren();
  }
}