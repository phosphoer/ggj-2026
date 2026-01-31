using UnityEngine;

public class PossessableSpawnPoint : MonoBehaviour
{
  public GameObject PossessableTemplate => _posseessableTemplate;

  [SerializeField] private GameObject _posseessableTemplate;

}
