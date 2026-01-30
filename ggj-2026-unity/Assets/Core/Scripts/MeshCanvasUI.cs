// The MIT License (MIT)
// Copyright (c) 2024 David Evans @festivevector
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// This component displays a mesh within a RectTransform at a 3/4 angle and scaled to fit within the parent rect
// It supports clipping from a RectMask, which will require support from any shaders used
[RequireComponent(typeof(RectTransform))]
[ExecuteAlways]
public class MeshCanvasUI : MonoBehaviour, IClippable
{
  public GameObject Prefab
  {
    get => _prefab;
    set
    {
      _prefab = value;
      RefreshUI();
    }
  }

  public MeshRenderer MeshRenderer => _meshRenderer;
  public Transform VisualRoot => _scaleRoot;

  [SerializeField] private GameObject _prefab = null;

  private MeshRenderer _meshRenderer;
  private MeshFilter _meshFilter;
  private Transform _scaleRoot;
  private Mesh _mesh;
  private GameObject _lastMeshPrefab;
  private List<Material> _materials = new();
  private List<Material> _materialInstances = new();

  private static readonly int KShaderClipRect = Shader.PropertyToID("_ClipRect");
  private static readonly int KShaderSoftness = Shader.PropertyToID("_UIMaskSoftness");

  private void Start()
  {
    RefreshUI();
  }

  private void Update()
  {
    if (transform.hasChanged)
    {
      RefreshUI();
      transform.hasChanged = false;
    }

    if (_prefab != _lastMeshPrefab)
      RefreshUI();
  }

  private void OnDestroy()
  {
    if (_mesh != null)
      MeshPool.FreeMesh(_mesh);

    _mesh = null;

    for (int i = 0; i < _materialInstances.Count; i++)
      DestroyImmediate(_materialInstances[i]);
  }

  private void RefreshUI()
  {
    if (_mesh == null)
      _mesh = MeshPool.GetMesh();

    if (_prefab != null)
    {
      // Rebuild mesh if necessary
      bool meshChanged = false;
      if (_prefab != _lastMeshPrefab)
      {
        for (int i = 0; i < _materialInstances.Count; i++)
          DestroyImmediate(_materialInstances[i]);

        _lastMeshPrefab = _prefab;
        MeshCombiner.MakeCombinedMesh(_prefab.transform, _mesh, _materials);
        _mesh.name = $"{name}-mesh";

        // Instantiate materials
        _materialInstances.Clear();
        for (int i = 0; i < _materials.Count; i++)
          _materialInstances.Add(Instantiate(_materials[i]));

        meshChanged = true;
      }

      if (_scaleRoot == null)
      {
        _scaleRoot = new GameObject("mesh-canvas-scale").transform;
        _scaleRoot.parent = transform;
        _scaleRoot.SetIdentityTransformLocal();
        _scaleRoot.gameObject.layer = gameObject.layer;
        _scaleRoot.gameObject.hideFlags = HideFlags.DontSave;
      }

      // Create renderer
      if (_meshRenderer == null)
      {
        GameObject childVisual = new GameObject("mesh-canvas-visual");
        childVisual.transform.SetParent(_scaleRoot);
        childVisual.transform.SetIdentityTransformLocal();
        childVisual.layer = gameObject.layer;
        childVisual.hideFlags = HideFlags.DontSave;
        _meshFilter = childVisual.GetOrAddComponent<MeshFilter>();
        _meshRenderer = childVisual.GetOrAddComponent<MeshRenderer>();
      }

      // Set mesh on visual
      if (meshChanged)
      {
        _meshFilter.sharedMesh = _mesh;
        _meshRenderer.sharedMaterials = _materialInstances.ToArray();
      }

      // Update size of visual to fit in rect
      RectTransform rectTransform = (RectTransform)transform;
      Vector3 lossyScale = rectTransform.lossyScale;
      if (lossyScale.x > 0 && lossyScale.y > 0)
      {
        Vector3 rectSize = Mathfx.GetRectTransformWorldSize(rectTransform);
        rectSize.x /= lossyScale.x;
        rectSize.y /= lossyScale.y;
        float rectSizeMax = Mathf.Max(rectSize.x, rectSize.y);

        _scaleRoot.localScale = Vector3.one;
        _meshRenderer.transform.localPosition = Vector3.zero;
        _meshRenderer.transform.localScale = Vector3.one;
        _meshRenderer.transform.localEulerAngles = new Vector3(20, -130, 20);

        Bounds meshBounds = _meshRenderer.bounds;
        Vector3 meshSize = meshBounds.size / lossyScale.x;
        Vector3 scaleRatio = new Vector3(
          rectSize.x / meshSize.x,
          rectSize.y / meshSize.y,
          rectSizeMax / meshSize.z);

        float scaleUniform = Mathf.Min(scaleRatio.x, scaleRatio.y, scaleRatio.z);
        _scaleRoot.localScale = (Vector3.one * scaleUniform).WithZ(scaleUniform * 0.5f);

        _meshRenderer.transform.localPosition = (rectTransform.position - meshBounds.center) / lossyScale.x;
        RecalculateClipping();
      }
    }
    else
    {
      _lastMeshPrefab = null;
      if (_mesh != null)
        _mesh.Clear();
    }
  }

  private void OnRectTransformDimensionsChange()
  {
    RecalculateClipping();
  }

  public void RecalculateClipping()
  {
    var rectMask = GetComponentInParent<RectMask2D>();
    if (rectMask != null)
    {
      Canvas parentCanvas = GetComponentInParent<Canvas>();
      Rect canvasRect = rectMask.canvasRect;
      Vector4 clipRect = new Vector4(canvasRect.xMin, canvasRect.yMin, canvasRect.xMax, canvasRect.yMax);
      Vector2 softnessWorld = ((Vector2)rectMask.softness) * parentCanvas.transform.lossyScale.x;

      Vector3 clipMinPos = parentCanvas.transform.TransformPoint(new Vector3(clipRect.x, clipRect.y, 0));
      Vector3 clipMaxPos = parentCanvas.transform.TransformPoint(new Vector3(clipRect.z, clipRect.w, 0));
      Vector4 clipRectWorld = new Vector4(clipMinPos.x, clipMinPos.y, clipMaxPos.x, clipMaxPos.y);

      for (int i = 0; i < _materialInstances.Count; i++)
      {
        _materialInstances[i].SetVector(KShaderClipRect, clipRectWorld);
        _materialInstances[i].SetVector(KShaderSoftness, softnessWorld);
        _materialInstances[i].EnableKeyword("UNITY_UI_CLIP_RECT");
      }
    }
    else
    {
      for (int i = 0; i < _materialInstances.Count; i++)
      {
        _materialInstances[i].SetVector(KShaderClipRect, Vector4.zero);
        _materialInstances[i].DisableKeyword("UNITY_UI_CLIP_RECT");
      }
    }
  }

  public void Cull(Rect clipRect, bool validRect)
  {
  }

  public void SetClipRect(Rect canvasRect, bool validRect)
  {
    Debug.Log("SetClipRect");
    if (validRect)
    {
      Vector4 clipRect = new Vector4(canvasRect.xMin, canvasRect.yMin, canvasRect.xMax, canvasRect.yMax);

      for (int i = 0; i < _materialInstances.Count; i++)
        _materialInstances[i].SetVector(KShaderClipRect, clipRect);
    }
  }

  public void SetClipSoftness(Vector2 clipSoftness)
  {
  }

  public RectTransform rectTransform => transform as RectTransform;
}