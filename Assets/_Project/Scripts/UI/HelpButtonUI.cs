using UnityEngine;
using UnityEngine.UI;

namespace VamosAprendiendo.UI
{
    /// <summary>
    /// Botón de ayuda ("?", debajo del de pausa): vuelve a mostrar el tutorial de controles cuando el jugador quiere repasarlo.
    /// No hace nada mientras el tutorial ya está en marcha o el menú de pausa está abierto.
    /// </summary>
    public class HelpButtonUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [Tooltip("Duración del rebote al tocar el botón (tiempo sin escala)")]
        [SerializeField] private float pulseDuration = 0.18f;

        private TutorialManager _tutorial;
        private float _pulseTimer = -1f;
        private Vector3 _baseScale = Vector3.one;

        private void Awake()
        {
            if (button == null) return;
            _baseScale = button.transform.localScale;
            button.onClick.AddListener(OnClicked);
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

        private void OnClicked()
        {
            _pulseTimer = 0f;
            if (TutorialManager.IsRunning) return;
            var menu = PauseMenuUI.Instance;
            if (menu != null && menu.IsPaused) return;

            if (_tutorial == null) _tutorial = FindFirstObjectByType<TutorialManager>();
            if (_tutorial != null) _tutorial.Begin();
        }
    }
}
