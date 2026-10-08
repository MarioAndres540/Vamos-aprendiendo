using UnityEngine;
using UnityEngine.UI;

namespace VamosAprendiendo.UI
{
    /// <summary>
    /// Botón táctil de pausa/play: pausa el juego con el menú de pausa y, estando en pausa,
    /// cambia a icono de play y reanuda al tocarlo. Esc / Start del mando siguen funcionando.
    /// </summary>
    public class PauseButtonUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private Sprite pauseSprite;
        [SerializeField] private Sprite playSprite;
        [Tooltip("Duración del rebote al tocar el botón (tiempo sin escala, funciona en pausa)")]
        [SerializeField] private float pulseDuration = 0.18f;

        private bool _showingPlay;
        private float _pulseTimer = -1f;
        private Vector3 _baseScale = Vector3.one;

        private void Awake()
        {
            if (button != null)
            {
                _baseScale = button.transform.localScale;
                button.onClick.AddListener(OnClicked);
            }
            Apply(false);
        }

        private void Update()
        {
            var menu = PauseMenuUI.Instance;
            bool paused = menu != null && menu.IsPaused;
            if (paused != _showingPlay) Apply(paused);

            if (_pulseTimer >= 0f && button != null)
            {
                _pulseTimer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(_pulseTimer / pulseDuration);
                // Se encoge al 88 % y vuelve con un leve rebote.
                float scale = 1f - 0.12f * Mathf.Sin(t * Mathf.PI);
                button.transform.localScale = _baseScale * scale;
                if (t >= 1f)
                {
                    _pulseTimer = -1f;
                    button.transform.localScale = _baseScale;
                }
            }
        }

        private void OnClicked()
        {
            _pulseTimer = 0f;
            var menu = PauseMenuUI.Instance;
            if (menu != null) menu.TogglePause();
        }

        private void Apply(bool paused)
        {
            _showingPlay = paused;
            if (icon != null) icon.sprite = paused ? playSprite : pauseSprite;
        }
    }
}
