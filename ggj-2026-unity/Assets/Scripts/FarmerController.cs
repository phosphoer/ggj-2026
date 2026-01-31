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

  public Animator _animator;
  public GameObject currentTarget = null;

  private static int kLocomotionParameter= Animator.StringToHash("LocomotionParameter");
  private static int kIsSearching= Animator.StringToHash("IsSearching");
  private static int kFeintTrigger= Animator.StringToHash("FeintTrigger");
  private static int kStartledTriggered= Animator.StringToHash("StartledTriggered");
  private static int kVictoryTrigger= Animator.StringToHash("VictoryTrigger");
  private static float kIsMovingThreshold= 0.01f;

  private bool _isScared= false;
  private bool _isMoving= false;
  private bool _isSearching= false;

  public enum eLocomotionState
  {
    idle,
    walking,
    walking_scared
  }

  public enum eEmote
  {
    feint,
    startled,
    victory
  }

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

    SetIsMoving(direction.sqrMagnitude > kIsMovingThreshold * kIsMovingThreshold);
  }

  private void SetIsMoving(bool isMoving)
  {
    if (isMoving != _isMoving)
    {
      _isMoving= isMoving;
      UpdateLocomotionState();
    }
  }

  public void SetIsScared(bool isScared)
  {
    if (isScared != _isScared)
    {
      _isScared = isScared;
      UpdateLocomotionState();
    }
  }

  public void SetIsSearching(bool isSearching)
  {
    if (isSearching != _isSearching)
    {
      _isSearching= isSearching;
      SetAnimatorBool(kIsSearching, _isSearching);
    }
  }

  private void UpdateLocomotionState()
  {
    if (_isMoving)
    {
      if (_isScared)
        SetLocomotionState(eLocomotionState.walking_scared);
      else
        SetLocomotionState(eLocomotionState.walking);
    }
    else
    {
      SetLocomotionState(eLocomotionState.idle);
    }
  }

  private void SetLocomotionState(eLocomotionState locomotionState)
  {
    float parameter= 0.0f;
    switch(locomotionState)
    {
    case eLocomotionState.idle: parameter = 0.0f; break;
    case eLocomotionState.walking: parameter = 1.0f; break;
    case eLocomotionState.walking_scared: parameter = 2.0f; break;
    }

    SetAnimatorFloat(kLocomotionParameter, parameter);
  }

  public void PlayEmote(eEmote emote)
  {
    switch(emote)
    {
    case eEmote.feint:
      SetAnimatorTrigger(kFeintTrigger);
      break;
    case eEmote.startled:
      SetAnimatorTrigger(kStartledTriggered);
      break;
    case eEmote.victory:
      SetAnimatorTrigger(kVictoryTrigger);
      break;
    }
  }

  private void SetAnimatorTrigger(int triggerId)
  {
    _animator.SetTrigger(triggerId);
  }

  private void SetAnimatorFloat(int parameterId, float value)
  {
    _animator.SetFloat(parameterId, value);
  }

  private void SetAnimatorBool(int parameterId, bool value)
  {
    _animator.SetBool(parameterId, value);
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
    controller.PlayEmote(FarmerController.eEmote.startled);
  }

  public void UpdateState(FarmerController controller)
  {
    // Chase logic
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