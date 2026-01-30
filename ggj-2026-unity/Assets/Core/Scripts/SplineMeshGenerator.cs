// The MIT License (MIT)
// Copyright (c) 2025 David Evans @festivevector
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;

[ExecuteAlways]
public class SplineMeshGenerator : MonoBehaviour
{
  public SplineContainer SplineContainer = null;
  public Mesh StartMesh = null;
  public Mesh EndMesh = null;
  public Mesh[] SectionMeshes = null;
  public float SectionLength = 1;
  public float SectionScale = 1;
  public bool UseMeshBoundsForLength = false;
  public bool RandomizeSections = true;
  public Material Material = null;
  public bool GenerateCollider = false;
  public SplineMeshComponent[] Components = null;

  private bool _isDirty;
  [System.NonSerialized] private bool _subscribedToChanged;
  private Mesh _generatedMesh;
  private Vector3 _lastGenPosition;
  private Vector3 _lastGenScale;
  private Quaternion _lastGenRotation;
  private List<CombineInstance> _combineInstances = new();

  private void Start()
  {
    RebuildMesh();
  }

  private void OnDestroy()
  {
    if (_generatedMesh != null)
      MeshPool.FreeMesh(_generatedMesh);

    _generatedMesh = null;

#if UNITY_EDITOR
    Spline.Changed -= OnSplineChanged;
#endif
  }


#if UNITY_EDITOR
  private void OnSplineChanged(Spline spline, int knotIndex, SplineModification modification)
  {
    // Rebuild the mesh when a spline we own changes
    if (SplineContainer && SplineContainer.Splines.Contains(spline))
    {
      RebuildMesh();
    }
  }

  private void Update()
  {
    // Watch for scene changes that require us to rebuild
    if (!Application.isPlaying)
    {
      if (!_subscribedToChanged)
      {
        _subscribedToChanged = true;
        Spline.Changed += OnSplineChanged;
      }

      Transform spawnTransform = transform;

      _isDirty |= _lastGenPosition != spawnTransform.position;
      _isDirty |= _lastGenScale != spawnTransform.localScale;
      _isDirty |= _lastGenRotation != spawnTransform.rotation;

      foreach (var component in Components)
        _isDirty |= component.NeedsRebuild;

      if (_isDirty)
        RebuildMesh();
    }
  }

  private void OnValidate()
  {
    // Rebuild when our inspector changes
    if (!Application.isPlaying)
    {
      _isDirty = true;
    }
  }
#endif

  private void RebuildMesh()
  {
    if (!SplineContainer || SectionMeshes == null || SectionMeshes.Length == 0)
      return;

    // Acquire a mesh
    if (_generatedMesh == null)
    {
      _generatedMesh = MeshPool.GetMesh();
      _generatedMesh.hideFlags = HideFlags.DontSave;
      _generatedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
    }

    // Store the last build info
    _lastGenPosition = transform.position;
    _lastGenScale = transform.localScale;
    _lastGenRotation = transform.rotation;
    _isDirty = false;

    int nextSectionIndex = 0;
    _combineInstances.Clear();
    if (SplineContainer.Splines.Count > 0)
    {
      // Build a straight line of meshes as long as the spline
      var spline = SplineContainer.Spline;
      float splineLength = spline.GetLength();
      float meshLength = 0;
      while (meshLength < splineLength)
      {
        if (RandomizeSections)
          nextSectionIndex = Random.Range(0, SectionMeshes.Length);

        float splineT = meshLength / splineLength;
        Mesh sectionMesh = SectionMeshes[nextSectionIndex];
        nextSectionIndex = Mathfx.Wrap(nextSectionIndex + 1, 0, SectionMeshes.Length - 1);

        if (_combineInstances.Count == 0 && StartMesh)
          sectionMesh = StartMesh;
        else if (meshLength + GetSectionLength(sectionMesh) >= splineLength && EndMesh)
          sectionMesh = EndMesh;

        float sectionLength = GetSectionLength(sectionMesh);
        meshLength += sectionLength;
        if (sectionMesh == EndMesh)
          meshLength = splineLength;

        CombineInstance meshInstance = default;
        meshInstance.mesh = sectionMesh;
        meshInstance.transform = Matrix4x4.TRS(splineLength * splineT * Vector3.forward, Quaternion.identity, Vector3.one * SectionScale);
        meshInstance.subMeshIndex = 0;
        _combineInstances.Add(meshInstance);
      }

      // Combine the straight line into a single mesh
      // and then deform the verts to fit the split
      // If we think about the existing mesh verts as in the local space of the spline
      // then we can map the mesh onto the spline with x = splineRight, y = splineUp and z = splineTangent
      if (_combineInstances.Count > 0)
      {
        _generatedMesh.CombineMeshes(_combineInstances.ToArray(), mergeSubMeshes: true, useMatrices: true, hasLightmapData: false);

        Vector3[] vertices = _generatedMesh.vertices;
        for (int j = 0; j < vertices.Length; ++j)
        {
          Vector3 vertexPos = vertices[j];
          float splineT = vertexPos.z / splineLength;
          if (spline.Evaluate(splineT, out var pos, out var tangent, out var up))
          {
            Vector3 splinePos = pos;
            Vector3 splineTangent = tangent;
            Vector3 splineUp = up;
            Vector3 splineRight = Vector3.Cross(splineUp, splineTangent).normalized;
            if (splineT > 1)
              splinePos += splineTangent.normalized * (splineLength * (splineT - 1));
            Vector3 deformedVertexPos = splinePos + splineRight * vertexPos.x + splineUp * vertexPos.y;
            vertices[j] = deformedVertexPos;
          }
        }

        _generatedMesh.SetVertices(vertices);

        for (int i = 0; i < Components.Length; ++i)
        {
          var component = Components[i];
          component.ApplyMeshModifier(_generatedMesh, transform);
        }

        _generatedMesh.RecalculateBounds();
        _generatedMesh.RecalculateNormals();
        _generatedMesh.Optimize();
        _generatedMesh.UploadMeshData(markNoLongerReadable: false);
        _generatedMesh.name = $"generated-{name}";
      }
    }

    // Set up rendering components for the generated mesh
    MeshFilter meshFilter = gameObject.GetOrAddComponent<MeshFilter>();
    meshFilter.sharedMesh = _generatedMesh;
    meshFilter.hideFlags = HideFlags.DontSave;

    MeshRenderer meshRenderer = gameObject.GetOrAddComponent<MeshRenderer>();
    meshRenderer.sharedMaterial = Material;
    meshRenderer.hideFlags = HideFlags.DontSave;

    if (GenerateCollider)
    {
      MeshCollider meshCollider = gameObject.GetOrAddComponent<MeshCollider>();
      meshCollider.sharedMesh = _generatedMesh;
    }
  }

  private float GetSectionLength(Mesh sectionMesh)
  {
    float sectionLength = (UseMeshBoundsForLength ? sectionMesh.bounds.size.z : SectionLength) * SectionScale;
    return sectionLength;
  }
}