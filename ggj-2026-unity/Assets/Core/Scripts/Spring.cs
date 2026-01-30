using UnityEngine;

[System.Serializable]
public struct Spring
{
  public float Value;
  public float TargetValue;
  public float Velocity;
  public float SpringStrength;
  public float SpringVelocityScale;
  public float SpringDamping;

  public static Spring UpdateSpring(Spring spring, float dt)
  {
    spring.Velocity += (spring.TargetValue - spring.Value) * spring.SpringStrength * dt;
    spring.Value += spring.Velocity * spring.SpringVelocityScale * dt;
    spring.Velocity = Mathfx.Damp(spring.Velocity, 0, 0.25f, dt * spring.SpringDamping);
    return spring;
  }
}