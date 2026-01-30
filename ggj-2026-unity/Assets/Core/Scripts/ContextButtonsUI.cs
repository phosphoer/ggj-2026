using UnityEngine;
using System.Collections.Generic;

public class ContextButtonsUI : UIPageBase
{
  [SerializeField] private Transform _contextButtonRoot = null;
  [SerializeField] private ContextButtonItemUI _contextButtonPrefab = null;

  private List<ContextStack> _contextStack = new();

  private struct ContextStack
  {
    public List<ContextButtonItemUI> Items;
    public string Name;
  }

  public void PushContext(string contextName)
  {
    ContextStack newStack = default;
    newStack.Items = new();
    newStack.Name = contextName;
    _contextStack.Add(newStack);

    EnsureStack();
  }

  public void PopContext(string contextName)
  {
    for (int i = 0; i < _contextStack.Count; ++i)
    {
      ContextStack context = _contextStack[i];
      if (context.Name == contextName)
      {
        foreach (var contextItem in context.Items)
          Destroy(contextItem.gameObject);

        context.Items.Clear();
        _contextStack.RemoveAt(i);

        EnsureStack();

        return;
      }
    }
  }

  public void AddAction(int actionId, string label)
  {
    ContextButtonItemUI contextButton = Instantiate(_contextButtonPrefab, _contextButtonRoot);
    contextButton.ActionId = actionId;
    contextButton.ActionLabel = label;

    ContextStack context = _contextStack[^1];
    context.Items.Add(contextButton);
  }

  public void RemoveAction(int actionId)
  {
    ContextStack context = _contextStack[^1];

    for (int i = 0; i < context.Items.Count; ++i)
    {
      if (context.Items[i].ActionId == actionId)
      {
        Destroy(context.Items[i].gameObject);
        context.Items.RemoveAt(i);
        return;
      }
    }
  }

  protected override void Awake()
  {
    base.Awake();
    Shown += OnShown;
    Hidden += OnHidden;
  }

  private void OnShown()
  {
  }

  private void OnHidden()
  {
  }

  private void EnsureStack()
  {
    for (int i = 0; i < _contextStack.Count; ++i)
    {
      bool isTopOfStack = i == _contextStack.Count - 1;
      ContextStack context = _contextStack[i];
      foreach (var item in context.Items)
        item.gameObject.SetActive(isTopOfStack);
    }
  }
}