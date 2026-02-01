using Assets.Core;
using System.Collections.Generic;

using UnityEngine;

public class FarmerPerceptionComponent : MonoBehaviour
{
  public IReadOnlyList<PlayerActorController> PlayersInCone => playersInCone;

  public float viewRadius = 10f;
  public float viewAngle = 60f;

  public Transform lightTransform;

  private List<PlayerActorController> ghostsInRadius = new List<PlayerActorController>();
  private List<PlayerActorController> possessedInRadius = new List<PlayerActorController>();
  private List<PlayerActorController> playersInRadius = new List<PlayerActorController>();
  private List<PlayerActorController> playersInCone = new List<PlayerActorController>();

  private void Update()
  {
    playersInRadius.Clear();
    playersInCone.Clear();
    possessedInRadius.Clear();
    ghostsInRadius.Clear();

    foreach (var player in PlayerActorController.Instances)
    {
      if (Vector3.Distance(player.transform.position, transform.position) < viewRadius)
      {
        playersInRadius.Add(player);

        if (player.IsPossessing)
          possessedInRadius.Add(player);
        else
          ghostsInRadius.Add(player);

        Vector3 toPlayer = player.transform.position - lightTransform.position;
        if (Vector3.Angle(lightTransform.forward, toPlayer.normalized) < viewAngle / 2)
        {
          playersInCone.Add(player);
        }
      }
    }
  }

  public int GetDetectedPlayerCount()
  {
    return ghostsInRadius.Count;
  }

  public Vector3 GetClosestDetectedObjectLocation()
  {
    float minDistance = 1000;
    PlayerActorController closestObject = null;

    foreach (var player in playersInRadius)
    {
      float distance = Vector3.Distance(player.transform.position, transform.position);
      if (distance < minDistance)
      {
        minDistance = distance;
        closestObject = player;
      }
    }

    if (closestObject)
    {
      return closestObject.transform.position;
    }

    return Vector3.zero;
  }

  public Vector3 FindClosestPlayerLocation()
  {
    float minDistance = Mathf.Infinity;
    PlayerActorController closestPlayer = null;

    foreach (var player in playersInRadius)
    {
      float distance = Vector3.Distance(player.transform.position, transform.position);
      if (distance < minDistance)
      {
        minDistance = distance;
        closestPlayer = player;
      }
    }

    return closestPlayer.transform.position;
  }

  private void OnDrawGizmos()
  {
    Gizmos.color = Color.red;
    Gizmos.matrix = lightTransform.localToWorldMatrix;
    Gizmos.DrawFrustum(Vector3.zero, viewAngle, viewRadius, 0f, 1);
  }
}
