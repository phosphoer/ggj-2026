using UnityEngine;

public class GameGlobals : Singleton<GameGlobals>
{
  public static string FormatDuration(System.TimeSpan timeSpan)
  {
    if (timeSpan.TotalMinutes < 1)
      return $"{timeSpan.TotalSeconds:0} seconds";

    if (timeSpan.TotalHours < 1)
      return $"{timeSpan.TotalMinutes:0} minutes";

    if (timeSpan.TotalDays < 1)
      return $"{timeSpan.TotalHours:0} hours";

    return $"{timeSpan.TotalDays:0.0} days";
  }

  public static string FormatMoney(int moneyAmount)
  {
    return $"$ {moneyAmount}";
  }

  public static string FormatMoneyDelta(int moneyAmount)
  {
    if (moneyAmount == 0)
      return string.Empty;

    string colorTag = moneyAmount >= 0 ? "<color=#6AE25D>" : "<color=#E25D60>";
    string operation = moneyAmount >= 0 ? "+" : "-";
    return $"{operation} {colorTag}$ {Mathf.Abs(moneyAmount)}</color>";
  }

  private void OnEnable()
  {
    Instance = this;
  }

#if UNITY_EDITOR
  [ContextMenu("Gather Assets")]
  private void EditorGatherAssets()
  {
    UnityEditor.Undo.RecordObject(this, "Gather Assets");
    UnityEditor.EditorUtility.SetDirty(this);
  }
#endif
}