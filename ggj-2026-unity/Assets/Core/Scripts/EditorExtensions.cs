#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class EditorExtensions
{
  public static List<T> FindAssetsByType<T>() where T : UnityEngine.Object
  {
    return FindAssetsByType<T>(typeof(T).ToString());
  }

  public static List<T> FindAssetsByType<T>(string typeName) where T : UnityEngine.Object
  {
    List<T> assets = new List<T>();
    string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeName));
    for (int i = 0; i < guids.Length; i++)
    {
      string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
      T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
      if (asset != null)
      {
        assets.Add(asset);
      }
    }

    return assets;
  }


  [MenuItem("Utilities/Asset Stats")]
  public static void PrintAssetStats()
  {
    var meshes = FindAssetsByType<Mesh>("mesh");
    var textures = FindAssetsByType<Texture>("texture");
    var materials = FindAssetsByType<Material>("material");
    var customAssets = FindAssetsByType<ScriptableObject>("scriptableobject");
    Debug.Log($"Mesh count: {meshes.Count}");
    Debug.Log($"Texture count: {textures.Count}");
    Debug.Log($"Material count: {materials.Count}");
    Debug.Log($"Custom Assets count: {customAssets.Count}");
  }
}
#endif
