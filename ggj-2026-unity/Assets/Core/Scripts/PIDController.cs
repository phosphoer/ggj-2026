using UnityEngine;

[System.Serializable]
public class PIDController
{
  public enum DerivativeMeasurementType
  {
    Velocity,
    ErrorRateOfChange
  }

  public float ProportionalGain = 1;
  public float IntegralGain = 1;
  public float DerivativeGain = 1;

  public RangedFloat OutputRange = new RangedFloat(-1, 1);
  public float IntegralSaturation = 1;

  public DerivativeMeasurementType DerivativeMeasurement;

  private float _valueLast;
  private float _errorLast;
  private float _integrationStored;
  private float _velocity; //only used for the info display
  private bool _derivativeInitialized;

  public PIDController(float p = 1, float i = 1, float d = 1)
  {
    ProportionalGain = p;
    IntegralGain = i;
    DerivativeGain = d;
  }

  public void Reset()
  {
    _derivativeInitialized = false;
    _valueLast = 0;
    _errorLast = 0;
    _integrationStored = 0;
    _velocity = 0;
  }

  public float Update(float dt, float currentValue, float targetValue)
  {
    if (dt == 0)
      return 0;

    float error = targetValue - currentValue;

    //calculate P term
    float P = ProportionalGain * error;

    //calculate I term
    _integrationStored = Mathf.Clamp(_integrationStored + (error * dt), -IntegralSaturation, IntegralSaturation);
    float I = IntegralGain * _integrationStored;

    //calculate both D terms
    float errorRateOfChange = (error - _errorLast) / dt;
    _errorLast = error;

    float valueRateOfChange = (currentValue - _valueLast) / dt;
    _valueLast = currentValue;
    _velocity = valueRateOfChange;

    //choose D term to use
    float deriveMeasure = 0;

    if (_derivativeInitialized)
    {
      if (DerivativeMeasurement == DerivativeMeasurementType.Velocity)
      {
        deriveMeasure = -valueRateOfChange;
      }
      else
      {
        deriveMeasure = errorRateOfChange;
      }
    }
    else
    {
      _derivativeInitialized = true;
    }

    float D = DerivativeGain * deriveMeasure;

    float result = P + I + D;

    return Mathf.Clamp(result, OutputRange.MinValue, OutputRange.MaxValue);
  }
}