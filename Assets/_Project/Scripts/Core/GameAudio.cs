using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VamosAprendiendo.Core
{
    /// <summary>
    /// Ajustes de audio del jugador: audio activado o no, volumen de la música y volumen de los efectos.
    /// Se guardan en PlayerPrefs y los usan el botón de audio y el menú de pausa.
    /// La música la escala <see cref="Gameplay.AmbientMusicPlayer"/>; los efectos se aplican a las demás fuentes de sonido de la escena.
    /// </summary>
    public static class GameAudio
    {
        private const string KeyEnabled = "audio_enabled";
        private const string KeyMusic = "audio_music_volume";
        private const string KeySfx = "audio_sfx_volume";

        private static bool _loaded;
        private static bool _enabled = true;
        private static float _music = 0.7f;
        private static float _sfx = 0.8f;
        private static readonly Dictionary<AudioSource, float> SfxBase = new Dictionary<AudioSource, float>();

        public static event Action Changed;

        public static bool Enabled { get { Load(); return _enabled; } }
        public static float MusicVolume { get { Load(); return _music; } }
        public static float SfxVolume { get { Load(); return _sfx; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            Load();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Apply();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ApplySfx();

        public static void SetEnabled(bool value)
        {
            Load();
            if (_enabled == value) return;
            _enabled = value;
            PlayerPrefs.SetInt(KeyEnabled, value ? 1 : 0);
            PlayerPrefs.Save();
            Apply();
            Changed?.Invoke();
        }

        public static void SetMusicVolume(float value)
        {
            Load();
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(_music, value)) return;
            _music = value;
            PlayerPrefs.SetFloat(KeyMusic, value);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void SetSfxVolume(float value)
        {
            Load();
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(_sfx, value)) return;
            _sfx = value;
            PlayerPrefs.SetFloat(KeySfx, value);
            PlayerPrefs.Save();
            ApplySfx();
            Changed?.Invoke();
        }

        private static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            _enabled = PlayerPrefs.GetInt(KeyEnabled, 1) == 1;
            _music = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyMusic, 0.7f));
            _sfx = Mathf.Clamp01(PlayerPrefs.GetFloat(KeySfx, 0.8f));
        }

        private static void Apply()
        {
            AudioListener.volume = _enabled ? 1f : 0f;
            ApplySfx();
        }

        // Escala las fuentes de efectos (todas menos la música ambiente) recordando su volumen original.
        private static void ApplySfx()
        {
            foreach (var src in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (src.GetComponent<Gameplay.AmbientMusicPlayer>() != null) continue;
                if (!SfxBase.TryGetValue(src, out float baseVolume))
                {
                    baseVolume = src.volume;
                    SfxBase[src] = baseVolume;
                }
                src.volume = baseVolume * _sfx;
            }

            var dead = new List<AudioSource>();
            foreach (var key in SfxBase.Keys) if (key == null) dead.Add(key);
            foreach (var key in dead) SfxBase.Remove(key);
        }
    }
}
