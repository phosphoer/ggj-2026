using UnityEngine;
using System.Collections.Generic;

public class ParticleSoundEvent : MonoBehaviour
{
  public ParticleSystem ParticleSystem;
  public SoundBank SoundBank;
  public float FadeInTime;
  public string GroupPlayIntervalName;
  public float MinPlayInterval = 1;

  private static Dictionary<string, float> _lastPlayTimeMap = new Dictionary<string, float>();

  private bool _didPlay;

  public AudioManager.AudioInstance GetAudioInstance()
  {
    return AudioManager.Instance.GetAudioInstance(gameObject, SoundBank);
  }

  private void Update()
  {
    if (!_didPlay && ParticleSystem && ParticleSystem.isPlaying)
    {
      _didPlay = true;
      PlaySound();
    }
  }

  private void PlaySound()
  {
    if (!string.IsNullOrEmpty(GroupPlayIntervalName))
    {
      float lastPlayTime;
      if (!_lastPlayTimeMap.TryGetValue(GroupPlayIntervalName, out lastPlayTime))
        lastPlayTime = 0;

      if (Time.unscaledTime > lastPlayTime + MinPlayInterval)
        _lastPlayTimeMap[GroupPlayIntervalName] = Time.unscaledTime;
      else
        return;
    }

    if (FadeInTime > 0)
      AudioManager.Instance.FadeSound(gameObject, SoundBank, FadeInTime, toVolume: 1);
    else
      AudioManager.Instance.PlaySound(gameObject, SoundBank);
  }
}