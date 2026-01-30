using UnityEngine;
using System.Collections.Generic;

public class MenuFocus : MonoBehaviour
{
  public static event System.Action FocusedChanged;
  public static MenuFocus CurrentFocus => _focusStack.Count > 0 ? _focusStack[^1] : null;
  public static bool AnyFocusTaken => _focusStack.Count > 0;
  public static bool ForceDisableAllFocus { get; set; }

  public bool HasFocus => ReferenceEquals(CurrentFocus, this) && enabled && !ForceDisableAllFocus;

  private static List<MenuFocus> _focusStack = new();

  // Reset static state for editor without domain reload
#if UNITY_EDITOR
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
  private static void EditorInit()
  {
    FocusedChanged = null;
    _focusStack.Clear();
  }
#endif

  public static void PushFocus(MenuFocus menuInput)
  {
    _focusStack.Add(menuInput);
    FocusedChanged?.Invoke();
  }

  public static void PopFocus(MenuFocus menuInput)
  {
    _focusStack.Remove(menuInput);
    FocusedChanged?.Invoke();
  }

  private void OnEnable()
  {
    PushFocus(this);
  }

  private void OnDisable()
  {
    PopFocus(this);
  }
}