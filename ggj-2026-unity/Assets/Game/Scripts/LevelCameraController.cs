using UnityEngine;

public class LevelCameraController : CameraControllerDynamic
{
  [Header("Dynamic Framing")]
  [SerializeField] private float _maxIdealViewingAngle = 30f; // Max angle from center before zooming out (degrees)
  [SerializeField] private float _minIdealViewingAngle = 10f; // Min angle from center before zooming in (degrees)
  [SerializeField] private float _minDistance = 0f; // Minimum distance from initial position
  [SerializeField] private float _maxDistance = 80f; // Maximum distance from initial position
  [SerializeField] private float _zoomSpeed = 2f; // How quickly the camera zooms in/out

  private Vector3 _initialPosition = Vector3.zero;
  private Vector3 _initialForward = Vector3.forward;
  private float _currentDistance = 0f;

  public void Awake()
  {
    _initialPosition = MountPoint.position;
    _initialForward = MountPoint.forward.normalized;
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

    // Check viewing angles for all players
    float maxAngle = 0f;
    bool anyPlayerOutside = false;
    bool allPlayersInside = true;

    foreach (var player in players)
    {
      Vector3 toPlayer = player.transform.position - currentCameraPos;
      float angle = Vector3.Angle(_initialForward, toPlayer);

      if (angle > maxAngle)
        maxAngle = angle;

      if (angle > _maxIdealViewingAngle)
        anyPlayerOutside = true;

      if (angle > _minIdealViewingAngle)
        allPlayersInside = false;
    }

    // Adjust distance based on viewing angles
    float distanceAdjustment = 0f;

    if (anyPlayerOutside)
    {
      // Players too close to edge - zoom out
      float overshoot = maxAngle - _maxIdealViewingAngle;
      distanceAdjustment = -overshoot * _zoomSpeed * Time.deltaTime;
    }
    else if (allPlayersInside)
    {
      // All players well within view - zoom in
      float undershoot = _minIdealViewingAngle - maxAngle;
      distanceAdjustment = undershoot * _zoomSpeed * Time.deltaTime;
    }

    // Apply adjustment and clamp
    _currentDistance += distanceAdjustment;
    _currentDistance = Mathf.Clamp(_currentDistance, _minDistance, _maxDistance);

    // Position the camera along the initial forward direction from the initial position
    Vector3 targetPosition = _initialPosition + _initialForward * _currentDistance;
    MountPoint.position = targetPosition;
  }

  public void Reset()
  {
    MountPoint.position = _initialPosition;
    _currentDistance = 0f;
  }
}