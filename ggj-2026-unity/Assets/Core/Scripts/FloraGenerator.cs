using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class FloraGenerator : MonoBehaviour, ILODContent
{
  public LODRange[] LODRanges => _lodRanges;
  public int CurrentLODLevel { get; private set; } = -1;

  [Header("Spawn")]
  public Mesh[] FloraMeshes = null;
  public Material FloraMaterial = null;
  public int Seed = -1;
  public float SpawnRadius = 30;
  public int SpawnCount = 30;
  public Transform SpawnTransform;
  public float SpawnMaxSlopeAngle = 90;

  [Header("Transform")]
  [Range(0, 1)] public float RotationNormalInfluence = 0.25f;
  [Range(0, 1)] public float RotationRandomInfluence = 0.0f;
  public RangedFloat ScaleRange = new RangedFloat(0.75f, 1.25f);
  public float HeightOffset = -1;
  public bool GenerateCollision = false;

  [Header("Vertex Colors")]
  public bool EnableInstanceVColors = false;
  public Gradient InstanceVColorGradient;

  public bool EnableGroundVColors = false;
  public Gradient GroundVColorGradient;
  public float GroundVColorDistance = 10;
  [Range(0, 1)] public float GroundVColorBlend;

  [Header("Raycast")]
  public float MaxRaycastDistance = 100;
  public LayerMask RaycastMask = default;

  [Header("Skirt")]
  public bool GenerateSkirt = false;
  public Material SkirtMaterial;
  public int SkirtLoopResolution = 4;
  public float SkirtHeight = 5;
  public float SkirtOuterRadius = 1;
  public float SkirtEdgeOpacity = 0.5f;
  public float SkirtInnerOpacity = 0.75f;
  public AnimationCurve SkirtOpacityCurve = null;

  public List<FloraGenerator> FloraDependencies;

  private Mesh _mesh;
  private Mesh _skirtMesh;
  private GameObject _skirtGameObject;
  private Transform _helperTransform;
  private Vector3 _lastGenPosition;
  private Vector3 _lastGenScale;
  private Quaternion _lastGenRotation;
  private bool _isDirty = false;
  private bool _isGenerated = false;
  private MeshRenderer _renderer;
  private List<CombineInstance> _combineInstances = new();
  private List<CombineInstance> _skirtCombineInstances = new();
  private List<Vector3> _groundPositions = new();
  private List<Vector3> _skirtHullPoints = new();
  private List<Vector3> _skirtHullNormals = new();
  private List<Vector3> _skirtVertices = new();
  private List<Color> _skirtVertColors = new();
  private List<int> _skirtIndices = new();

  private static readonly LODRange[] _lodRanges =
  {
    new LODRange() { Min = 0, Max = 2000 },
    new LODRange() { Min = 2100, Max = Mathf.Infinity },
  };

  public float GetLODDistance(Vector3 fromPos)
  {
    Transform spawnTransform = SpawnTransform != null ? SpawnTransform : transform;
    Vector3 fromPosLocal = spawnTransform.InverseTransformPoint(fromPos);
    Vector3 pointOnRadiusLocal = fromPosLocal.normalized * SpawnRadius;
    Vector3 pointOnRadiusWorld = spawnTransform.TransformPoint(pointOnRadiusLocal);
    return Vector3.Distance(pointOnRadiusWorld, fromPos);
  }

  public void SetLOD(int lodLevel)
  {
    CurrentLODLevel = lodLevel;
    if (lodLevel == 0)
      Generate();
    else
      Clear();
  }

  public void Generate()
  {
    Regenerate();
  }

  public void Clear()
  {
    if (_mesh != null)
    {
      MeshPool.FreeMesh(_mesh);
      _renderer.enabled = false;
      _mesh = null;
    }
  }

  private void Awake()
  {
    _isGenerated = false;
  }

  private void OnDestroy()
  {
    Clear();
  }

  private void OnEnable()
  {
    if (Application.isPlaying)
    {
      LODContentManager.AddLODContent(this);
    }
  }

  private void OnDisable()
  {
    if (Application.isPlaying)
    {
      LODContentManager.RemoveLODContent(this);
    }
  }

#if UNITY_EDITOR
  private void Update()
  {
    if (!Application.isPlaying)
    {
      Transform spawnTransform = SpawnTransform != null ? SpawnTransform : transform;

      _isDirty |= _lastGenPosition != spawnTransform.position;
      _isDirty |= _lastGenScale != spawnTransform.localScale;
      _isDirty |= _lastGenRotation != spawnTransform.rotation;

      if (_isDirty)
        Regenerate();
    }
  }

  private void OnValidate()
  {
    if (!Application.isPlaying)
    {
      _isDirty = true;
    }
  }

  private void OnDrawGizmos()
  {
    Transform spawnTransform = SpawnTransform != null ? SpawnTransform : transform;

    Gizmos.color = Color.green;
    GizmosEx.DrawCircle(spawnTransform.localToWorldMatrix, SpawnRadius);

    Gizmos.color = Color.white;
    for (int i = 0; i < _skirtHullPoints.Count - 1; ++i)
    {
      Gizmos.DrawLine(_skirtHullPoints[i], _skirtHullPoints[i + 1]);
    }

    for (int i = 0; i < _skirtHullNormals.Count; ++i)
    {
      Gizmos.DrawRay(_skirtHullPoints[i], _skirtHullNormals[i]);
    }
  }
#endif

  private void Regenerate()
  {
    _isDirty = false;
    _isGenerated = true;
    if (FloraMeshes == null || FloraMeshes.Length == 0)
      return;

    for (int i = 0; i < FloraDependencies.Count; ++i)
    {
      if (!FloraDependencies[i])
        FloraDependencies.RemoveAt(i--);
    }

    foreach (var dependency in FloraDependencies)
    {
      if (dependency != this && dependency && !dependency._isGenerated)
      {
        dependency.Regenerate();
      }
    }

    Transform spawnTransform = SpawnTransform != null ? SpawnTransform : transform;
    _lastGenPosition = spawnTransform.position;
    _lastGenScale = spawnTransform.localScale;
    _lastGenRotation = spawnTransform.rotation;

    if (_mesh == null)
      _mesh = MeshPool.GetMesh();

    _mesh.Clear();
    _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

    _helperTransform = new GameObject("flora-transform-helper").transform;
    _helperTransform.parent = transform;
    _helperTransform.gameObject.hideFlags = HideFlags.DontSave;

    System.Random rand = new System.Random(Seed);

    _combineInstances.Clear();
    _skirtCombineInstances.Clear();

    if (!Application.isPlaying)
      FloraDependencies.Clear();

    for (int i = 0; i < SpawnCount; ++i)
    {
      Vector2 randCirclePos = rand.NextPointInsideCircle();
      Vector2 point = randCirclePos * SpawnRadius;
      Vector3 pos = spawnTransform.TransformPoint(point.OnXZPlane());

      Vector3 raycastOrigin = pos;
      if (Mathfx.RaycastIgnoringTransform(raycastOrigin + spawnTransform.up * SpawnRadius, -spawnTransform.up, out RaycastHit hitInfo, MaxRaycastDistance + SpawnRadius, RaycastMask, ignoreTransform: transform))
      {
        pos = hitInfo.point + Vector3.up * HeightOffset;
        Vector3 normal = hitInfo.normal;

        float slopeAngle = Vector3.Angle(normal, Vector3.up);
        if (slopeAngle <= SpawnMaxSlopeAngle)
        {
          if (!Application.isPlaying)
          {
            FloraGenerator hitFlora = hitInfo.transform.GetComponent<FloraGenerator>();
            if (hitFlora && hitFlora.gameObject.scene == gameObject.scene && !FloraDependencies.Contains(hitFlora))
            {
              FloraDependencies.Add(hitFlora);
            }
          }

          Quaternion targetRot = Quaternion.LookRotation(normal) * Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(transform.rotation);
          _helperTransform.position = transform.InverseTransformPoint(pos);
          _helperTransform.rotation = Quaternion.Slerp(Quaternion.identity, targetRot, RotationNormalInfluence);
          _helperTransform.Rotate(0, rand.NextFloatRanged(0, 360), 0, Space.Self);
          _helperTransform.rotation = Quaternion.Slerp(_helperTransform.rotation, rand.NextRotation(), RotationRandomInfluence);
          _helperTransform.localScale = ScaleRange.SeededRandom(rand) * Vector3.one;

          Mesh floraMesh = FloraMeshes[rand.NextIntRanged(0, FloraMeshes.Length)];
          _combineInstances.Add(new CombineInstance()
          {
            transform = _helperTransform.localToWorldMatrix,
            mesh = floraMesh
          });
        }
      }
      else
      {
        // Debug.DrawRay(raycastOrigin + spawnTransform.up * SpawnRadius, -spawnTransform.up * MaxRaycastDistance, Color.red, 10);
      }
    }

    _mesh.CombineMeshes(_combineInstances.ToArray(), true, true, false);
    _mesh.RecalculateBounds();

    List<Color> vertColors = new();
    List<Vector3> verts = new();
    _mesh.GetColors(vertColors);
    _mesh.GetVertices(verts);

    Vector3 meshSizeWorld = transform.TransformPoint(_mesh.bounds.size);
    Vector3 meshMaxWorld = transform.TransformPoint(_mesh.bounds.max);

    for (int i = vertColors.Count; i < _mesh.vertexCount; ++i)
      vertColors.Add(Color.white);

    int offset = 0;
    foreach (var instance in _combineInstances)
    {
      _groundPositions.Clear();

      Vector3 meshCenter = Vector3.zero;
      int vertexCount = instance.mesh.vertexCount;
      float instanceId = rand.NextFloat();
      Color instanceColor = EnableInstanceVColors ? InstanceVColorGradient.Evaluate(rand.NextFloat() * 1) : Color.white;
      for (int i = 0; i < vertexCount; ++i)
      {
        if (EnableInstanceVColors)
        {
          vertColors[i + offset] = instanceColor.linear;
        }

        if (EnableGroundVColors)
        {
          Vector3 vertex = verts[i + offset];
          Vector3 vertexWorldPos = transform.TransformPoint(vertex);
          meshCenter += vertexWorldPos;
          Vector3 rayStart = vertexWorldPos + spawnTransform.up * meshSizeWorld.y;

          RaycastHit terrainHit;
          if (Mathfx.RaycastIgnoringTransform(rayStart, -spawnTransform.up, out terrainHit, meshSizeWorld.y * 1.25f, RaycastMask, ignoreTransform: transform))
          {
            float distToGround = Mathf.Abs(terrainHit.point.y - vertexWorldPos.y);
            float groundColorT = 1 - Mathf.Clamp01(distToGround / GroundVColorDistance);
            Color groundColor = vertColors[i + offset] * GroundVColorGradient.Evaluate(groundColorT);
            vertColors[i + offset] = Color.Lerp(vertColors[i + offset], groundColor, GroundVColorBlend);
            _groundPositions.Add(terrainHit.point);
          }
        }
        else
        {
          vertColors[i + offset] = vertColors[i + offset].WithA(instanceId);
        }
      }

      meshCenter /= vertexCount;

      if (GenerateSkirt)
      {
        float skirtLoopHeight = SkirtHeight / SkirtLoopResolution;

        CombineInstance skirtCombineInstance = default;
        skirtCombineInstance.mesh = MeshPool.GetMesh();

        _skirtVertices.Clear();
        _skirtVertColors.Clear();
        _skirtIndices.Clear();

        Mathfx.ConvexHull(_groundPositions, _skirtHullPoints, 256);
        if (_skirtHullPoints.Count > 0)
        {
          // Calculate hull normals
          _skirtHullNormals.Clear();
          for (int i = 0; i < _skirtHullPoints.Count; ++i)
          {
            Vector3 hullPoint = _skirtHullPoints[i];
            Vector3 prevHullPoint = _skirtHullPoints[Mathfx.Wrap(i - 1, 0, _skirtHullPoints.Count - 1)];
            Vector3 hullNormal = Vector3.Cross(prevHullPoint - hullPoint, Vector3.up);
            _skirtHullNormals.Add(hullNormal.normalized);
          }

          int loopVertCount = _skirtHullPoints.Count;
          const int kSkirtLoopCount = 3;
          for (int heightIndex = 0; heightIndex < kSkirtLoopCount; ++heightIndex)
          {
            float heightT = heightIndex / (float)(kSkirtLoopCount - 1);
            for (int i = 0; i < loopVertCount; ++i)
            {
              Vector3 skirtNormal = _skirtHullNormals[i];
              Vector3 skirtVert = _skirtHullPoints[i];

              float skirtOpacity = 0;
              if (heightIndex == 0)
              {
                skirtVert += skirtNormal * SkirtOuterRadius;
              }
              else if (heightIndex == 1)
              {
                skirtVert = _skirtHullPoints[i];
                skirtOpacity = SkirtEdgeOpacity;
              }
              else if (heightIndex == 2)
              {
                skirtVert = meshCenter;
                skirtOpacity = SkirtInnerOpacity;
              }


              if (RaycastManager.Instance)
                skirtVert = RaycastManager.Instance.SnapPosToRaycast(skirtVert, Vector3.up, RaycastMask) + Vector3.up * 0.15f;

              _skirtVertices.Add(skirtVert);
              _skirtVertColors.Add(Color.black.WithA(skirtOpacity));
            }
          }

          for (int heightIndex = 0; heightIndex < kSkirtLoopCount - 1; ++heightIndex)
          {
            int loopStartH0 = loopVertCount * heightIndex;
            int loopStartH1 = loopVertCount * (heightIndex + 1);
            int loopEndH0 = loopStartH1 - 1;
            int loopEndH1 = loopStartH1 + loopVertCount - 1;
            for (int i = 0; i < loopVertCount; ++i)
            {
              int startIndex = i + heightIndex * loopVertCount;
              _skirtIndices.Add(startIndex + 0);
              _skirtIndices.Add(startIndex + loopVertCount);
              _skirtIndices.Add(Mathfx.Wrap(startIndex + 1, loopStartH0, loopEndH0));
              _skirtIndices.Add(Mathfx.Wrap(startIndex + 1, loopStartH0, loopEndH0));
              _skirtIndices.Add(startIndex + loopVertCount);
              _skirtIndices.Add(Mathfx.Wrap(startIndex + loopVertCount + 1, loopStartH1, loopEndH1));
            }
          }
        }

        skirtCombineInstance.mesh.SetVertices(_skirtVertices);
        skirtCombineInstance.mesh.SetIndices(_skirtIndices, MeshTopology.Triangles, 0);
        skirtCombineInstance.mesh.SetColors(_skirtVertColors);

        skirtCombineInstance.transform = transform.worldToLocalMatrix;
        _skirtCombineInstances.Add(skirtCombineInstance);
      }

      offset += vertexCount;
    }

    _mesh.SetColors(vertColors);
    _mesh.SetVertices(verts);
    _mesh.name = "flora-generated";
    _mesh.hideFlags = HideFlags.DontSave;

    MeshFilter meshFilter = gameObject.GetOrAddComponent<MeshFilter>();
    meshFilter.sharedMesh = _mesh;
    meshFilter.hideFlags = HideFlags.DontSave;

    _renderer = gameObject.GetOrAddComponent<MeshRenderer>();
    _renderer.sharedMaterial = FloraMaterial;
    _renderer.enabled = true;
    _renderer.hideFlags = HideFlags.DontSave;

    if (GenerateSkirt)
    {
      _skirtMesh = MeshPool.GetMesh();
      _skirtMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
      _skirtMesh.hideFlags = HideFlags.DontSave;
      _skirtMesh.CombineMeshes(_skirtCombineInstances.ToArray(), true, true);
      _skirtMesh.RecalculateBounds();
      _skirtMesh.RecalculateNormals();
      _skirtMesh.Optimize();
      _skirtMesh.UploadMeshData(markNoLongerReadable: true);

      foreach (var instance in _skirtCombineInstances)
        MeshPool.FreeMesh(instance.mesh);

      if (!_skirtGameObject)
      {
        _skirtGameObject = new GameObject($"{name}-skirt");
        _skirtGameObject.hideFlags = HideFlags.DontSave;
        _skirtGameObject.transform.SetParent(transform, worldPositionStays: false);
        _skirtGameObject.transform.SetIdentityTransformLocal();
      }

      MeshFilter skirtMeshFilter = _skirtGameObject.GetOrAddComponent<MeshFilter>();
      skirtMeshFilter.sharedMesh = _skirtMesh;
      skirtMeshFilter.hideFlags = HideFlags.DontSave;

      MeshRenderer skirtRenderer = _skirtGameObject.GetOrAddComponent<MeshRenderer>();
      skirtRenderer.sharedMaterial = SkirtMaterial;
      skirtRenderer.hideFlags = HideFlags.DontSave;
    }

    if (GenerateCollision)
    {
      MeshCollider collider = gameObject.GetOrAddComponent<MeshCollider>();
      collider.sharedMesh = _mesh;
      collider.hideFlags = HideFlags.DontSave;
    }

#if UNITY_EDITOR
    if (!Application.isPlaying)
      UnityEditor.EditorUtility.SetDirty(this);
#endif

    DestroyImmediate(_helperTransform.gameObject);
  }
}