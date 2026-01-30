using UnityEngine;

public class Slappable : MonoBehaviour
{
  public event System.Action<GameCharacterController> Slapped;

  public void ReceiveSlap(GameCharacterController fromCharacter)
  {
    Debug.Log($"{name} received slap from {fromCharacter.name}");
    Slapped?.Invoke(fromCharacter);
  }
}