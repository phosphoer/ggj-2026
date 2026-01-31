using Assets.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public interface IState
{
    void OnEnter(FarmerController controller);
    void UpdateState(FarmerController controller);
    void OnExit(FarmerController controller);
}

public class FarmerController : MonoBehaviour
{
    public float maxHealth = 100;
    public float health = 100;

    public float speed = 10;
    public float turnSpeed = 45;

    public float minProximityToTarget = 5;

    public float minIdleTime = 5;
    public float maxIdleTime = 10;

    public float startledTurnSpeed = 30;

    public FarmerPerceptionComponent perceptionObject;
    
    IState currentState;

    GameObject[] targets;
    List<GameObject> shuffledTargets = new List<GameObject>();
    public GameObject currentTarget = null;

    void Start()
    {
        targets = GameObject.FindGameObjectsWithTag("FarmerTarget");
        ShuffleTargets();
        ChangeState(new WalkState());

        health = maxHealth;
    }

    void Update()
    {
        Debug.DrawLine(transform.position, transform.position + transform.forward, Color.red);

        if (shuffledTargets.Count <= 0)
        {
            ShuffleTargets();
        }

        if (currentState != null)
        {
            currentState.UpdateState(this);
        }
    }

    public void ChangeState(IState newState)
    {
        if (currentState != null)
        {
            currentState.OnExit(this);
        }
        currentState = newState;
        currentState.OnEnter(this);
    }

    public bool IsStartled()
    {
        return perceptionObject.GetDetectedObjectCount() > 0;
    }   

    public void ShuffleTargets()
    {
        for (int targetIndex = 0; targetIndex < targets.Length; targetIndex++)
        {
            shuffledTargets.Add(targets[targetIndex]);
        }

        System.Random random = new System.Random();
        int n = shuffledTargets.Count;
        for (int i = 0; i < n; i++)
        {
            int r = i + random.Next(n - i);
            GameObject temp = shuffledTargets[r];
            shuffledTargets[r] = shuffledTargets[i];
            shuffledTargets[i] = temp;
        }
    }

    public void FindAndSetNextTarget()
    {
        currentTarget = shuffledTargets[0];
        shuffledTargets.Remove(currentTarget);
    }
}

public class WalkState : IState
{
    public void OnEnter(FarmerController controller)
    {
        Debug.Log("Farmer State: WALK");
        controller.FindAndSetNextTarget();
    }

    public void UpdateState(FarmerController controller)
    {
        // if close to target, change to idle
        float distanceToTarget = Vector3.Distance(controller.currentTarget.transform.position, controller.transform.position);

        if (distanceToTarget <= controller.minProximityToTarget)
        {
            controller.ChangeState(new IdleState());
            return;
        }

        if(controller.IsStartled())
        { 
            controller.ChangeState(new StartledState());
            return;
        }


        Vector3 heading = controller.currentTarget.transform.position - controller.transform.position;
        Vector3 direction = heading.NormalizedSafe();

        // if not facing target, turn towards it
        if (direction != -controller.transform.forward)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            controller.transform.rotation = Quaternion.Slerp(controller.transform.rotation, targetRotation, Time.deltaTime * controller.turnSpeed); // Smooth rotation
        }

        if(Mathf.Approximately(Vector3.Angle(controller.transform.forward, direction), 0.0f))
        {
            // if we made it this far, move toward the target
            controller.transform.position += direction * controller.speed * Time.deltaTime;        
        }

    }

    public void OnExit(FarmerController controller)
    {
        // Cleanup patrol state
    }
}

public class StartledState : IState
{
    float timeRemaining = 3;

    Vector3 targetLocation;

    public void OnEnter(FarmerController controller)
    {
        Debug.Log("Farmer State: STARTLED");
        targetLocation = controller.perceptionObject.GetClosestDetectedObjectLocation();
    }

    public void UpdateState(FarmerController controller)
    {
        //Vector3 heading = controller.currentTarget.transform.position - controller.transform.position;
        //Vector3 direction = heading.NormalizedSafe();

        //Quaternion targetRotation = Quaternion.LookRotation(direction);
        //controller.transform.rotation = Quaternion.Slerp(controller.transform.rotation, targetRotation, Time.deltaTime * controller.startledTurnSpeed); // Smooth rotation
    }

    public void OnExit(FarmerController controller)
    {
        
    }
}

public class WalkScaredState : WalkState
{
    float timeRemaining = 5.0f;

    public void OnEnter(FarmerController controller)
    {
        Debug.Log("Farmer State: WALKSCARED");
    }

    public void UpdateState(FarmerController controller)
    {
        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            if (timeRemaining < 0)
            {
                controller.ChangeState(new WalkState());
            }
        }
    }

    public void OnExit(FarmerController controller)
    {

    }
}

public class DamagedState : IState
{
    public void OnEnter(FarmerController controller)
    {
        Debug.Log("Farmer State: DAMAGED");

        // take damage
        // check if faint
        // if not change state to Search or walk scared?
    }
    public void UpdateState(FarmerController controller)
    {
        if (controller.health <= 0)
        {
            controller.ChangeState(new FaintState());
        }
        else
        {
            controller.ChangeState(new SearchState());
        }


    }

    public void OnExit(FarmerController controller)
    {
    }

}


public class FaintState : IState
{
    public void OnEnter(FarmerController controller)
    {
        Debug.Log("Farmer State: FAINT");
    }

    public void UpdateState(FarmerController controller)
    {
        // He's dead Jim
    }

    public void OnExit(FarmerController controller)
    {
    }
}

public class AttackState : IState
{
    float timeRemaining = 5.0f;
    public void OnEnter(FarmerController controller)
    {
        // take damage
        Debug.Log("Farmer State: ATTACK");
    }

    public void UpdateState(FarmerController controller)
    {
        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            if (timeRemaining < 0)
            {
                controller.ChangeState(new WalkScaredState());
            }
        }
    }

    public void OnExit(FarmerController controller)
    {
        // recover
    }
}

public class SearchState : IState
{
    float timeRemaining = 5.0f;

    public void OnEnter(FarmerController controller)
    {
        Debug.Log("Farmer State: SEARCH");
    }

    public void UpdateState(FarmerController controller)
    {
        if(controller.IsStartled())
        {
            controller.ChangeState(new StartledState());
        }

        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            if (timeRemaining < 0)
            {
                controller.ChangeState(new WalkScaredState());
            }
        }
    }

    public void OnExit(FarmerController controller)
    {

    }
}

public class IdleState : IState
{
    float timeRemaining = 1000;

    public void OnEnter(FarmerController controller)
    {
        Debug.Log("Farmer State: IDLE");
        timeRemaining = Random.Range(controller.minIdleTime, controller.maxIdleTime);
    }

    public void UpdateState(FarmerController controller)
    {
        if (controller.IsStartled())
        {
            controller.ChangeState(new StartledState());
        }

        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            if(timeRemaining < 0)
            {
                controller.ChangeState(new WalkState());
            }
        }
    }

    public void OnExit(FarmerController controller)
    {

    }
}