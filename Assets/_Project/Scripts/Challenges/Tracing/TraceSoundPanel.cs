using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VamosAprendiendo.Core;

namespace VamosAprendiendo.Challenges.Tracing
{
    public enum TraceSoundMode { Silence, Music, Voice }

    /// <summary>
    /// Panel "Sonido de apoyo" del reto de trazo: Silencio, Música suave o Voz guía, con un volumen ajustable.
    /// La música es una pista suave en bucle; la voz guía lee la letra o el número (Resources/Voice/&lt;id del glifo&gt;, por ejemplo
    /// "lo_a" o "d_7"): si todavía no existe el audio, el botón de escuchar avisa en vez de fallar. El modo y el volumen se recuerdan.
    /// </summary>
    public class TraceSoundPanel : MonoBehaviour
    {
        private const string ModeKey = "trace_sound_mode";

        [SerializeField] private Button silenceButton;
        [SerializeField] private Button musicButton;
        [SerializeField] private Button voiceButton;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TMP_Text volumeLabel;
        [SerializeField] private AudioClip musicClip;
        [Tooltip("Voces de las letras. Si falta, se busca Resources/LetterVoices (Tools/Audio/Build Letter Voice Library)")]
        [SerializeField] private LetterVoiceLibrary voices;
        [SerializeField] private Color selectedColor = new Color32(0xDD, 0xD6, 0xFF, 255);
        [SerializeField] private Color normalColor = new Color32(0xF1, 0xEF, 0xFF, 255);

        private AudioSource _music;
        private AudioSource _voice;
        private TraceSoundMode _mode;

        public TraceSoundMode Mode => _mode;

        private void Awake()
        {
            _music = gameObject.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.loop = true;
            _music.spatialBlend = 0f;
            _music.clip = musicClip;
            _voice = gameObject.AddComponent<AudioSource>();
            _voice.playOnAwake = false;
            _voice.spatialBlend = 0f;

            if (silenceButton != null) silenceButton.onClick.AddListener(() => SetMode(TraceSoundMode.Silence));
            if (musicButton != null) musicButton.onClick.AddListener(() => SetMode(TraceSoundMode.Music));
            if (voiceButton != null) voiceButton.onClick.AddListener(() => SetMode(TraceSoundMode.Voice));
            if (volumeSlider != null)
            {
                volumeSlider.minValue = 0f;
                volumeSlider.maxValue = 1f;
                volumeSlider.SetValueWithoutNotify(GameAudio.MusicVolume);
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }

            if (voices == null) voices = Resources.Load<LetterVoiceLibrary>("LetterVoices");
            // Por defecto suena la voz de la letra al empezar el ejercicio; quien prefiera silencio lo elige y se recuerda
            _mode = (TraceSoundMode)Mathf.Clamp(PlayerPrefs.GetInt(ModeKey, (int)TraceSoundMode.Voice), 0, 2);
        }

        private void Start() => Apply();

        private void OnDestroy()
        {
            if (_music != null) _music.Stop();
        }

        private void SetMode(TraceSoundMode mode)
        {
            _mode = mode;
            PlayerPrefs.SetInt(ModeKey, (int)mode);
            PlayerPrefs.Save();
            Apply();
        }

        private void OnVolumeChanged(float value)
        {
            GameAudio.SetMusicVolume(value);
            Apply();
        }

        private void Apply()
        {
            float volume = GameAudio.MusicVolume;
            _music.volume = volume * 0.6f;
            _voice.volume = volume;
            bool playMusic = _mode == TraceSoundMode.Music && musicClip != null;
            if (playMusic && !_music.isPlaying) _music.Play();
            else if (!playMusic && _music.isPlaying) _music.Stop();

            Paint(silenceButton, _mode == TraceSoundMode.Silence);
            Paint(musicButton, _mode == TraceSoundMode.Music);
            Paint(voiceButton, _mode == TraceSoundMode.Voice);
            if (volumeLabel != null)
                volumeLabel.text = volume <= 0.01f ? "Sin volumen" : (volume < 0.4f ? "Volumen bajo" : (volume < 0.75f ? "Volumen medio" : "Volumen alto"));
        }

        private void Paint(Button button, bool selected)
        {
            if (button == null) return;
            if (button.targetGraphic != null) button.targetGraphic.color = selected ? selectedColor : normalColor;
            // El modo elegido se agranda un poco, así se distingue aunque las imágenes de los botones sean iguales
            button.transform.localScale = selected ? Vector3.one * 1.08f : Vector3.one;
        }

        /// <summary>Lee en voz alta la letra o el número. Devuelve false si todavía no hay audio para ese ejercicio.</summary>
        public bool PlayVoice(string glyphId)
        {
            var clip = voices != null ? voices.GetClip(glyphId) : null;
            if (clip == null) clip = Resources.Load<AudioClip>("Voice/" + glyphId);
            if (clip == null) return false;
            _voice.Stop();
            _voice.clip = clip;
            _voice.volume = GameAudio.MusicVolume;
            _voice.Play();
            return true;
        }

        /// <summary>Al empezar el ejercicio suena la letra, salvo que el jugador haya elegido Silencio.</summary>
        public void OnGlyphShown(string glyphId)
        {
            if (_mode != TraceSoundMode.Silence) PlayVoice(glyphId);
        }
    }
}
