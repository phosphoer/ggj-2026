using UnityEngine;
using UnityEngine.UI;

public class UIViewportScaler : MonoBehaviour
{
  [SerializeField] private Vector2 _referenceResolution = new Vector2(1280, 800);

  private float _lastScale;
  private CanvasScaler _canvasScaler;

  private void Start()
  {
    _canvasScaler = GetComponent<CanvasScaler>();
  }

  private void Update()
  {
    float scaleX = Screen.width / _referenceResolution.x;
    float scaleY = Screen.height / _referenceResolution.y;
    float minScale = Mathf.Min(scaleX, scaleY);
    if (scaleX == 0 || scaleY == 0 || Mathf.Abs(_lastScale - minScale) < 0.01f)
      return;

    _canvasScaler.scaleFactor = minScale;
    _lastScale = minScale;

    Debug.Log($"Applying UI scale of {_lastScale} for screen res {Screen.width}x{Screen.height}");
    Canvas.ForceUpdateCanvases();
  }

  [ContextMenu("Apply Scale")]
  private void ApplyScale()
  {
    Update();
  }
}