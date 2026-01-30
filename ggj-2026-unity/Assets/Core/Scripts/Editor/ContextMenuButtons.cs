// Based heavily on https://github.com/SubjectNerd-Unity/ReorderableInspector
// Pulled out just the relevant bits to make the context menu buttons work
// Put this in an Editor folder and mark a method on a component with [ContextMenu] to get a clickable button in the inspector!

using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System;

[CustomEditor(typeof(UnityEngine.Object), true, isFallback = true)]
[CanEditMultipleObjects]
public class ContextMenuButtons : Editor
{
  private struct ContextMenuData
  {
    public string MenuItem;
    public MethodInfo Function;
    public MethodInfo Validate;

    public ContextMenuData(string item)
    {
      MenuItem = item;
      Function = null;
      Validate = null;
    }
  }

  private Dictionary<string, ContextMenuData> _contextData = new Dictionary<string, ContextMenuData>();

  public override void OnInspectorGUI()
  {
    base.OnInspectorGUI();
    DrawContextMenuButtons();
  }

  private void OnEnable()
  {
    FindContextMenu();
  }

  private IEnumerable<MethodInfo> GetAllMethods(Type t)
  {
    if (t == null)
      return Enumerable.Empty<MethodInfo>();
    var binding = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    return t.GetMethods(binding).Concat(GetAllMethods(t.BaseType));
  }

  private void FindContextMenu()
  {
    _contextData.Clear();

    // Get context menu
    Type targetType = target.GetType();
    Type contextMenuType = typeof(ContextMenu);
    MethodInfo[] methods = GetAllMethods(targetType).ToArray();
    for (int index = 0; index < methods.GetLength(0); ++index)
    {
      MethodInfo methodInfo = methods[index];
      foreach (ContextMenu contextMenu in methodInfo.GetCustomAttributes(contextMenuType, false))
      {
        if (_contextData.ContainsKey(contextMenu.menuItem))
        {
          var data = _contextData[contextMenu.menuItem];
          if (contextMenu.validate)
            data.Validate = methodInfo;
          else
            data.Function = methodInfo;

          _contextData[data.MenuItem] = data;
        }
        else
        {
          var data = new ContextMenuData(contextMenu.menuItem);
          if (contextMenu.validate)
            data.Validate = methodInfo;
          else
            data.Function = methodInfo;

          _contextData.Add(data.MenuItem, data);
        }
      }
    }
  }

  private void DrawContextMenuButtons()
  {
    if (_contextData.Count == 0)
      return;

    EditorGUILayout.Space();
    EditorGUILayout.LabelField("Context Menu", EditorStyles.boldLabel);
    foreach (KeyValuePair<string, ContextMenuData> kv in _contextData)
    {
      bool enabledState = GUI.enabled;
      bool isEnabled = true;
      if (kv.Value.Validate != null)
        isEnabled = (bool)kv.Value.Validate.Invoke(target, null);

      GUI.enabled = isEnabled;
      if (GUILayout.Button(kv.Key) && kv.Value.Function != null)
      {
        kv.Value.Function.Invoke(target, null);
      }
      GUI.enabled = enabledState;
    }
  }
}