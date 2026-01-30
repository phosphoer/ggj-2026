using UnityEngine;

public class ContextButtonItemUI : MonoBehaviour
{
  public int ActionId
  {
    get => _actionId;
    set
    {
      _actionId = value;
      _actionIcon.SetAction(_actionId);
    }
  }

  public string ActionLabel
  {
    get => _actionLabel.text;
    set => _actionLabel.text = value;
  }

  [SerializeField] private InputIconRendererBase _actionIcon = null;
  [SerializeField] private TMPro.TMP_Text _actionLabel = null;

  private int _actionId;
}