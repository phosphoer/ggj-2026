using UnityEngine;

public class SpookHitBox : MonoBehaviour
{
  private bool _hitConsumed = false;

  private void OnTriggerEnter(Collider c)
  {
    Debug.Log($"SpookHitBox OnTriggerEnter {c.name}");

    if (_hitConsumed)
      return;

    FarmerController farmer = c.GetComponentInParent<FarmerController>();
    if (farmer)
    {
      Debug.Log($"SpookHitBox hit farmer");
      farmer.PlayEmote(FarmerController.eEmote.startled);
      _hitConsumed = true;
    }
  }
}