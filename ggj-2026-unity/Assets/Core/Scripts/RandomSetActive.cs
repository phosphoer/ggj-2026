using UnityEngine;

public class RandomSetActive : MonoBehaviour
{
  public float ActiveChance = 0.5f;

  [SerializeField]
  private GameObject[] _targets = null;

  private void Awake()
  {
    foreach (var obj in _targets)
      obj.SetActive(Random.value < ActiveChance);
  }
}