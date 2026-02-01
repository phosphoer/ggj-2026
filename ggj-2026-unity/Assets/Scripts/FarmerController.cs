using Assets.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static IdleState;

//=======================================================

public interface IState
{
  void OnEnter(FarmerController controller);
  void UpdateState(FarmerController controller);

  void OnExit(FarmerController controller);
}

//=======================================================
// THE MAN HIMSELF
//=======================================================

public class FarmerController : MonoBehaviour
{
  public float maxHealth = 100;
  public float health = 100;

  public float speed = 10;
  public float turnSpeed = 45;
  public float startledTurnSpeed = 50;

  public float minProximityToTarget = 5;

  public float minIdleTime = 5;
  public float maxIdleTime = 10;

  public float startledDuration = 1.0f;
  public float scaredWalkDuration = 5.0f;
  public float damagedDuration = 3.0f;
  public float faintDuration = 3.0f;
  public float attackDuration = 3.0f;
  public float searchDuration = 3.0f;

  public FarmerPerceptionComponent perceptionObject;

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
    Debug.DrawLine(transform.position, transform.position + transform.forward, Color.red);

    if (shuffledTargets.Count <= 0)
    {
      targets = GameObject.FindGameObjectsWithTag("FarmerTarget");
      ShuffleTargets();
      ChangeState(new WalkState());

      health = maxHealth;
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
    return perceptionObject ? perceptionObject.GetDetectedPlayerCount() > 0 : false;
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

  public void FaceTowards(Vector3 direction, float turnSpeed)
  {
    if (_actor != null)
    {
      _actor.RotateSpeed = turnSpeed;
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

    if (_animator != null)
    {
      SetIsMoving(direction.sqrMagnitude > kIsMovingThreshold * kIsMovingThreshold);
    }
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
    if (_animator != null)
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
    if (_animator != null)
    {
      _animator.SetTrigger(triggerId);
    }
  }

  private void SetAnimatorFloat(int parameterId, float value)
  {
    if (_animator != null)
    {
      _animator.SetFloat(parameterId, value);
    }
  }

  private void SetAnimatorBool(int parameterId, bool value)
  {
    if (_animator != null)
    {
      _animator.SetBool(parameterId, value);
    }
  }
}

//=======================================================
// FARMER STATES
//=======================================================
// WALK
//-------------------------------------------------------

public class WalkState : IState
{
  public virtual void OnEnter(FarmerController controller)
  {
    Debug.Log("Farmer State: WALK");
    controller.FindAndSetNextTarget();
  }

  public virtual void UpdateState(FarmerController controller)
  {
    // if close to target, change to idle
    float distanceToTarget = Vector3.Distance(controller.currentTarget.transform.position, controller.transform.position);

    if (distanceToTarget <= controller.minProximityToTarget)
    {
      controller.ChangeState(new IdleState());
      return;
    }

    // something something perceive a player
    if (controller.IsStartled())
    {
      controller.ChangeState(new StartledState());
      return;
    }

    Vector3 heading = controller.currentTarget.transform.position - controller.transform.position;
    Vector3 direction = heading.NormalizedSafe();

    controller.FaceTowards(direction, controller.turnSpeed);
    controller.MoveTowards(direction);
  }

  public virtual void OnExit(FarmerController controller)
  {
    // Cleanup patrol state
    controller.MoveTowards(Vector3.zero);
  }
}

//-------------------------------------------------------
// STARTLED
//-------------------------------------------------------

public class StartledState : IState
{
  float timeRemaining = 0.0f;

  Vector3 targetLocation;

  public void OnEnter(FarmerController controller)
  {
    Debug.Log("Farmer State: STARTLED");
    timeRemaining = controller.startledDuration;
    targetLocation = controller.perceptionObject.GetClosestDetectedObjectLocation();

    controller.PlayEmote(FarmerController.eEmote.startled);
  }

  public void UpdateState(FarmerController controller)
  {
    Vector3 heading = targetLocation - controller.transform.position;
    Vector3 direction = heading.NormalizedSafe();

    controller.FaceTowards(direction, controller.startledTurnSpeed);

    if (timeRemaining > 0)
    {
      timeRemaining -= Time.deltaTime;
      if (timeRemaining < 0)
      {
        controller.ChangeState(new SearchState());
      }
    }
  }

  public void OnExit(FarmerController controller)
  {

  }
}

//-------------------------------------------------------
// IDLE
//-------------------------------------------------------


public class IdleState : IState
{
  float timeRemaining = 0.0f;

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
      return;
    }

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

//-------------------------------------------------------
// WALK SCARED
//-------------------------------------------------------

public class WalkScaredState : WalkState
{
  float timeRemaining = 0.0f;

  public override void OnEnter(FarmerController controller)
  {
    Debug.Log("Farmer State: WALKSCARED");
    timeRemaining = controller.scaredWalkDuration;
    controller.SetIsScared(true);
    base.OnEnter(controller);
  }

  public override void UpdateState(FarmerController controller)
  {
    if (timeRemaining > 0)
    {
      timeRemaining -= Time.deltaTime;
      if (timeRemaining < 0)
      {
        controller.ChangeState(new WalkState());
      }
    }
    base.UpdateState(controller);
  }

  public override void OnExit(FarmerController controller)
  {
    controller.SetIsScared(false);
    base.OnExit(controller);
  }
}

//-------------------------------------------------------
// DAMAGED
//-------------------------------------------------------

public class DamagedState : IState
{
  float timeRemaining = 0.0f;

  public void OnEnter(FarmerController controller)
  {
    timeRemaining = controller.damagedDuration; 
    Debug.Log("Farmer State: DAMAGED");
  }
  public void UpdateState(FarmerController controller)
  {
    if (controller.health <= 0)
    {
      controller.ChangeState(new FaintState());
    }
    else
    {
      if (timeRemaining > 0)
      {
        timeRemaining -= Time.deltaTime;
        if (timeRemaining < 0)
        {
          controller.ChangeState(new SearchState());
        }
      }
    }
  }

  public void OnExit(FarmerController controller)
  {

  }
}

//-------------------------------------------------------
// FAINT
//-------------------------------------------------------

public class FaintState : IState
{
  float timeRemaining = 0.0f;
  public void OnEnter(FarmerController controller)
  {
    timeRemaining = controller.faintDuration;
    Debug.Log("Farmer State: FAINT");
    controller.PlayEmote(FarmerController.eEmote.feint);
  }

  public void UpdateState(FarmerController controller)
  {
    if (timeRemaining > 0)
    {
      timeRemaining -= Time.deltaTime;
      if (timeRemaining < 0)
      {
        GameController.Instance.SpawnDeadFarmer();
      }
    }
  }

  public void OnExit(FarmerController controller)
  {

  }
}

//-------------------------------------------------------
// ATTACK
//-------------------------------------------------------

public class AttackState : IState
{
  float timeRemaining = 0.0f;
  public void OnEnter(FarmerController controller)
  {
    Debug.Log("Farmer State: ATTACK");
    timeRemaining = controller.attackDuration;

    if (controller.perceptionObject)
    {
      List<PlayerActorController> players= controller.perceptionObject.FindVisiblePlayers();

      foreach (var player in players)
      {
        player.EjectPossession();
      }
    }
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

  }
}


public class SearchState : IState
{
  float timeRemaining = 0.0f;

  public void OnEnter(FarmerController controller)
  {
    Debug.Log("Farmer State: SEARCH");
    timeRemaining = controller.searchDuration;
    controller.SetIsSearching(true);
  }

  public void UpdateState(FarmerController controller)
  {
    //if (controller.IsStartled())
    //{
    //  controller.ChangeState(new StartledState());
    //  return;
    //}

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
    controller.SetIsSearching(false);
  }
}