using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VamosAprendiendo.UI
{
    /// <summary>
    /// Panel que se muestra al abrir el cofre de una ruta completa: la ruta, la experiencia (XP) que ganó el jugador en ella
    /// y su total. Primero aparece con "..." mientras llega la cifra del servidor; si no se puede consultar (sin conexión)
    /// se felicita sin mostrar un número inventado. Lo arma MissionSignSetupTool (prefab MissionRewardPanel).
    /// </summary>
    public class MissionRewardPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform card;
        [SerializeField] private TMP_Text routeText;
        [SerializeField] private TMP_Text captionText;
        [SerializeField] private TMP_Text xpText;
        [SerializeField] private TMP_Text totalText;
        [SerializeField] private GameObject starIcon;
        [SerializeField] private Button closeButton;

        public event Action Closed;

        private bool _closing;

        /// <summary>Muestra el panel con la animación de entrada; la cifra de XP llega después con <see cref="ShowXp"/>.</summary>
        public void Open(string routeName)
        {
            routeText.text = routeName;
            captionText.text = "Experiencia ganada";
            xpText.text = "...";
            totalText.text = "";
            starIcon.SetActive(false);
            closeButton.interactable = false;
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
            StartCoroutine(Enter());
        }

        public void ShowXp(int routeXp, int totalXp)
        {
            if (routeXp <= 0) { ShowWithoutXp(); return; }
            starIcon.SetActive(true);
            captionText.text = "Experiencia ganada en esta ruta";
            totalText.text = $"Experiencia total: {totalXp} XP";
            closeButton.interactable = true;
            StartCoroutine(CountUp(routeXp));
        }

        /// <summary>Sin conexión o sin cifra: se felicita sin mostrar una cantidad.</summary>
        public void ShowWithoutXp()
        {
            starIcon.SetActive(false);
            captionText.text = "Tu experiencia ya está guardada";
            xpText.text = "¡Lo lograste!";
            totalText.text = "";
            closeButton.interactable = true;
        }

        private void Close()
        {
            if (_closing) return;
            _closing = true;
            StartCoroutine(Exit());
        }

        private IEnumerator Enter()
        {
            group.alpha = 0f;
            card.localScale = Vector3.one * 0.6f;
            const float duration = 0.45f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = t / duration;
                group.alpha = Mathf.Clamp01(k * 2f);
                card.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, EaseOutBack(k));
                yield return null;
            }
            group.alpha = 1f;
            card.localScale = Vector3.one;
        }

        private IEnumerator Exit()
        {
            closeButton.interactable = false;
            const float duration = 0.25f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = t / duration;
                group.alpha = 1f - k;
                card.localScale = Vector3.one * Mathf.Lerp(1f, 0.85f, k);
                yield return null;
            }
            Closed?.Invoke();
            Destroy(gameObject);
        }

        private IEnumerator CountUp(int target)
        {
            const float duration = 1.1f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = 1f - Mathf.Pow(1f - t / duration, 3f);
                xpText.text = $"+{Mathf.RoundToInt(target * k)} XP";
                yield return null;
            }
            xpText.text = $"+{target} XP";
        }

        private static float EaseOutBack(float k)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            k = Mathf.Clamp01(k) - 1f;
            return 1f + c3 * k * k * k + c1 * k * k;
        }
    }
}
