using System.Collections.Generic;

[System.Serializable]
public class StateMachine<T>
{
  public event System.Action StateUpdate;
  public event System.Action StateEnter;
  public event System.Action StateExit;

  public T CurrentState => _currentState;
  public T LastState => _lastState;
  public float CurrentStateTime => _stateTimer;
  public bool DidStateChange => _didStateChange;

  private T _currentState;
  private T _lastState;
  private T _nextState;
  private float _stateTimer;
  private bool _pendingChange;
  private bool _didStateChange;

  public void Reset()
  {
    _lastState = default;
    _nextState = default;
    _stateTimer = default;
    _pendingChange = default;
    _didStateChange = default;
  }

  public void Update(float dt)
  {
    if (_didStateChange)
      _didStateChange = false;

    if (_pendingChange)
    {
      StateExit?.Invoke();
      _lastState = _currentState;
      _currentState = _nextState;
      _stateTimer = 0;
      _pendingChange = false;
      _didStateChange = true;
      StateEnter?.Invoke();
    }

    _stateTimer += dt;
    StateUpdate?.Invoke();
  }

  public void GoToState(T nextState)
  {
    _nextState = nextState;
    _pendingChange = true;
  }
}