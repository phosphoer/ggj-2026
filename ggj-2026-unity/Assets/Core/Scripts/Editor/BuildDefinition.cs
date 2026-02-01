#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Build;

[CreateAssetMenu(fileName = "new-build-definition", menuName = "Build Definition")]
public class BuildDefinition : ScriptableObject
{
  public string LocationPathName = "relative/path/build.exe";
  public string ProductNameOverride;
  public string CompanyNameOverride;

  public BuildTarget BuildTarget;
  public BuildTargetGroup BuildTargetGroup;

  public NamedBuildTarget BuildTargetName => NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup);

  public string[] Defines;
  public SceneField[] Scenes;
  public bool WriteBuildInfo = true;
  public string BuildInfoFile = "BuildInfo.cs";
  public string BuildName = "Main";

  private const string kBuildVersionText =
    "public static class BuildInfo {{ public static string Name = \"{0}\"; public static string Date = \"{1}\"; public static long Number = {2}; }}";

  public void Build(long buildNumber = 0)
  {
    var report = BuildPipeline.BuildPlayer(ApplyBuildSettings(buildNumber));
    Debug.Log($"{name} completed with result: {report.summary.result}");
  }

  [ContextMenu("Copy Scenes From Build Settings")]
  public void CopySceneListFromBuild()
  {
    Scenes = new SceneField[EditorBuildSettings.scenes.Length];
    for (int i = 0; i < EditorBuildSettings.scenes.Length; ++i)
    {
      var sceneBuildSetting = EditorBuildSettings.scenes[i];
      var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(sceneBuildSetting.path);
      Scenes[i] = new SceneField { sceneAsset = sceneAsset };
    }
  }

  public BuildPlayerOptions ApplyBuildSettings(long buildNumber)
  {
    if (WriteBuildInfo)
    {
      string pathToBuildAsset = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(this));
      string versionFileText = string.Format(
        kBuildVersionText,
        BuildName,
        System.DateTime.Now.ToString(),
        buildNumber
      );

      System.IO.File.WriteAllText(
        System.IO.Path.Combine(pathToBuildAsset, BuildInfoFile),
        versionFileText
      );

      AssetDatabase.Refresh();
    }

    // Modern define handling (NamedBuildTarget-based)
    string defineList = string.Join(";", Defines);
    PlayerSettings.SetScriptingDefineSymbols(BuildTargetName, defineList);

    if (!string.IsNullOrEmpty(ProductNameOverride))
      PlayerSettings.productName = ProductNameOverride;

    if (!string.IsNullOrEmpty(CompanyNameOverride))
      PlayerSettings.companyName = CompanyNameOverride;

    // Build scene list
    List<string> sceneList = new List<string>();
    List<EditorBuildSettingsScene> editorScenes = new List<EditorBuildSettingsScene>();

    foreach (SceneField scene in Scenes)
    {
      string path = AssetDatabase.GetAssetPath(scene.sceneAsset);
      sceneList.Add(path);
      editorScenes.Add(new EditorBuildSettingsScene(path, true));
    }

    EditorBuildSettings.scenes = editorScenes.ToArray();

    // Switch build target (Unity 6+ API)
    EditorUserBuildSettings.SwitchActiveBuildTarget(
      BuildTargetName,
      BuildTarget
    );

    // Build options (NO targetGroup anymore)
    BuildPlayerOptions buildOptions = new BuildPlayerOptions
    {
      target = BuildTarget,
      locationPathName = LocationPathName,
      scenes = sceneList.ToArray()
    };

    return buildOptions;
  }
}

#endif
