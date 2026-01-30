// The MIT License (MIT)
// Copyright (c) 2025 David Evans @festivevector
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEditor;
using UnityEngine;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using UnityEditor.U2D.Sprites;
using System.Linq;

// Put this in an Editor folder somewhere in Assets/
// Open with Window->Xml Sprite Slicer
public class XMLSpriteSlicer : EditorWindow
{
  [SerializeField] private Texture2D _targetSprite = null;
  [SerializeField] private TextAsset _xmlTextAsset = null;

  [System.Serializable]
  [XmlRoot("TextureAtlas")]
  public class XmlRoot
  {
    [XmlElement("SubTexture")]
    public XmlSubTexture[] SubTextures;
  }

  [System.Serializable]
  public struct XmlSubTexture
  {
    [XmlIgnore] public Rect Rect => new Rect(X, Y, Width, Height);
    [XmlAttribute("name")] public string Name;
    [XmlAttribute("x")] public int X;
    [XmlAttribute("y")] public int Y;
    [XmlAttribute("width")] public int Width;
    [XmlAttribute("height")] public int Height;
  }

  [MenuItem("Window/XML Sprite Slicer")]
  public static void ShowWindow()
  {
    EditorWindow.GetWindow<XMLSpriteSlicer>("XML Sprite Slicer");
  }

  private void OnGUI()
  {
    EditorGUILayout.BeginVertical();

    _targetSprite = EditorGUILayout.ObjectField(_targetSprite, typeof(Texture2D), allowSceneObjects: false) as Texture2D;
    _xmlTextAsset = EditorGUILayout.ObjectField(_xmlTextAsset, typeof(TextAsset), allowSceneObjects: false) as TextAsset;

    if (GUILayout.Button("Slice"))
    {
      string xmlText = _xmlTextAsset.text;
      var xmlReader = new XmlSerializer(typeof(XmlRoot));
      var textStream = new MemoryStream(System.Text.Encoding.Unicode.GetBytes(xmlText));
      XmlRoot xmlRoot = xmlReader.Deserialize(textStream) as XmlRoot;
      SliceSpritesheet(_targetSprite, xmlRoot);
    }

    EditorGUILayout.EndVertical();
  }

  public static void SliceSpritesheet(Texture2D targetTexture, XmlRoot xmlRoot)
  {
    // Update the texture importer to use multiple sprite mode if it isn't set yet
    string path = AssetDatabase.GetAssetPath(targetTexture);
    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
    if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Multiple)
    {
      importer.textureType = TextureImporterType.Sprite;
      importer.spriteImportMode = SpriteImportMode.Multiple;
      importer.SaveAndReimport();
    }

    // Get weird sprite factory thingy to begin modifying spritesheet data
    var factory = new SpriteDataProviderFactories();
    factory.Init();
    var dataProvider = factory.GetSpriteEditorDataProviderFromObject(targetTexture);
    dataProvider.InitSpriteEditorDataProvider();

    // Get lists of sprite info to modify and reset them
    var spriteRects = dataProvider.GetSpriteRects().ToList();
    spriteRects.Clear();

    var spriteNameFileIdDataProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
    var nameFileIdPairs = spriteNameFileIdDataProvider.GetNameFileIdPairs().ToList();
    nameFileIdPairs.Clear();

    // For each sub texture add a sprite
    foreach (var subTexture in xmlRoot.SubTextures)
    {
      var newSprite = new SpriteRect()
      {
        name = subTexture.Name,
        spriteID = GUID.Generate(),
        rect = subTexture.Rect
      };

      // Add the sprite info
      spriteRects.Add(newSprite);

      // Register the new Sprite Rect's name and GUID with the ISpriteNameFileIdDataProvider
      nameFileIdPairs.Add(new SpriteNameFileIdPair(newSprite.name, newSprite.spriteID));
    }

    // Apply the changes to the sprite and reimport the asset
    dataProvider.SetSpriteRects(spriteRects.ToArray());
    spriteNameFileIdDataProvider.SetNameFileIdPairs(nameFileIdPairs);

    dataProvider.Apply();
    var assetImporter = dataProvider.targetObject as AssetImporter;
    assetImporter.SaveAndReimport();
  }
}


