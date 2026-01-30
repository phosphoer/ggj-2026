[System.Serializable]
public class BoolStateStack
{
  public event System.Action StatePushed;
  public event System.Action StatePopped;

  public bool IsActive => _count > 0;

  private int _count;
  private bool _warnBelowZero;
  private string _name;

  public BoolStateStack()
  {
    _count = 0;
    _warnBelowZero = true;
    _name = "unnamed";
  }

  public BoolStateStack(bool warnBelowZero, string name)
  {
    _count = 0;
    _warnBelowZero = warnBelowZero;
    _name = name;
  }

  public static implicit operator bool(BoolStateStack rhs)
  {
    return rhs.IsActive;
  }

  public void Set(bool active)
  {
    _count = active ? 1 : 0;
  }

  public void Push()
  {
    _count += 1;
    StatePushed?.Invoke();
  }

  public void Pop()
  {
    _count -= 1;
    if (_count < 0)
    {
      _count = 0;
    }

    StatePopped?.Invoke();
  }
}
