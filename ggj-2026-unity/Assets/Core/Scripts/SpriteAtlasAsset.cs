// The MIT License (MIT)
// Copyright (c) 2021 David Evans @phosphoer
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


using UnityEngine;

// Usage: Create an asset from the Assets menu, and add the textures you want in the atlas to the Textures list. 
// Then click the 'Generate Atlas' button to create a texture named AtlasName. Then use Unity's sprite editor to slice
// up the atlas like normal, or do whatever else you like with it.
//
// Note: There is nothing that prevents you from having more textures than can fit in the atlas for the given cell size,
// so watch out!
[CreateAssetMenu(fileName = "new-sprite-atlas", menuName = "Sprite Atlas Asset")]
public class SpriteAtlasAsset : ScriptableObject
{
  public string AtlasName = "sprite-atlas";
  public int AtlasWidth = 512;
  public int AtlasHeight = 512;
  public int CellWidth = 128;
  public int CellHeight = 128;

  [Tooltip("Re-imports source textures without compression for atlas creation, then re-imports them again with their original compression")]
  public bool DisableCompression = true;

  [Tooltip("List of textures to build into the atlas")]
  public Texture2D[] Textures = null;
}