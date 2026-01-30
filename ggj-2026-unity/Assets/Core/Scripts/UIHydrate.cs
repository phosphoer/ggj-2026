using UnityEngine;

public class UIHydrate : MonoBehaviour
{
  public event System.Action Hydrated;
  public event System.Action Dehydrated;

  public bool IsAnimating => _isAnimating;
  public bool IsHydrated => _isHydrated;

  public bool HydrateOnEnable = false;
  public bool StartDehydrated = false;
  public bool DestroyOnDehydrate = false;

  [SerializeField] private Transform _targetTransform = null;

  [SerializeField] private bool _useNormalizedScale = true;

  [SerializeField] private bool _enableRandomDelay = false;

  private Vector3 _startScale;
  private bool _isHydrated;
  private bool _isDehydrated;
  private bool _isAnimating;
  private float _delayTimer;
  private float _scaleVelocity;
  private float _currentScale;
  private System.Action _finishCallback;

  public void SetFullScale()
  {
    EnsureTargetTransform();

    _targetTransform.localScale = _startScale;
  }

  public void SetZeroScale()
  {
    EnsureTargetTransform();

    _targetTransform.localScale = Vector3.zero;
  }

  [ContextMenu("Hydrate")]
  public void HydrateIfNecesssary(System.Action finishCallback = null)
  {
    if (!_isHydrated)
      Hydrate(finishCallback);
    else
      finishCallback?.Invoke();
  }

  [ContextMenu("Dehydrate")]
  public void DehydrateIfNecessary(System.Action finishCallback = null)
  {
    if (_isHydrated)
      Dehydrate(finishCallback);
    else
      finishCallback?.Invoke();
  }

  public void Hydrate(System.Action finishCallback = null)
  {
    EnsureTargetTransform();

    _isHydrated = true;
    _isDehydrated = false;
    _isAnimating = true;
    _finishCallback = finishCallback;
    _delayTimer = _enableRandomDelay ? Random.Range(0f, 0.25f) : 0;
    enabled = true;
    gameObject.SetActive(true);
    _targetTransform.localScale = Vector3.zero;
    _currentScale = 0;
  }

  public void Dehydrate(System.Action finishCallback = null)
  {
    EnsureTargetTransform();

    _isHydrated = false;
    _isDehydrated = true;
    _isAnimating = true;
    _finishCallback = finishCallback;
    _delayTimer = _enableRandomDelay ? Random.Range(0f, 0.25f) : 0;
    enabled = true;
    _currentScale = 1;
    _scaleVelocity = 10;
    gameObject.SetActive(true);
  }

  public void SnapToHydrated()
  {
    EnsureTargetTransform();
    _isHydrated = true;
    _isDehydrated = false;
    _isAnimating = false;
    _currentScale = 1;
    _scaleVelocity = 0;
    _targetTransform.localScale = Vector3.one * _currentScale;
    gameObject.SetActive(true);
  }

  public void SnapToDehydrated()
  {
    EnsureTargetTransform();
    _isHydrated = false;
    _isDehydrated = true;
    _isAnimating = false;
    _currentScale = 0;
    _scaleVelocity = 0;
    _targetTransform.localScale = Vector3.one * _currentScale;
    gameObject.SetActive(false);
  }

  private void Awake()
  {
    EnsureTargetTransform();

    _startScale = _useNormalizedScale ? Vector3.one : _targetTransform.localScale;

    if (StartDehydrated)
    {
      _targetTransform.localScale = Vector3.zero;
      _isHydrated = false;
      _currentScale = 0;
      enabled = false;
      gameObject.SetActive(false);
    }
    else
    {
      _isHydrated = true;
    }
  }

  private void OnEnable()
  {
    if (HydrateOnEnable && !_isDehydrated)
    {
      Hydrate();
    }
  }

  private void Update()
  {
    float targetScale = _isHydrated ? 1 : 0;
    float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);

    if (_isAnimating)
    {
      _delayTimer -= dt;
      if (_delayTimer <= 0)
      {
        float scaleDelta = targetScale - _currentScale;
        _scaleVelocity += dt * scaleDelta * 200;
        _currentScale += _scaleVelocity * dt;
        _scaleVelocity = Mathfx.Damp(_scaleVelocity, 0, 0.25f, dt * 10);

        if (Mathf.Abs(_scaleVelocity) < 0.01f && Mathf.Abs(_currentScale - targetScale) < 0.01f)
          _currentScale = targetScale;

        if (_isDehydrated && _currentScale <= 0)
        {
          _currentScale = targetScale;
          _scaleVelocity = 0;
        }

        transform.localScale = _startScale * _currentScale;
      }
    }

    bool isDone = _currentScale == targetScale;
    if (isDone)
    {
      _isAnimating = false;
      enabled = false;

      if (_isHydrated)
      {
        Hydrated?.Invoke();
        _finishCallback?.Invoke();
      }
      else
      {
        Dehydrated?.Invoke();
        _finishCallback?.Invoke();
        gameObject.SetActive(false);

        if (DestroyOnDehydrate)
        {
          Destroy(gameObject);
        }
      }
    }
  }

  private void EnsureTargetTransform()
  {
    if (!_targetTransform)
      _targetTransform = transform;
  }
}