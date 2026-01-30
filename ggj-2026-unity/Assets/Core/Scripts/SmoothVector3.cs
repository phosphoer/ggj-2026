using UnityEngine;

namespace Assets.Core
{
  public struct SmoothVector3
  {
    private Vector3 rollingVector;
    private int numberOfValuesAdded;
    private float smoothingFactor;

    public SmoothVector3(float smoothingFactor)
    {
      this.numberOfValuesAdded = 0;
      this.smoothingFactor = smoothingFactor;
      this.rollingVector = Vector3.zero;
    }

    public Vector3 Value
    {
      get
      {
        return this.rollingVector;
      }
    }

    public void Add(Vector3 value)
    {
      if (this.numberOfValuesAdded == 0)
      {
        this.rollingVector = value;
      }
      else
      {
        this.rollingVector = Vector3.Lerp(value, this.rollingVector, this.smoothingFactor);
      }

      ++this.numberOfValuesAdded;
    }

    public string ToShortString()
    {
      return $"{this.Value.x.ToString("F1")}, {this.Value.y.ToString("F1")}, {this.Value.z.ToString("F1")}";
    }
  }
}
