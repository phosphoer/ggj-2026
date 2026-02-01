using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
  [SerializeField] private Animator _animator = null;
  [SerializeField] private ObjectActorController _actor = null;

  private static readonly int kAnimMoveAmount = Animator.StringToHash("MoveAmount");
  private static readonly int kAnimPossess = Animator.StringToHash("Possess");
  private static readonly int kAnimDepossess = Animator.StringToHash("Depossess");


  private float _moveBlend;

  public void PlayPossess()
  {
    _animator.SetTrigger("Possess");
  }

  public void PlayDepossess()
  {
    _animator.SetTrigger("Depossess");
  }

  private void Update()
  {
    float targetMoveAmount = _actor.MoveAxis.magnitude > 0.01f ? 1 : 0;
    _moveBlend = Mathfx.Damp(_moveBlend, targetMoveAmount, 0.25f, Time.deltaTime);
    if (_animator.gameObject.activeInHierarchy)
      _animator.SetFloat(kAnimMoveAmount, _moveBlend);
  }
}