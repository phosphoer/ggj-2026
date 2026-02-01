/// <summary>
/// Fixed-size circular buffer for storing ECG signal samples.
/// Optimized for zero GC allocations during runtime updates.
/// </summary>
public class ECGSignalBuffer
{
  private float[] _samples;
  private int _writeHead;
  private int _capacity;

  /// <summary>
  /// Gets the current write head position in the buffer
  /// </summary>
  public int WriteHead => _writeHead;

  /// <summary>
  /// Gets the capacity of the buffer
  /// </summary>
  public int Capacity => _capacity;

  /// <summary>
  /// Initializes the circular buffer with the specified capacity
  /// </summary>
  /// <param name="capacity">Number of samples to store</param>
  public ECGSignalBuffer(int capacity)
  {
    _capacity = capacity;
    _samples = new float[capacity];
    _writeHead = 0;

    // Initialize with baseline (zero)
    for (int i = 0; i < capacity; i++)
    {
      _samples[i] = 0f;
    }
  }

  /// <summary>
  /// Writes a new sample to the buffer and advances the write head
  /// </summary>
  /// <param name="value">Signal value to write</param>
  public void WriteSample(float value)
  {
    _samples[_writeHead] = value;
    _writeHead = (_writeHead + 1) % _capacity;
  }

  /// <summary>
  /// Reads a sample from the buffer at the specified index
  /// </summary>
  /// <param name="index">Index to read from (will be wrapped to buffer capacity)</param>
  /// <returns>Signal value at the index</returns>
  public float ReadSample(int index)
  {
    // Ensure index is within bounds using modulo
    int wrappedIndex = index % _capacity;
    if (wrappedIndex < 0)
      wrappedIndex += _capacity;

    return _samples[wrappedIndex];
  }

  /// <summary>
  /// Clears the buffer and resets all samples to zero
  /// </summary>
  public void Clear()
  {
    for (int i = 0; i < _capacity; i++)
    {
      _samples[i] = 0f;
    }
    _writeHead = 0;
  }
}
