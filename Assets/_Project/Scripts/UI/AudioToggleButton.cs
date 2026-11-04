using UnityEngine;
using UnityEngine.UI;
using VamosAprendiendo.Core;

namespace VamosAprendiendo.UI
{
    /// <summary>
    /// Botón de audio (arriba a la derecha): activa o silencia todo el sonido del juego (música y efectos) y
    /// recuerda la elección entre sesiones (ver GameAudio). Cambia el icono entre altavoz con ondas y altavoz tachado.
    /// </summary>
    public class AudioToggleButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private Sprite onSprite;
        [SerializeField] private Sprite offSprite;
        [Tooltip("Duración del rebote al tocar el botón (tiempo sin escala, funciona en pausa)")]
        [SerializeField] private float pulseDuration = 0.18f;

        private float _pulseTimer = -1f;
        private Vector3 _baseScale = Vector3.one;

        private void Awake()
        {
            if (button != null)
            {
                _baseScale = button.transform.localScale;
                button.onClick.AddListener(Toggle);
            }
            Apply();
        }

        private void OnEnable()
        {
            GameAudio.Changed += Apply; // el menú de pausa también cambia este estado
            Apply();
        }

        private void OnDisable()
        {
            GameAudio.Changed -= Apply;
        }

        private void Update()
        {
            if (_pulseTimer < 0f || button == null) return;

            _pulseTimer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_pulseTimer / pulseDuration);
            button.transform.localScale = _baseScale * (1f - 0.12f * Mathf.Sin(t * Mathf.PI));
            if (t >= 1f)
            {
                _pulseTimer = -1f;
                button.transform.localScale = _baseScale;
            }
        }

        private void Toggle()
        {
            _pulseTimer = 0f;
            GameAudio.SetEnabled(!GameAudio.Enabled); // Apply() se llama solo desde el evento Changed
        }

        private void Apply()
        {
            if (icon != null) icon.sprite = GameAudio.Enabled ? onSprite : offSprite;
        }
    }
}
