using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CanvasCursor : MonoBehaviour
{
  public static bool IsVisible => !_autoHidden && _visibleStack > 0;
  public static Vector3 CursorDelta => _lastMouseDelta;
  public static Vector3 CursorWorldPos => _cursorRect.position;
  public static RectTransform CursorTransform => _cursorRect;

  [SerializeField] private float _autoHideTime = 10.0f;

  private static Canvas _canvas;
  private static RectTransform _canvasRect;
  private static RectTransform _cursorRect;
  private static Image _cursorImage;

  private static float _targetScale;
  private static float _autoHideTimer;
  private static bool _autoHidden;
  private static int _visibleStack;
  private static Vector3 _lastMousePos;
  private static Vector3 _lastMouseDelta;

  // Reset static state for editor without domain reload
#if UNITY_EDITOR
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
  private static void EditorInit()
  {
    _visibleStack = 0;
    _autoHidden = true;
  }
#endif

  public static void PushVisible()
  {
    _visibleStack += 1;
    // Debug.Log($"CanvasCursor.PushVisible: {_visibleStack}");
  }

  public static void PopVisible()
  {
    _visibleStack -= 1;
    // Debug.Log($"CanvasCursor.PopVisible: {_visibleStack}");
  }

  private void Awake()
  {
    _canvas = GetComponentInParent<Canvas>();
    _canvasRect = _canvas.GetComponent<RectTransform>();
    _cursorRect = GetComponent<RectTransform>();
    _cursorImage = GetComponentInChildren<Image>();

    Cursor.visible = false;
    _autoHideTimer = _autoHideTime;
    _lastMousePos = Input.mousePosition;
    _visibleStack = 0;
    _targetScale = 1;
    _autoHidden = true;
  }

  private void Update()
  {
    _cursorImage.enabled = IsVisible;

    Cursor.visible = false;
    Cursor.lockState = _visibleStack > 0 ? CursorLockMode.None : CursorLockMode.Locked;

    // Convert real cursor coords to canvas 
    Vector3 cursorNormalized = Input.mousePosition;
    cursorNormalized.x /= Screen.width;
    cursorNormalized.y /= Screen.height;
    cursorNormalized.z = 0;

    _lastMouseDelta = Input.mousePosition - _lastMousePos;
    _lastMousePos = Input.mousePosition;

    Vector3 cursorPos = cursorNormalized;
    cursorPos.x *= _canvasRect.rect.width;
    cursorPos.y *= _canvasRect.rect.height;

    // Debug.Log($"Input mouse pos {Input.mousePosition.x}x{Input.mousePosition.y}");
    // Debug.Log($"Screen size {Screen.width}x{Screen.height}");
    // Debug.Log($"Canvas rect {_canvasRect.rect.width}x{_canvasRect.rect.height}");
    // Debug.Log($"Normalized pos {cursorNormalized.x}x{cursorNormalized.y}");
    // Debug.Log($"Cursor pos {cursorPos.x}x{cursorPos.y}");

    // Auto hide when mouse doesn't move 
    _autoHidden = _autoHideTimer >= _autoHideTime;
    if (Mathf.Approximately(CursorDelta.x, 0) &&
        Mathf.Approximately(CursorDelta.y, 0))
    {
      _autoHideTimer += Time.unscaledDeltaTime;
    }
    else
    {
      _autoHideTimer = 0;
    }

    // Position cursor icon
    _cursorRect.anchoredPosition = cursorPos;

    // Click animation
    if (Input.GetKey(KeyCode.Mouse0))
    {
      _targetScale = 1.5f;
    }
    else
    {
      _targetScale = 1;
    }

    _cursorRect.localScale = Mathfx.Damp(_cursorRect.localScale, Vector3.one * _targetScale, 0.25f,
      Time.unscaledDeltaTime * 20.0f);
  }
}