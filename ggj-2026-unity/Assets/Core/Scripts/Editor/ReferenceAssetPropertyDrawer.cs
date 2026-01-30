#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.IO;

[CustomPropertyDrawer(typeof(ReferenceAsset<>))]
public class ReferenceAssetDrawer : PropertyDrawer
{
    const string RootFolder = "Assets/RuntimeReferences";

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty assetProp = property.FindPropertyRelative("_asset");
        Type expectedType = fieldInfo.FieldType.GetGenericArguments()[0];

        EditorGUI.BeginProperty(position, label, property);

        // Layout
        Rect fieldRect = position;
        fieldRect.width -= 22;

        Rect buttonRect = position;
        buttonRect.x = position.xMax - 20;
        buttonRect.width = 20;

        // Object field
        UnityEngine.Object obj = EditorGUI.ObjectField(
            fieldRect,
            label,
            assetProp.objectReferenceValue,
            typeof(RuntimeReferenceAsset),
            false);

        if (obj != null)
        {
            RuntimeReferenceAsset asset = obj as RuntimeReferenceAsset;
            if (asset == null || asset.ValueType != expectedType)
            {
                Debug.LogError(
                    $"ReferenceAsset type mismatch. Expected {expectedType}, got {asset?.ValueType}");
                obj = null;
            }
        }

        assetProp.objectReferenceValue = obj;

        // + button
        if (GUI.Button(buttonRect, "+"))
        {
            CreateAndAssignAsset(assetProp, expectedType);
        }

        EditorGUI.EndProperty();
    }

    static void CreateAndAssignAsset(SerializedProperty assetProp, Type valueType)
    {
        EnsureFolder();

        var asset = ScriptableObject.CreateInstance<RuntimeReferenceAsset>();
        asset.ValueType = valueType;

        string assetName = $"{valueType.Name}Reference.asset";
        string path = AssetDatabase.GenerateUniqueAssetPath(
            Path.Combine(RootFolder, assetName));

        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();

        assetProp.objectReferenceValue = asset;
        assetProp.serializedObject.ApplyModifiedProperties();

        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(RootFolder))
        {
            AssetDatabase.CreateFolder("Assets", "RuntimeReferences");
        }
    }
}
#endif
