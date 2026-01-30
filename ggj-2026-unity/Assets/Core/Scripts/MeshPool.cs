// The MIT License (MIT)
// Copyright (c) 2024 David Evans @festivevector
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEngine;
using System.Collections.Generic;

public static class MeshPool
{
  private static List<Mesh> _meshPool = new List<Mesh>();
  private static int _statsCounter = kDebugStatsInterval;
  private static int _meshActiveCount = 0;
  private static bool _isExitingPlayMode;

  // How often to debug print stats about the mesh pool
  private const int kDebugStatsInterval = 10;

  // How many meshes max can fit in the pool
  // Meshes above that will be destroyed instead of stored in the pool
  private const int kMaxMeshPoolCount = 100;

#if UNITY_EDITOR
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
  private static void EditorStaticReset()
  {
    _isExitingPlayMode = false;
    ClearMeshes();

    UnityEditor.EditorApplication.playModeStateChanged -= OnEditorPlayStateChange;
    UnityEditor.EditorApplication.playModeStateChanged += OnEditorPlayStateChange;
  }

  private static void OnEditorPlayStateChange(UnityEditor.PlayModeStateChange stateChange)
  {
    if (stateChange == UnityEditor.PlayModeStateChange.ExitingPlayMode)
    {
      _isExitingPlayMode = true;
      ClearMeshes();
    }

    if (stateChange == UnityEditor.PlayModeStateChange.EnteredEditMode)
    {
      _isExitingPlayMode = false;
    }
  }
#endif

  // Get a new empty mesh from the pool
  // When done with the mesh, use FreeMesh() to return it to the pool
  public static Mesh GetMesh()
  {
#if UNITY_EDITOR
    if (!Application.isPlaying)
    {
      return new Mesh();
    }
#endif

    if (--_statsCounter <= 0)
    {
      _statsCounter = kDebugStatsInterval;
      Debug.Log($"MeshPool: {_meshPool.Count} pooled");
      Debug.Log($"MeshPool: {_meshActiveCount} active");
    }

    ++_meshActiveCount;
    if (_meshPool.Count > 0)
    {
      Mesh mesh = _meshPool[_meshPool.Count - 1];
      _meshPool.RemoveAt(_meshPool.Count - 1);
      return mesh;
    }

    return new Mesh();
  }

  // Free a mesh that was previously acquired from the pool
  // Any mesh data will be cleared
  public static void FreeMesh(Mesh mesh)
  {
#if UNITY_EDITOR
    if (!Application.isPlaying)
    {
      Mesh.DestroyImmediate(mesh);
      return;
    }
#endif

    mesh.Clear();
    mesh.UploadMeshData(markNoLongerReadable: false);
    --_meshActiveCount;

#if UNITY_EDITOR
    if (_isExitingPlayMode)
    {
      Mesh.DestroyImmediate(mesh);
      return;
    }
#endif

    if (_meshPool.Count < kMaxMeshPoolCount)
    {
      mesh.name = "mesh-pooled";
      _meshPool.Add(mesh);
    }
    else
    {
      Debug.Log($"MeshPool: Max pool size reached, destroying freed mesh");
      Mesh.Destroy(mesh);
    }
  }

  private static void ClearMeshes()
  {
    _meshActiveCount = 0;
    _statsCounter = 0;

    foreach (var mesh in _meshPool)
    {
      Mesh.DestroyImmediate(mesh);
    }

    _meshPool.Clear();
  }
}