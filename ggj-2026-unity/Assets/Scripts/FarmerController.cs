using Assets.Core;
using System.Collections.Generic;
using Unity.VisualScripting;
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
  public float speed = 10;
  public float turnSpeed = 45;

  public float minProximityToTarget = 5;

  public float minIdleTime = 5;
  public float maxIdleTime = 10;

  IState currentState;

  GameObject[] targets;

  List<GameObject> shuffledTargets = new List<GameObject>();
  ObjectActorController _actor = null;

  public GameObject currentTarget = null;

  void Start()
  {
    _actor = GetComponent<ObjectActorController>();

    targets = GameObject.FindGameObjectsWithTag("FarmerTarget");
    ShuffleTargets();
    ChangeState(new WalkState());
  }

  void Update()
  {
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

  //public void ShuffleTargets()
  //{
  //    List<int> targetIndexes = new List<int>();

  //    for (int targetIndex = 0; targetIndex < targets.Length; targetIndex++)
  //    {
  //        targetIndexes.Add(targetIndex); 
  //    }

  //    for (int shufflerIndex = 0; shufflerIndex < targets.Length; shufflerIndex++)
  //    {
  //        int index = Random.Range(0, targetIndexes.Count);
  //        shuffledTargets.Add(targets[index]);
  //        targetIndexes.Remove(index);
  //    }
  //}

  public void FindAndSetNextTarget()
  {
    currentTarget = shuffledTargets[0];
    shuffledTargets.Remove(currentTarget);
  }

  public void FaceTowards(Vector3 direction)
  {
    if (_actor != null)
    {
      _actor.LookAxis = direction.XZ();
    }
    else
    {
      // if not facing target, turn towards it
      if (direction != -transform.forward)
      {
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed); // Smooth rotation
      }
    }
  }

  public void MoveTowards(Vector3 direction)
  {
    if (_actor != null)
    {
      _actor.MoveAxis = direction.XZ();
    }
    else
    {
      // if we made it this far, move toward the target
      transform.position += direction * speed * Time.deltaTime;
    }
  }
}

public class WalkState : IState
{
  public void OnEnter(FarmerController controller)
  {
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

    // something something perceive a player
    bool fakebool = false;
    if (fakebool)
    {
      controller.ChangeState(new StartledState());
    }


    Vector3 heading = controller.currentTarget.transform.position - controller.transform.position;
    Vector3 direction = heading.NormalizedSafe();

    controller.FaceTowards(direction);
    controller.MoveTowards(direction);
  }

  public void OnExit(FarmerController controller)
  {
    // Cleanup patrol state
    controller.MoveTowards(Vector3.zero);
  }
}

public class StartledState : IState
{
  public void OnEnter(FarmerController controller)
  {
    // Initialize chase state
  }

  public void UpdateState(FarmerController controller)
  {
    // Chase logic
  }

  public void OnExit(FarmerController controller)
  {
    // Cleanup chase state
  }
}

public class IdleState : IState
{
  float timeRemaining = 1000;

  public void OnEnter(FarmerController controller)
  {
    timeRemaining = Random.Range(controller.minIdleTime, controller.maxIdleTime);
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