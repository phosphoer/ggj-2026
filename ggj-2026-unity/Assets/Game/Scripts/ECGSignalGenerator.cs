using UnityEngine;

/// <summary>
/// Generates procedural ECG (electrocardiogram) signal data with realistic QRS complex waveforms.
/// Pure data generation with no Unity dependencies.
/// </summary>
public class ECGSignalGenerator
{
  private float _currentPhase = 0f;
  private float _currentSample = 0f;

  /// <summary>
  /// Updates the signal generator and advances the phase based on the current BPM
  /// </summary>
  /// <param name="deltaTime">Time since last update in seconds</param>
  /// <param name="bpm">Beats per minute (heart rate)</param>
  public void Update(float deltaTime, float bpm)
  {
    // Calculate beat duration in seconds
    float beatDuration = 60f / Mathf.Max(bpm, 1f);

    // Advance phase (0 to 1 represents one complete heartbeat)
    _currentPhase += deltaTime / beatDuration;
    if (_currentPhase >= 1f)
      _currentPhase -= 1f;

    // Generate the current sample based on phase
    _currentSample = GenerateSample(_currentPhase);
  }

  /// <summary>
  /// Gets the current signal sample value
  /// </summary>
  /// <returns>Normalized signal value [-1, 1]</returns>
  public float GetCurrentSample()
  {
    return _currentSample;
  }

  /// <summary>
  /// Generates a single ECG sample based on the current phase in the heartbeat cycle
  /// </summary>
  /// <param name="phase">Phase value [0, 1] where 0 is the start of a heartbeat</param>
  /// <returns>Signal amplitude [-1, 1]</returns>
  private float GenerateSample(float phase)
  {
    // P wave: 0-15% (small bump before QRS)
    if (phase < 0.15f)
    {
      float t = phase / 0.15f;
      return 0.15f * Gaussian(t, 0.5f, 0.3f);
    }
    // PR segment: 15-25% (baseline)
    else if (phase < 0.25f)
    {
      return 0f;
    }
    // QRS complex: 25-35% (sharp spike)
    else if (phase < 0.35f)
    {
      float t = (phase - 0.25f) / 0.1f;
      return QRSComplex(t);
    }
    // ST segment: 35-45% (baseline)
    else if (phase < 0.45f)
    {
      return 0f;
    }
    // T wave: 45-65% (smooth bump after QRS)
    else if (phase < 0.65f)
    {
      float t = (phase - 0.45f) / 0.2f;
      return 0.25f * Gaussian(t, 0.5f, 0.4f);
    }
    // Rest: 65-100% (baseline)
    else
    {
      return 0f;
    }
  }

  /// <summary>
  /// Generates the QRS complex waveform (Q dip, R spike, S dip)
  /// </summary>
  /// <param name="t">Normalized time [0, 1] within the QRS duration</param>
  /// <returns>Signal amplitude</returns>
  private float QRSComplex(float t)
  {
    // Q wave: small negative dip (0-30%)
    if (t < 0.3f)
    {
      float qt = t / 0.3f;
      return -0.15f * Mathf.Sin(qt * Mathf.PI);
    }
    // R wave: sharp positive spike (30-60%)
    else if (t < 0.6f)
    {
      float rt = (t - 0.3f) / 0.3f;
      // Sharp spike using power function
      return Mathf.Pow(Mathf.Sin(rt * Mathf.PI), 2f);
    }
    // S wave: small negative dip (60-100%)
    else
    {
      float st = (t - 0.6f) / 0.4f;
      return -0.2f * Mathf.Sin(st * Mathf.PI);
    }
  }

  /// <summary>
  /// Gaussian function for smooth wave envelopes (P and T waves)
  /// </summary>
  /// <param name="x">Input value [0, 1]</param>
  /// <param name="center">Center of the gaussian peak [0, 1]</param>
  /// <param name="width">Width/spread of the gaussian</param>
  /// <returns>Gaussian value [0, 1]</returns>
  private float Gaussian(float x, float center, float width)
  {
    float offset = (x - center) / width;
    return Mathf.Exp(-offset * offset);
  }
}
