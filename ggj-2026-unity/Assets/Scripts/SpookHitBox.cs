using UnityEngine;

public class SpookHitBox : MonoBehaviour
{
  private bool _hitConsumed = false;

  private void OnTriggerEnter(Collider c)
  {
    if (_hitConsumed)
      return;

    FarmerController farmer = c.GetComponent<FarmerController>();
    if (farmer)
    {
      farmer.PlayEmote(FarmerController.eEmote.startled);
      _hitConsumed = true;
    }
  }
}