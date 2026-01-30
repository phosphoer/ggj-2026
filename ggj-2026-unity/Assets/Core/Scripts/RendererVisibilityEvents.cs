using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class RendererVisiblityEvents : MonoBehaviour
{
  public event System.Action BecameVisible;
  public event System.Action BecameInvisible;
  public event System.Action<bool> VisibilityChanged;

  private void OnBecameVisible()
  {
    BecameVisible?.Invoke();
    VisibilityChanged?.Invoke(true);
  }

  private void OnBecameInvisible()
  {
    BecameInvisible?.Invoke();
    VisibilityChanged?.Invoke(false);
  }
}