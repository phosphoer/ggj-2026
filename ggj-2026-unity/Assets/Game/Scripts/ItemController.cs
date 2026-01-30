using UnityEngine;
using System.Collections;


public class ItemController : MonoBehaviour
{
  public Interactable Interactable => _interactable;
 
  [SerializeField]
  private Interactable _interactable = null;

  [SerializeField]
  private Rigidbody _rigidbody = null;

  public void SetInteractable(bool isInteractable)
  {
    _interactable.enabled = isInteractable;
    _rigidbody.isKinematic = !isInteractable;
  }

  private void Awake()
  {
  }
}