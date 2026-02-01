using Assets.Core;
using System.Collections.Generic;

using UnityEngine;

public class FarmerPerceptionComponent : MonoBehaviour
{
    public float viewRadius = 10f;
    public float viewAngle = 60f;
    public LayerMask targetMask, obstacleMask;

    List<PlayerActorController> playersInRadius = new List<PlayerActorController>();

    void OnTriggerEnter(Collider otherCollider)
    {
      PlayerActorController player = otherCollider.GetComponentInParent<PlayerActorController>();
      if (player != null)
      {
        playersInRadius.Add(player);
      }
    }
    void OnTriggerExit(Collider otherCollider)
    {
      PlayerActorController player = otherCollider.GetComponentInParent<PlayerActorController>();
      if (player != null)
      {
        playersInRadius.Remove(player);
      }
    } 

    public int GetDetectedPlayerCount()
    {
        List<PlayerActorController> players = FindClosePlayers();
        return players.Count;
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

    public List<PlayerActorController> FindClosePlayers()
    {
      List<PlayerActorController> visiblePlayers = new List<PlayerActorController>();

      foreach (var target in playersInRadius)
      {
        Vector3 dirToTarget = (target.transform.position - transform.position).normalized;
        float angleToTarget = Vector3.Angle(transform.up, dirToTarget);
        
        if (!Physics.Raycast(transform.position, dirToTarget, out RaycastHit hit, viewRadius, obstacleMask))
        {
          visiblePlayers.Add(target);
        }
      }

      return visiblePlayers;
    }

    public List<PlayerActorController> FindVisiblePlayers()
    {
        List<PlayerActorController> visiblePlayers = new List<PlayerActorController>();

        foreach (var target in playersInRadius)
        {
          Vector3 dirToTarget = (target.transform.position - transform.position).normalized;
          float angleToTarget = Vector3.Angle(transform.up, dirToTarget);
          if (angleToTarget < viewAngle / 2)
          {
              if (!Physics.Raycast(transform.position, dirToTarget, out RaycastHit hit, viewRadius, obstacleMask))
              {
                  visiblePlayers.Add(target);
              }
           }
        }

        return visiblePlayers;
    }

    public Vector3 FindClosestPlayerLocation()
    {
        float minDistance = 1000;
        PlayerActorController closestPlayer = null;

        List<PlayerActorController> visiblePlayers = FindVisiblePlayers();
        foreach (var player in visiblePlayers)
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
}
