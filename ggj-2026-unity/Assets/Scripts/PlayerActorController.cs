using UnityEngine;

public class PlayerActorController : MonoBehaviour
{
  [SerializeField] private ObjectActorController _actor = null;
  [SerializeField] private LegNoodleController _legPrefab = null;
  [SerializeField] private GameObject _footPrefab = null;

  private void Update()
  {
    var rewiredPlayer = Rewired.ReInput.players.GetPlayer(0);
    float horizontalAxis = rewiredPlayer.GetAxis(RewiredConsts.Action.MoveHorizontal);
    float forwardAxis = rewiredPlayer.GetAxis(RewiredConsts.Action.MoveForward);

    Vector2 targetAxis = new Vector2(horizontalAxis, forwardAxis);
    _actor.MoveAxis = Mathfx.Damp(_actor.MoveAxis, targetAxis, 0.25f, Time.deltaTime * 3);
  }
}