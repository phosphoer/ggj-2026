using UnityEngine;
using static UnityEngine.UI.Image;

public class LevelCameraController : CameraControllerStatic
{
  [Header("Dynamic Framing")]
  [SerializeField] private float _minDistance = 10f; // Minimum distance from initial position
  [SerializeField] private float _maxDistance = 30f; // Maximum distance from initial position
  [SerializeField] private float _zoomSpeed = 2f; // How quickly the camera zooms in/out

  private Vector3 _initialPosition = Vector3.zero;
  private Vector3 _initialForward = Vector3.forward;
  private float _currentDistance = 0f;

  public void Awake()
  {
    var forward = MountPoint.forward;

    _initialPosition = MountPoint.position;
    _initialForward = new Vector3(forward.x, 0, forward.z).normalized;
    _currentDistance = 0f;
  }

  void Update()
  {
    UpdateCameraPosition();
  }

  private void UpdateCameraPosition()
  {
    var players = GameController.Instance?.SpawnedPlayers;
    if (players == null || players.Count == 0)
      return;

    // Calculate current camera position along the track
    Vector3 currentCameraPos = MountPoint.position;

    var playerCentroid = Vector3.zero;
    if (players.Count > 0)
    {
      foreach (var player in players)
      {
        playerCentroid += player.transform.position;
      }

      playerCentroid /= (float)players.Count;
    }

    Vector3 vectorToCentroid3d = playerCentroid - MountPoint.position;
    float distanceToCentroid = vectorToCentroid3d.magnitude;
    Vector3 directonToCentroid = vectorToCentroid3d / distanceToCentroid;

    // Adjust distance based on viewing angles
    float distanceAdjustment = 0f;
    if (distanceToCentroid < _minDistance)
    {
      // Players too close to edge - zoom out
      float undershoot = _minDistance - distanceToCentroid;
      distanceAdjustment = -undershoot * _zoomSpeed * Time.deltaTime;
    }
    else if (distanceToCentroid > _maxDistance)
    {
      // All players well within view - zoom in
      float overshoot = distanceToCentroid - _maxDistance;
      distanceAdjustment = overshoot * _zoomSpeed * Time.deltaTime;
    }

    Vector3 targetPosition = MountPoint.position + directonToCentroid.WithY(0) * distanceAdjustment;
    MountPoint.position = Mathfx.Damp(MountPoint.position, targetPosition, 0.25f, Time.deltaTime);

    Quaternion targetOrientation = Quaternion.LookRotation(vectorToCentroid3d, Vector3.up);
    MountPoint.rotation = Mathfx.Damp(MountPoint.rotation, targetOrientation, 0.25f, Time.deltaTime);
  }

  public void Reset()
  {
    MountPoint.position = _initialPosition;
    _currentDistance = 0f;
  }
}