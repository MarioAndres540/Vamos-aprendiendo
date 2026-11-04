using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VamosAprendiendo.Core;

namespace VamosAprendiendo.UI
{
    /// <summary>
    /// Panel de audio del menú de pausa: casilla para activar o silenciar el audio y barras de volumen de la música y
    /// los efectos (en pasos de 5 %). Lee y escribe en <see cref="GameAudio"/>, así que queda sincronizado con el botón
    /// de audio de la esquina. Con el audio desactivado las barras se atenúan pero conservan su valor.
    /// </summary>
    public class PauseAudioPanel : MonoBehaviour
    {
        private const float Step = 5f;

        [Header("Casilla")]
        [SerializeField] private Button checkButton;
        [Tooltip("Centro blanco de la casilla: solo se ve cuando el audio está desactivado")]
        [SerializeField] private GameObject emptyBox;
        [SerializeField] private GameObject checkMark;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text subtitle;

        [Header("Volúmenes")]
        [SerializeField] private CanvasGroup slidersGroup;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private TMP_Text musicPercent;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private TMP_Text sfxPercent;

        private void Awake()
        {
            if (checkButton != null) checkButton.onClick.AddListener(() => GameAudio.SetEnabled(!GameAudio.Enabled));
            if (musicSlider != null) musicSlider.onValueChanged.AddListener(v => GameAudio.SetMusicVolume(Snap(musicSlider, v) / 100f));
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(v => GameAudio.SetSfxVolume(Snap(sfxSlider, v) / 100f));
        }

        private void OnEnable()
        {
            GameAudio.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            GameAudio.Changed -= Refresh;
        }

        private static float Snap(Slider slider, float value)
        {
            float snapped = Mathf.Round(value / Step) * Step;
            if (!Mathf.Approximately(snapped, value)) slider.SetValueWithoutNotify(snapped);
            return snapped;
        }

        private void Refresh()
        {
            bool on = GameAudio.Enabled;
            if (emptyBox != null) emptyBox.SetActive(!on);
            if (checkMark != null) checkMark.SetActive(on);
            if (title != null) title.text = on ? "Audio activado" : "Audio desactivado";
            if (subtitle != null) subtitle.text = on ? "Música y efectos suenan" : "Todo en silencio";
            if (slidersGroup != null)
            {
                slidersGroup.alpha = on ? 1f : 0.4f;
                slidersGroup.interactable = on;
                slidersGroup.blocksRaycasts = on;
            }

            Show(musicSlider, musicPercent, GameAudio.MusicVolume);
            Show(sfxSlider, sfxPercent, GameAudio.SfxVolume);
        }

        private static void Show(Slider slider, TMP_Text label, float volume)
        {
            float percent = Mathf.Round(volume * 100f / Step) * Step;
            if (slider != null) slider.SetValueWithoutNotify(percent);
            if (label != null) label.text = Mathf.RoundToInt(percent) + "%";
        }
    }
}
