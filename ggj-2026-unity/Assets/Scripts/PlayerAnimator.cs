using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
  [SerializeField] private Animator _animator = null;
  [SerializeField] private ObjectActorController _actor = null;

  private static readonly int kAnimMoveAmount = Animator.StringToHash("MoveAmount");

  private float _moveBlend;

  private void Update()
  {
    float targetMoveAmount = _actor.MoveAxis.magnitude > 0.01f ? 1 : 0;
    _moveBlend = Mathfx.Damp(_moveBlend, targetMoveAmount, 0.25f, Time.deltaTime);
    if (_animator.gameObject.activeInHierarchy)
      _animator.SetFloat(kAnimMoveAmount, _moveBlend);
  }
}