using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class WorldUIManager : Singleton<WorldUIManager>
{
  public RectTransform RootCanvas => _rootCanvas;

  [SerializeField] private RectTransform _itemTemplate = null;

  [SerializeField] private RectTransform _rootCanvas = null;

  private List<UIObject> _uiObjects = new List<UIObject>();

  private int _currentId;

  [System.Serializable]
  private struct UIObject
  {
    public RectTransform UI;
    public Transform WorldAnchor;
    public Vector3 WorldOffset;
    public bool IsShown;
  }

  public void Show()
  {
    _rootCanvas.gameObject.SetActive(true);
  }

  public void Hide()
  {
    _rootCanvas.gameObject.SetActive(false);
  }

  public RectTransform ShowItem(Transform attachedTransform, Vector3 worldOffset)
  {
    UIObject obj = new();
    obj.UI = Instantiate(_itemTemplate, _itemTemplate.parent);
    obj.WorldAnchor = attachedTransform;
    obj.WorldOffset = worldOffset;
    obj.IsShown = true;
    obj.UI.gameObject.SetActive(true);
    obj.UI.name = $"world-locked-ui-{_currentId}";
    _currentId += 1;

    _uiObjects.Add(obj);

    return obj.UI;
  }

  public void HideItem(Transform ui)
  {
    HideItem(ui as RectTransform);
  }

  public void HideItem(RectTransform ui)
  {
    for (int i = 0; i < _uiObjects.Count; ++i)
    {
      // Find the UI object and hide it
      UIObject uiObj = _uiObjects[i];
      if (ReferenceEquals(ui, uiObj.UI) && uiObj.IsShown)
      {
        uiObj.IsShown = false;
        _uiObjects[i] = uiObj;

        UIHydrate hydrate = ui.GetComponent<UIHydrate>();
        hydrate.DehydrateIfNecessary(() => { RemoveUI(ui); });

        return;
      }
    }
  }

  private void RemoveUI(RectTransform ui)
  {
    if (ui != null)
    {
      // Find the UI object in our list and remove it
      for (int i = 0; i < _uiObjects.Count; ++i)
      {
        if (ReferenceEquals(_uiObjects[i].UI, ui))
        {
          UIObject obj = _uiObjects[i];
          _uiObjects.RemoveAt(i);
          break;
        }
      }

      Destroy(ui.gameObject);
    }
  }

  private void Awake()
  {
    _itemTemplate.gameObject.SetActive(false);
    Instance = this;
  }

  private void LateUpdate()
  {
    for (int i = 0; i < _uiObjects.Count; ++i)
    {
      UIObject uiObject = _uiObjects[i];
      if (uiObject.WorldAnchor != null)
      {
        // Position the ui at the screenspace position of the object 
        Vector3 worldPos = uiObject.WorldAnchor.position + uiObject.WorldOffset;
        Vector3 canvasPos = Mathfx.WorldToCanvasPosition(_rootCanvas, MainCamera.Instance.Camera, worldPos, allowOffscreen: true);
        uiObject.UI.anchoredPosition = canvasPos;

        // While the UI is shown, control its visiblity by distance
        if (uiObject.IsShown)
        {
          bool isVisible = canvasPos.z >= 0;
          uiObject.UI.gameObject.SetActive(isVisible);
        }
      }
      else
      {
        HideItem(uiObject.UI);
      }
    }
  }
}