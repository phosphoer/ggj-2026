using UnityEngine;

public class MonoBehaviourEvents : MonoBehaviour
{
  public event System.Action<GameObject> EventAwake;
  public event System.Action<GameObject> EventOnEnable;
  public event System.Action<GameObject> EventOnDisable;
  public event System.Action<GameObject> EventStart;
  public event System.Action<GameObject> EventOnDestroy;

  private void Awake()
  {
    EventAwake?.Invoke(gameObject);
  }

  private void OnEnable()
  {
    EventOnEnable?.Invoke(gameObject);
  }

  private void OnDisable()
  {
    EventOnDisable?.Invoke(gameObject);
  }

  private void Start()
  {
    EventStart?.Invoke(gameObject);
  }

  private void OnDestroy()
  {
    EventOnDestroy?.Invoke(gameObject);
  }
}