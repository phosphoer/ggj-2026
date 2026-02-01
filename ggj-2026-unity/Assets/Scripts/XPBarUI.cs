using UnityEngine;
using UnityEngine.UI;

public class XPBarUI : MonoBehaviour
{
  public event System.Action LevelUp;

  [SerializeField] private Transform _barFillRoot = null;
  [SerializeField] private TMPro.TMP_Text _currentLevelText = null;
  [SerializeField] private Spring _levelUpAnimSpring = default;

  private bool _animFinished;
  private float _targetPercent;
  private float _currentPercent;
  private int _currentLevel;
  private float _showTimer;

  public void AnimateXP(float startPercent, float targetPercent, int currentLevel)
  {
    _animFinished = false;
    _targetPercent = Mathf.Clamp01(targetPercent);
    _currentPercent = Mathf.Clamp01(startPercent);
    _currentLevel = currentLevel;
    _currentLevelText.text = $"{currentLevel}";
    SetBarFillPercent(_currentPercent);
  }

  private void OnEnable()
  {
    _showTimer = 1;
  }

  private void Update()
  {
    _currentPercent = Mathfx.Damp(_currentPercent, _targetPercent, 0.25f, Time.deltaTime);
    SetBarFillPercent(_currentPercent);

    if (Mathf.Abs(_currentPercent - _targetPercent) < 0.01f)
    {
      if (_currentPercent >= 1 && !_animFinished)
      {
        _animFinished = true;
        _showTimer += 1;
        _currentLevel += 1;
        _currentLevelText.text = $"{_currentLevel}";

        _levelUpAnimSpring.Velocity += 5;
        LevelUp?.Invoke();
      }

      _showTimer -= Time.deltaTime;
      if (_showTimer <= 0)
        WorldUIManager.Instance.HideItem(transform.parent);
    }

    _levelUpAnimSpring = Spring.UpdateSpring(_levelUpAnimSpring, Time.deltaTime);
    _currentLevelText.transform.localScale = Vector3.one * (1 + _levelUpAnimSpring.Value);
  }

  private void SetBarFillPercent(float fillPercent)
  {
    _barFillRoot.localScale = Vector3.one.WithX(fillPercent);
  }
}