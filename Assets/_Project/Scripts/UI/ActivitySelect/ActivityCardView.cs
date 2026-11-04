using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VamosAprendiendo.Challenges.Tracing;

namespace VamosAprendiendo.UI
{
    /// <summary>Una tarjeta de la pantalla de actividades: su porcentaje, la barra, la estrella de completada y el candado.</summary>
    public class ActivityCardView : MonoBehaviour
    {
        [SerializeField] private ActivityKind kind;
        [SerializeField] private Button button;
        [SerializeField] private Image barFill;
        [SerializeField] private TMP_Text percentText;
        [SerializeField] private Image star;
        [SerializeField] private GameObject lockIcon;
        [Tooltip("Sin contenido todavía: la tarjeta aparece bloqueada con \"Próximamente\"")]
        [SerializeField] private bool comingSoon;
        [SerializeField] private Color accent = new Color32(0x5B, 0x57, 0xD6, 255);
        [SerializeField] private Color starOff = new Color32(0xD6, 0xD8, 0xEA, 255);
        [SerializeField] private Color starOn = new Color32(0xFF, 0xB8, 0x1C, 255);

        private float _shown;
        private float _target;

        public ActivityKind Kind => kind;
        public Button Button => button;
        public bool ComingSoon => comingSoon;

        /// <summary>Lee el porcentaje guardado y lo muestra. Con <paramref name="animate"/> la barra sube suave.</summary>
        public void Refresh(bool animate)
        {
            int percent = comingSoon ? 0 : ActivityProgress.Percent(kind);
            _target = percent / 100f;
            if (!animate) _shown = _target;

            if (percentText != null) percentText.text = comingSoon ? "Próximamente" : percent + "%";
            if (star != null) star.color = percent >= 100 ? starOn : starOff;
            if (star != null) star.gameObject.SetActive(!comingSoon);
            if (lockIcon != null) lockIcon.SetActive(comingSoon);
            if (button != null) button.interactable = !comingSoon;
            ApplyFill();
        }

        private void Update()
        {
            if (Mathf.Approximately(_shown, _target)) return;
            _shown = Mathf.MoveTowards(_shown, _target, Time.unscaledDeltaTime * 0.8f);
            ApplyFill();
        }

        private void ApplyFill()
        {
            if (barFill != null) barFill.fillAmount = _shown;
        }
    }
}
