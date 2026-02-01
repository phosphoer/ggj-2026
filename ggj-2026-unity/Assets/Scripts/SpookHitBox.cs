using UnityEngine;

public class SpookHitBox : MonoBehaviour
{
  public float Damage;

  private bool _hitConsumed = false;

  private void OnTriggerEnter(Collider c)
  {
    Debug.Log($"SpookHitBox OnTriggerEnter {c.name}");

    if (_hitConsumed || c.isTrigger)
      return;

    FarmerController farmer = c.GetComponentInParent<FarmerController>();
    if (farmer)
    {
      Debug.Log($"SpookHitBox hit farmer");
      farmer.PlayEmote(FarmerController.eEmote.startled);
      farmer.health -= Damage;
      farmer.ChangeState(new DamagedState());

      var gameUI = PlayerUI.Instance.GetPage<GamePlayUI>();
      gameUI.HeartRateUI.SetTempHeartRate(100);

      _hitConsumed = true;
    }
  }
}