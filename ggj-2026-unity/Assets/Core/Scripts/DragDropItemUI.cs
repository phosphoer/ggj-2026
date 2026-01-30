using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DragDropItemUI : MonoBehaviour
{
  public DragDropItem Item
  {
    get => _dragDropItem;
    set
    {
      _dragDropItem = value;
      _icon.Prefab = _dragDropItem.DragIcon;
      _text.text = _dragDropItem.DragLabel;
      _quantityLabel.text = $"{_dragDropItem.DragQuantity}";
    }
  }

  public UIHydrate UIHydrate => _uiHydrate;
  public MeshCanvasUI Icon => _icon;

  [SerializeField] private MeshCanvasUI _icon = null;
  [SerializeField] private TMP_Text _text = null;
  [SerializeField] private TMP_Text _quantityLabel = null;
  [SerializeField] private UIHydrate _uiHydrate = null;

  private DragDropItem _dragDropItem = null;
}