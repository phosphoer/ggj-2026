using Assets.Core;
using System.Collections.Generic;

using UnityEngine;

public class FarmerPerceptionComponent : MonoBehaviour
{
    public float viewRadius = 10f;
    public float viewAngle = 60f;
    public LayerMask targetMask, obstacleMask;

    List<GameObject> objectsInRadius = new List<GameObject>();

    void OnTriggerEnter(Collider otherCollider)
    {
        objectsInRadius.Add(otherCollider.gameObject);
    }
    void OnTriggerExit(Collider otherCollider)
    {
        objectsInRadius.Remove(otherCollider.gameObject);
    }

    public int GetDetectedObjectCount()
    {
        return objectsInRadius.Count;
    }


    public Vector3 GetClosestDetectedObjectLocation()
    {
        float minDistance = 1000;
        GameObject closestObject = null;

        foreach (GameObject player in objectsInRadius)
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

    public List<GameObject> FindVisiblePlayers()
    {
        List<GameObject> visiblePlayers = new List<GameObject>();

        foreach (var target in objectsInRadius)
        {
            if(target.CompareTag("Player"))
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
        }

        return visiblePlayers;
    }

    public Vector3 FindClosestPlayerLocation()
    {
        float minDistance = 1000;
        GameObject closestPlayer = null;

        List<GameObject> visiblePlayers = FindVisiblePlayers();
        foreach (GameObject player in visiblePlayers)
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
