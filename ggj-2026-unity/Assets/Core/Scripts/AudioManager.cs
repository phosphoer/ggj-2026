// The MIT License (MIT)
// Copyright (c) 2025 David Evans @festivevector
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public class AudioManager : Singleton<AudioManager>
{
  [SerializeField] private AudioMixerSnapshot _defaultSnapshot = null;

  private Dictionary<GameObject, AudioGroup> _audioGroups = new Dictionary<GameObject, AudioGroup>();
  private List<AudioMixerSnapshot> _snapshotStack = new();
  private List<FadeAudioState> _activeFades = new();
  private Dictionary<SoundBank, SoundBankInfo> _soundBankInfoMap = new();

  private class SoundBankInfo
  {
    public float LastPlayTime;
  }

  [System.Serializable]
  private struct FadeAudioState
  {
    public float T => Mathf.Clamp01(Timer / Duration);
    public GameObject Source;
    public SoundBank Soundbank;
    public AudioInstance Instance;
    public float TargetVolume;
    public float StartVolume;
    public float Duration;
    public float Timer;
  }

  public class AudioInstance
  {
    public AudioSource AudioSource;
    public SoundBank SoundBank;

    private int _lastRandomClip;

    public AudioClip GetNextRandomClip()
    {
      int index = Random.Range(0, SoundBank.AudioClips.Length);
      if (index == _lastRandomClip)
      {
        index = (index + 1) % SoundBank.AudioClips.Length;
      }

      _lastRandomClip = index;
      return SoundBank.AudioClips[index];
    }
  }

  private class AudioGroup
  {
    public int InstanceCount { get { return _instanceMap.Count; } }

    private Dictionary<SoundBank, AudioInstance> _instanceMap = new Dictionary<SoundBank, AudioInstance>();

    public void AddAudioInstance(AudioInstance audioInstance)
    {
      _instanceMap.Add(audioInstance.SoundBank, audioInstance);
    }

    public void RemoveAudioInstance(AudioInstance audioInstance)
    {
      _instanceMap.Remove(audioInstance.SoundBank);
    }

    public AudioInstance GetAudioInstance(SoundBank forSoundBank)
    {
      AudioInstance audioInstance = null;
      _instanceMap.TryGetValue(forSoundBank, out audioInstance);
      return audioInstance;
    }
  }

  public void PushSnapshot(AudioMixerSnapshot snapshot, float transitionTime)
  {
    _snapshotStack.Add(snapshot);
    snapshot.TransitionTo(transitionTime);
  }

  public void PopSnapshot(AudioMixerSnapshot snapshot, float transitionTime)
  {
    if (_snapshotStack.Count > 0)
    {
      AudioMixerSnapshot activeSnapshot = _snapshotStack[^1];
      _snapshotStack.Remove(snapshot);

      if (activeSnapshot == snapshot && _snapshotStack.Count > 0)
        _snapshotStack[^1].TransitionTo(transitionTime);
    }
  }

  // Fade a sound to a volume over a time period, if the source gets destroyed it will be cancelled
  public AudioManager.AudioInstance FadeSound(GameObject source, SoundBank soundBank, float duration, float toVolume = 1.0f)
  {
    // If there is already an active fade, just adjust that one to fade to the new volume
    int existingIndex = TryGetFadeState(source, soundBank, out FadeAudioState existingState);
    if (existingIndex >= 0)
    {
      float existingVolume = Mathf.SmoothStep(existingState.StartVolume, existingState.TargetVolume, existingState.T);
      existingState.TargetVolume = toVolume;
      existingState.StartVolume = existingVolume;
      existingState.Duration = duration;
      existingState.Timer = 0;
      _activeFades[existingIndex] = existingState;
      return existingState.Instance;
    }

    // Setup new fade state info
    FadeAudioState fadeState = default;
    fadeState.Duration = duration;
    fadeState.StartVolume = 0;
    fadeState.TargetVolume = toVolume;
    fadeState.Soundbank = soundBank;
    fadeState.Source = source;
    fadeState.Timer = 0;

    // If the sound is currently playing, use that instance
    // We also don't start sounds just to fade them to zero, that should
    // be done intentionally with a PlaySound() first
    AudioInstance existingInstance = GetAudioInstance(source, soundBank);
    if (existingInstance != null && existingInstance.AudioSource.isPlaying)
    {
      fadeState.StartVolume = existingInstance.AudioSource.volume;
      fadeState.Instance = existingInstance;
    }
    else if (fadeState.TargetVolume > 0)
      fadeState.Instance = PlaySound(source, soundBank, 0);

    _activeFades.Add(fadeState);
    return fadeState.Instance;
  }

  private int TryGetFadeState(GameObject source, SoundBank soundBank, out FadeAudioState fadeState)
  {
    for (int i = 0; i < _activeFades.Count; ++i)
    {
      FadeAudioState state = _activeFades[i];
      if (state.Source == source & state.Soundbank == soundBank)
      {
        fadeState = state;
        return i;
      }
    }

    fadeState = default;
    return -1;
  }

  // Play a sound on the global source (multiple simulataneous sounds supported)
  public void PlaySound(SoundBank soundBank, float volumeScale = 1.0f)
  {
    if (soundBank != null)
    {
      PlaySound(gameObject, soundBank, volumeScale);
    }
  }

  // Play a sound from a specific source, use for spatial sounds or if you want to do things with the 
  // AudioInstance like modify the volume over time by accessing the unity AudioSource
  // There is always a unique audio source per simulatenous sound
  public AudioInstance PlaySound(GameObject source, SoundBank soundBank, float volumeScale = 1.0f)
  {
    AudioInstance audioInstance = GetOrAddAudioInstance(source, soundBank);
    if (audioInstance != null)
    {
      audioInstance.AudioSource.pitch = 1.0f + audioInstance.SoundBank.PitchOffset + audioInstance.SoundBank.PitchOffsetRange.RandomValue;
      audioInstance.AudioSource.volume = audioInstance.SoundBank.VolumeScale * volumeScale;
      audioInstance.AudioSource.clip = audioInstance.GetNextRandomClip();

      // Get info about this soundbank
      if (!_soundBankInfoMap.TryGetValue(soundBank, out SoundBankInfo info))
      {
        info = new SoundBankInfo();
        _soundBankInfoMap[soundBank] = info;
      }

      // To avoid needing to handle  cases where this returns null due to the sound being culled, we'll do everything normally except
      // actually calling play
      bool shouldPlay = Time.unscaledTime > info.LastPlayTime + soundBank.MaxPlayInterval;
      if (shouldPlay)
      {
        audioInstance.AudioSource.Play();
        info.LastPlayTime = Time.unscaledTime;
      }
    }
    else
    {
      Debug.LogWarning(string.Format("Couldn't find audio instance for {0}:{1}", source.name, soundBank.name));
    }

    return audioInstance;
  }

  public AudioInstance PlaySoundClip(GameObject source, SoundBank soundBank, int clipIndex, float volumeScale = 1.0f)
  {
    AudioInstance audioInstance = GetOrAddAudioInstance(source, soundBank);
    if (audioInstance != null)
    {
      audioInstance.AudioSource.pitch = 1.0f + audioInstance.SoundBank.PitchOffset + audioInstance.SoundBank.PitchOffsetRange.RandomValue;
      audioInstance.AudioSource.volume = audioInstance.SoundBank.VolumeScale * volumeScale;
      audioInstance.AudioSource.clip = audioInstance.SoundBank.AudioClips[clipIndex];
      audioInstance.AudioSource.Play();
    }
    else
    {
      Debug.LogWarning(string.Format("Couldn't find audio instance for {0}:{1}", source.name, soundBank.name));
    }

    return audioInstance;
  }

  // Stop a currently playing sound on the global source
  public void StopSound(SoundBank soundBank)
  {
    StopSound(gameObject, soundBank);
  }

  // Stop a currently playing sound on a specific source
  public void StopSound(GameObject source, SoundBank soundBank)
  {
    AudioInstance audioInstance = GetAudioInstance(source, soundBank);
    if (audioInstance != null)
    {
      audioInstance.AudioSource.Stop();
    }
    else
    {
      Debug.LogWarning(string.Format("Couldn't find audio instance for {0}:{1}", source.name, soundBank.name));
    }
  }

  // Get a currently playing audio instance on an object
  public AudioInstance GetAudioInstance(GameObject forSource, SoundBank forSoundBank)
  {
    AudioGroup audioGroup = GetAudioGroup(forSource);
    if (audioGroup == null)
      return null;

    AudioInstance audioInstance = audioGroup.GetAudioInstance(forSoundBank);
    if (audioInstance != null && audioInstance.AudioSource == null)
    {
      audioGroup.RemoveAudioInstance(audioInstance);
      if (audioGroup.InstanceCount == 0)
        _audioGroups.Remove(forSource);

      return null;
    }

    return audioInstance;
  }

  private AudioGroup GetAudioGroup(GameObject forSource)
  {
    AudioGroup audioGroup = null;
    _audioGroups.TryGetValue(forSource, out audioGroup);
    return audioGroup;
  }

  private AudioGroup GetOrAddAudioGroup(GameObject forSource)
  {
    AudioGroup audioGroup = GetAudioGroup(forSource);
    if (audioGroup == null)
    {
      audioGroup = new AudioGroup();
      _audioGroups.Add(forSource, audioGroup);
    }

    return audioGroup;
  }

  private void Awake()
  {
    Instance = this;

    if (_defaultSnapshot)
      PushSnapshot(_defaultSnapshot, 1);
  }

  private void Update()
  {
    float dt = Time.unscaledDeltaTime;
    for (int i = 0; i < _activeFades.Count; ++i)
    {
      FadeAudioState fadeState = _activeFades[i];
      fadeState.Timer += dt;

      float fadeT = fadeState.T;
      if (fadeState.Instance?.AudioSource)
      {
        fadeState.Instance.AudioSource.volume = Mathf.SmoothStep(fadeState.StartVolume, fadeState.TargetVolume, fadeT);
      }
      else
      {
        _activeFades.RemoveAt(i);
        i -= 1;
        continue;
      }

      _activeFades[i] = fadeState;

      if (fadeT >= 1)
      {
        if (fadeState.TargetVolume <= 0)
          StopSound(fadeState.Source, fadeState.Soundbank);

        _activeFades.RemoveAt(i);
        i -= 1;
        continue;
      }
    }
  }

  // I didn't make much about the audio souces configurable, this is where you'd change the defaults
  private AudioInstance GetOrAddAudioInstance(GameObject forSource, SoundBank soundBank)
  {
    AudioGroup audioGroup = GetOrAddAudioGroup(forSource);
    AudioInstance audioInstance = audioGroup.GetAudioInstance(soundBank);
    if (audioInstance == null)
    {
      audioInstance = new AudioInstance();
      audioInstance.AudioSource = forSource.AddComponent<AudioSource>();
      audioInstance.AudioSource.playOnAwake = false;
      audioInstance.AudioSource.spatialize = soundBank.IsSpatial;
      audioInstance.AudioSource.spatialBlend = soundBank.IsSpatial ? 1.0f : 0.0f;
      audioInstance.AudioSource.volume = soundBank.VolumeScale;
      audioInstance.AudioSource.loop = soundBank.IsLooping;
      audioInstance.AudioSource.minDistance = soundBank.MinDistance;
      audioInstance.AudioSource.maxDistance = soundBank.MaxDistance;
      audioInstance.AudioSource.rolloffMode = AudioRolloffMode.Linear;
      audioInstance.AudioSource.outputAudioMixerGroup = soundBank.AudioMixerGroup;
      audioInstance.AudioSource.dopplerLevel = soundBank.DopplerLevel;
      audioInstance.SoundBank = soundBank;
      audioGroup.AddAudioInstance(audioInstance);
    }

    return audioInstance;
  }
}