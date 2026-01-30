using UnityEngine;
using UnityEngine.EventSystems;

public struct DragDropResult
{
  public bool Success;
  public int DropCount;
}

public interface IDragDropSource
{
  DragDropResult OnDropItem(DragDropItem dropItem, PointerEventData eventData);
  void OnDropEnd(DragDropItem dropItem, PointerEventData eventData, DragDropResult result);
}

public class DragDropItem : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
  public static bool IsAnyDragActive { get; private set; }
  public static DragDropItem ActiveDragItem { get; private set; }

  public static event System.Action<PointerEventData> AnyDragBegin;
  public static event System.Action<PointerEventData> AnyDragEnd;

  public event System.Action<PointerEventData> DragBegin;
  public event System.Action<PointerEventData> DragUpdate;
  public event System.Action<PointerEventData> DragEnd;

  public IDragDropSource Source => _dragDropSource;
  public bool IsFakeDrag => _isFakeDrag;

  public DragDropItemUI DragPrefab;
  public GameObject DragIcon;
  public string DragLabel;
  public int DragQuantity = 1;

  private IDragDropSource _dragDropSource;
  private bool _isDragActive;
  private bool _isFakeDrag;
  private DragDropItemUI _dragDropVisual;
  private PointerEventData _lastDragEventData;
  private System.Action<PointerEventData> _fakeDragEndCallback;

  // Reset static state for editor without domain reload
#if UNITY_EDITOR
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
  private static void EditorInit()
  {
    AnyDragBegin = null;
    AnyDragEnd = null;
  }
#endif

  public void StartFakeDrag(System.Action<PointerEventData> endCallback = null)
  {
    _isFakeDrag = true;
    _fakeDragEndCallback = endCallback;
    OnBeginDrag(new PointerEventData(EventSystem.current));
  }

  public void CancelDrag()
  {
    _lastDragEventData ??= new PointerEventData(EventSystem.current);
    _lastDragEventData.pointerCurrentRaycast = default;
    OnEndDrag(_lastDragEventData);
  }

  public void OnBeginDrag(PointerEventData eventData)
  {
    if (enabled)
    {
      // Get parent drag source which we'll inform about the drag later
      _dragDropSource = GetComponentInParent<IDragDropSource>();
      _isDragActive = true;
      IsAnyDragActive = true;
      ActiveDragItem = this;

      // Create a visual to attach to the cursor during the drag
      _dragDropVisual = Instantiate(DragPrefab, PlayerUI.Instance.PageRoot);
      _dragDropVisual.transform.position = transform.position.WithZ(50);
      _dragDropVisual.transform.SetAsLastSibling();
      _dragDropVisual.Item = this;

      DragBegin?.Invoke(eventData);
      AnyDragBegin?.Invoke(eventData);
    }
  }

  public void OnDrag(PointerEventData eventData)
  {
    if (_isDragActive)
    {
      _lastDragEventData = eventData;

      Vector2 rectSize = Mathfx.GetRectTransformWorldSize(_dragDropVisual.transform as RectTransform);
      Vector2 uiEventPos = eventData.position;
      Vector2 centerEventPos = Vector2.zero;
      RectTransform parentRect = _dragDropVisual.transform.parent as RectTransform;
      RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, uiEventPos, PlayerUI.Instance.Camera, out uiEventPos);
      RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, rectSize, PlayerUI.Instance.Camera, out rectSize);
      RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, centerEventPos, PlayerUI.Instance.Camera, out centerEventPos);

      rectSize -= centerEventPos;
      Vector3 offset = Vector3.left * rectSize.x * 0.5f + Vector3.up * rectSize.y * 0.5f;

      Vector3 dragPos = uiEventPos.OnXYPlane() + offset / parentRect.lossyScale.x;
      _dragDropVisual.transform.localPosition = dragPos.WithZ(-50);
      DragUpdate?.Invoke(eventData);
    }
  }

  public void OnEndDrag(PointerEventData eventData)
  {
    if (_isDragActive)
    {
      _isFakeDrag = false;
      _isDragActive = false;
      IsAnyDragActive = false;
      ActiveDragItem = null;

      // Hide the visual
      _dragDropVisual.UIHydrate.DestroyOnDehydrate = true;
      _dragDropVisual.UIHydrate.Dehydrate();
      _dragDropVisual = null;

      // Try to find a valid target to drop the item on
      GameObject dropObject = eventData.pointerCurrentRaycast.gameObject;
      DragDropResult result = default;
      if (dropObject != null)
      {
        IDragDropSource dropTarget = dropObject.GetComponentInParent<IDragDropSource>();
        if (dropTarget != null)
        {
          result = dropTarget.OnDropItem(this, eventData);
        }
      }

      // Tell the drop source whether we were succesfully dropped
      Source.OnDropEnd(this, eventData, result);
      DragEnd?.Invoke(eventData);
      AnyDragEnd?.Invoke(eventData);
      _fakeDragEndCallback?.Invoke(eventData);
      _fakeDragEndCallback = null;
    }
  }

  private void OnDisable()
  {
    if (_isDragActive)
    {
      OnEndDrag(_lastDragEventData);
    }
  }

  private void OnDestroy()
  {
    if (_dragDropVisual != null)
    {
      Destroy(_dragDropVisual.gameObject);
    }
  }
}