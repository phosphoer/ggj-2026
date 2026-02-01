using UnityEngine;

/// <summary>
/// Displays a procedural ECG heart rate monitor using a LineRenderer.
/// Generates realistic QRS complex waveforms with variable heart rates.
/// </summary>
public class HeartRateMonitorDisplay : MonoBehaviour
{
  [Header("Heart Rate Settings")]
  [SerializeField] private float _heartRateBPM = 75f;
  [SerializeField] private float _bpmSmoothingSpeed = 0.5f;
  [SerializeField] private float _minHeartRateBPM = 60f;
  [SerializeField] private float _maxHeartRateBPM = 180f;

  [Header("Display Settings")]
  [SerializeField] private int _bufferSize = 512;
  [SerializeField] private float _displayWidth = 10f;
  [SerializeField] private float _displayHeight = 2f;

  [Header("Line Settings")]
  [SerializeField] private Color _lineColor = Color.green;
  [SerializeField] private float _lineWidth = 0.05f;
  [SerializeField] private Material _lineMaterial = null;

  private GradientColorKey[] _colorKeys;
  static GradientColorKey[] defaultGradientKeys = new GradientColorKey[]
    {
        new GradientColorKey(Color.red, 0f),
        new GradientColorKey(Color.yellow, 0.5f),
        new GradientColorKey(Color.green, 1f)
    };

  private LineRenderer _lineRenderer;
  private ECGSignalGenerator _signalGenerator;
  private ECGSignalBuffer _signalBuffer;
  private float _currentBPM;
  private float _targetBPM;
  private bool _isMonitoring = true;
  private FarmerController farmer = null;
  private Gradient _gradient = null;

  private void Awake()
  {
    _gradient = new Gradient();
    _gradient.colorKeys = defaultGradientKeys;

    // Initialize signal generator and buffer
    _signalGenerator = new ECGSignalGenerator();
    _signalBuffer = new ECGSignalBuffer(_bufferSize);

    // Initialize BPM values
    _currentBPM = _heartRateBPM;
    _targetBPM = _heartRateBPM;

    // Set up LineRenderer component
    _lineRenderer = GetComponent<LineRenderer>();
    if (_lineRenderer == null)
    {
      _lineRenderer = gameObject.AddComponent<LineRenderer>();
    }

    ConfigureLineRenderer();
  }

  private void ConfigureLineRenderer()
  {
    // Set position count to match buffer size
    _lineRenderer.positionCount = _bufferSize;

    // Configure line appearance
    SetLineColor(_lineColor);
    _lineRenderer.startWidth = _lineWidth;
    _lineRenderer.endWidth = _lineWidth;

    // Use world space positioning
    _lineRenderer.useWorldSpace = false;

    // Set material if provided
    if (_lineMaterial != null)
    {
      _lineRenderer.material = _lineMaterial;
    }

    // Disable shadows and light interaction
    _lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    _lineRenderer.receiveShadows = false;
  }

  private void SetLineColor(Color newColor)
  {
    _lineColor = newColor;
    _lineRenderer.startColor = _lineColor;
    _lineRenderer.endColor = _lineColor;
  }

  private void LateUpdate()
  {
    if (!_isMonitoring)
      return;

    float dt = Time.deltaTime;

    float newTargetBPM = _targetBPM;
    if (farmer != null)
    {
      float healthFraction = farmer.health / farmer.maxHealth;
      float newHR = Mathf.Lerp(_maxHeartRateBPM, _minHeartRateBPM, healthFraction);
      Color newColor = _gradient.Evaluate(healthFraction); ;

      SetHeartRate(newHR);
      SetLineColor(newColor);
    }

    // Smoothly transition BPM using frame-independent damping
    _currentBPM = Mathfx.Damp(_currentBPM, _targetBPM, _bpmSmoothingSpeed, dt);

    // Update signal generation
    _signalGenerator.Update(dt, _currentBPM);

    // Write new sample to buffer
    float newSample = _signalGenerator.GetCurrentSample();
    _signalBuffer.WriteSample(newSample);

    // Update LineRenderer positions
    UpdateLineRendererPositions();
  }

  private void UpdateLineRendererPositions()
  {
    int bufferWriteHead = _signalBuffer.WriteHead;
    int bufferCapacity = _signalBuffer.Capacity;

    for (int i = 0; i < _lineRenderer.positionCount; i++)
    {
      // Calculate buffer index (oldest samples on the left, newest on the right)
      int bufferIndex = (bufferWriteHead - _lineRenderer.positionCount + i + bufferCapacity) % bufferCapacity;

      // Calculate position
      float x = ((float)i / _lineRenderer.positionCount) * _displayWidth;
      float y = _signalBuffer.ReadSample(bufferIndex) * _displayHeight;

      // Set LineRenderer position
      _lineRenderer.SetPosition(i, new Vector3(x, y, 0f));
    }
  }

  /// <summary>
  /// Sets the target heart rate in beats per minute
  /// </summary>
  /// <param name="bpm">Beats per minute</param>
  public void SetHeartRate(float bpm)
  {
    _targetBPM = Mathf.Max(bpm, 1f);
  }

  public void SetTempHeartRate(float bpm)
  {
    _currentBPM = bpm;
  }

  /// <summary>
  /// Sets the heart rate as a multiplier of the base rate
  /// </summary>
  /// <param name="multiplier">Multiplier (1.0 = normal, 1.5 = 1.5x faster)</param>
  public void SetBPMMultiplier(float multiplier)
  {
    _targetBPM = _heartRateBPM * multiplier;
  }

  /// <summary>
  /// Starts the heart rate monitoring display
  /// </summary>
  public void StartMonitoring()
  {
    farmer = GameController.Instance.Farmer;

    _isMonitoring = true;
  }

  /// <summary>
  /// Stops the heart rate monitoring display
  /// </summary>
  public void StopMonitoring()
  {
    _isMonitoring = false;
  }

  /// <summary>
  /// Gets whether the monitor is currently active
  /// </summary>
  public bool IsMonitoring => _isMonitoring;

  /// <summary>
  /// Gets the current heart rate in BPM
  /// </summary>
  public float CurrentBPM => _currentBPM;

  private void OnValidate()
  {
    // Update target BPM when edited in inspector
    _targetBPM = _heartRateBPM;
  }
}
