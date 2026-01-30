using UnityEngine;

public class MainCamera : Singleton<MainCamera>
{
  public static event System.Action Initialized;
  public static event System.Action Uninitialized;

  public CameraControllerStack CameraStack => _cameraControllerStack;
  public Camera Camera => _camera;
  public Transform CachedTransform => _cachedTransform;
  public SimplePostFX PostFX => _postFx;

  [SerializeField] private CameraControllerStack _cameraControllerStack = null;
  [SerializeField] private Camera _camera = null;
  [SerializeField] private SimplePostFX _postFx = null;
  [SerializeField] private bool _writeDepth = true;

  private Transform _cachedTransform = null;

  // Reset static state for editor without domain reload
#if UNITY_EDITOR
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
  private static void EditorInit()
  {
    Initialized = null;
    Uninitialized = null;
  }
#endif

  private void Awake()
  {
    Instance = this;
    _cachedTransform = transform;

    if (_writeDepth)
      _camera.depthTextureMode |= DepthTextureMode.Depth;

    Initialized?.Invoke();
  }

  private void OnDestroy()
  {
    _cachedTransform = null;
    Uninitialized?.Invoke();
  }
}