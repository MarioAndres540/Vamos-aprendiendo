using UnityEngine;
using VamosAprendiendo.Core;

namespace VamosAprendiendo.Gameplay
{
    /// <summary>
    /// Música ambiente de la escena base: una lista de pistas para el día y otra para la noche. Cada lista suena en
    /// orden (al terminar vuelve a empezar) con fundido entre pistas, y al cambiar de día a noche (o al revés) se
    /// pasa a la otra lista con un fundido. Usa dos AudioSource para poder mezclar la pista que termina con la siguiente.
    /// </summary>
    public class AmbientMusicPlayer : MonoBehaviour
    {
        [Header("Pistas (en orden)")]
        [SerializeField] private AudioClip[] dayTracks;
        [SerializeField] private AudioClip[] nightTracks;

        [Header("Mezcla")]
        [Range(0f, 1f)] [SerializeField] private float volume = 0.6f;
        [Tooltip("Segundos de fundido al pasar de una pista a otra")]
        [SerializeField] private float crossfadeSeconds = 4f;

        private AudioSource[] _sources;
        private int _active;
        private AudioClip[] _list;
        private int _index = -1;
        private bool _night;

        private void Awake()
        {
            _sources = new AudioSource[2];
            for (int i = 0; i < 2; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                src.spatialBlend = 0f;
                src.volume = 0f;
                _sources[i] = src;
            }
        }

        private void OnEnable()
        {
            SkyboxAnimator.NightChanged += OnNightChanged;
        }

        private void OnDisable()
        {
            SkyboxAnimator.NightChanged -= OnNightChanged;
        }

        private void Start()
        {
            _night = SkyboxAnimator.IsNight;
            _list = _night ? nightTracks : dayTracks;
            _index = -1;
            PlayNext();
        }

        private void OnNightChanged(bool night)
        {
            if (night == _night) return;
            _night = night;
            _list = night ? nightTracks : dayTracks;
            _index = -1;
            PlayNext();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float step = crossfadeSeconds > 0.01f ? dt / crossfadeSeconds : 1f;
            float level = volume * GameAudio.MusicVolume; // el jugador regula la música desde el menú de pausa

            for (int i = 0; i < 2; i++)
            {
                var src = _sources[i];
                float target = i == _active ? level : 0f;
                src.volume = Mathf.MoveTowards(src.volume, target, step * volume);
                if (i != _active && src.isPlaying && src.volume <= 0.001f) src.Stop();
            }

            // Cuando a la pista activa le queda el tiempo del fundido, entra la siguiente.
            var cur = _sources[_active];
            if (cur.clip != null && cur.isPlaying && cur.clip.length - cur.time <= crossfadeSeconds) PlayNext();
        }

        private void PlayNext()
        {
            if (_list == null || _list.Length == 0) return;

            _index = (_index + 1) % _list.Length;
            var clip = _list[_index];
            if (clip == null) return;

            _active = 1 - _active;
            var src = _sources[_active];
            src.clip = clip;
            src.time = 0f;
            src.volume = 0f;
            src.Play();
        }
    }
}
