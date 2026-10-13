using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mutiny.Presentation
{
    [DisallowMultipleComponent]
    public sealed class MutinyAudioManager : MonoBehaviour
    {
        private static MutinyAudioManager s_Instance;

        public static MutinyAudioManager Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = FindAnyObjectByType<MutinyAudioManager>();
                    if (s_Instance == null)
                    {
                        var go = new GameObject("MutinyAudioManager");
                        s_Instance = go.AddComponent<MutinyAudioManager>();
                    }
                }
                return s_Instance;
            }
        }

        [Header("Audio Sources")]
        public AudioSource SfxSource;
        public AudioSource MusicSource;

        [Header("Settings")]
        public bool SfxEnabled = true;
        public bool MusicEnabled = true;
        public float SfxVolume = 1f;
        public float MusicVolume = 0.8f;

        private Dictionary<string, AudioClip> m_SfxClips = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, AudioClip> m_MusicClips = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);

        // Production diagnostics and parity tests observe actual resolved SFX here.
        // The event is raised only when a loaded clip is about to be played.
        public event Action<string> SfxPlayed;

        private void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_Instance = this;
            DontDestroyOnLoad(gameObject);

            // Load saved settings
            SfxEnabled = Mutiny.Persistence.MutinySaveSystem.SfxEnabled;
            MusicEnabled = Mutiny.Persistence.MutinySaveSystem.MusicEnabled;
            SfxVolume = Mutiny.Persistence.MutinySaveSystem.SfxVolume;
            MusicVolume = Mutiny.Persistence.MutinySaveSystem.MusicVolume;

            if (SfxSource == null)
            {
                SfxSource = gameObject.AddComponent<AudioSource>();
                SfxSource.playOnAwake = false;
            }

            if (MusicSource == null)
            {
                MusicSource = gameObject.AddComponent<AudioSource>();
                MusicSource.loop = true;
                MusicSource.playOnAwake = false;
            }

            LoadAllAudioClips();
        }

        private void LoadAllAudioClips()
        {
            m_SfxClips.Clear();
            AudioClip[] sfxClips = Resources.LoadAll<AudioClip>("Audio/SFX");
            for (int i = 0; i < sfxClips.Length; i++)
            {
                AudioClip clip = sfxClips[i];
                if (clip != null)
                    m_SfxClips[clip.name] = clip;
            }

            m_MusicClips.Clear();
            AudioClip[] musicClips = Resources.LoadAll<AudioClip>("Audio/Music");
            for (int i = 0; i < musicClips.Length; i++)
            {
                AudioClip clip = musicClips[i];
                if (clip != null)
                    m_MusicClips[clip.name] = clip;
            }
        }

        public void PlaySFX(string soundName, float volumeScale = 1f)
        {
            if (!SfxEnabled || SfxSource == null || string.IsNullOrEmpty(soundName))
                return;

            if (m_SfxClips.TryGetValue(soundName, out AudioClip clip))
            {
                SfxPlayed?.Invoke(soundName);
                SfxSource.PlayOneShot(clip, SfxVolume * volumeScale);
            }
        }

        public void PlayMusic(string musicName, bool loop = true)
        {
            if (MusicSource == null || string.IsNullOrEmpty(musicName))
                return;

            if (m_MusicClips.TryGetValue(musicName, out AudioClip clip))
            {
                bool alreadyPlaying = MusicSource.clip == clip && MusicSource.isPlaying;

                // The requested menu/game track is logical state, so remember it even
                // while music is disabled. Turning music back on must start the track
                // belonging to the current screen instead of waiting for another request.
                MusicSource.clip = clip;
                MusicSource.loop = loop;
                MusicSource.volume = MusicVolume;

                if (!MusicEnabled || alreadyPlaying)
                    return;

                MusicSource.Play();
            }
        }

        public void StopMusic()
        {
            if (MusicSource != null)
            {
                MusicSource.Stop();
            }
        }

        public void PlayCharacterVoice(string characterType)
        {
            if (string.IsNullOrEmpty(characterType)) return;
            string voiceName = characterType
                .Replace("Captain", "", StringComparison.OrdinalIgnoreCase)
                .Replace("Chief", "", StringComparison.OrdinalIgnoreCase);
            PlaySFX(voiceName);
        }

        public void ToggleSFX()
        {
            SfxEnabled = !SfxEnabled;
            Mutiny.Persistence.MutinySaveSystem.SfxEnabled = SfxEnabled;
            Debug.Log($"[MutinyAudio] HUD-CORNER-06 SFX={(SfxEnabled ? "on" : "off")}", this);
        }

        public void ToggleMusic()
        {
            MusicEnabled = !MusicEnabled;
            Mutiny.Persistence.MutinySaveSystem.MusicEnabled = MusicEnabled;
            if (!MusicEnabled && MusicSource != null)
            {
                // MusicController.turnOffMusic stops both Flash Sound instances. Do
                // not pause: turning it back on restarts the current menu/game track.
                MusicSource.Stop();
            }
            else if (MusicEnabled && MusicSource != null)
            {
                if (MusicSource.clip != null)
                {
                    MusicSource.volume = MusicVolume;
                    MusicSource.Play();
                }
            }

            Debug.Log($"[MutinyAudio] HUD-CORNER-06 music={(MusicEnabled ? "on" : "off")} clip={(MusicSource != null && MusicSource.clip != null ? MusicSource.clip.name : "none")}", this);
        }

        public void SetSfxVolume(float vol)
        {
            SfxVolume = Mathf.Clamp01(vol);
            Mutiny.Persistence.MutinySaveSystem.SfxVolume = SfxVolume;
        }

        public void SetMusicVolume(float vol)
        {
            MusicVolume = Mathf.Clamp01(vol);
            Mutiny.Persistence.MutinySaveSystem.MusicVolume = MusicVolume;
            if (MusicSource != null)
            {
                MusicSource.volume = MusicVolume;
            }
        }
    }
}
