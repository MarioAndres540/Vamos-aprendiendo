using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace VamosAprendiendo.UI
{
    /// <summary>
    /// Menú principal, entre el login y el mundo: Jugar, Instructivo (cómo se juega y los controles), Créditos y Cerrar sesión.
    /// El instructivo puede volver a lanzar el tutorial del juego.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Escenas")]
        [SerializeField] private string playScene = "BaseScene";
        [SerializeField] private string loginScene = "LoginScene";

        [Header("Botones")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button howToButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button logoutButton;

        [Header("Paneles")]
        [SerializeField] private GameObject howToPanel;
        [SerializeField] private Button howToCloseButton;
        [SerializeField] private Button replayTutorialButton;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private Button creditsCloseButton;

        [Header("Saludo")]
        [SerializeField] private TMP_Text greetingLabel;

        private void Awake()
        {
            if (playButton != null) playButton.onClick.AddListener(() => Play(false));
            if (howToButton != null) howToButton.onClick.AddListener(() => Show(howToPanel, true));
            if (creditsButton != null) creditsButton.onClick.AddListener(() => Show(creditsPanel, true));
            if (howToCloseButton != null) howToCloseButton.onClick.AddListener(() => Show(howToPanel, false));
            if (creditsCloseButton != null) creditsCloseButton.onClick.AddListener(() => Show(creditsPanel, false));
            if (replayTutorialButton != null) replayTutorialButton.onClick.AddListener(() => Play(true));
            if (logoutButton != null) logoutButton.onClick.AddListener(Logout);
            Show(howToPanel, false);
            Show(creditsPanel, false);
        }

        private void Start()
        {
            Time.timeScale = 1f;
            if (greetingLabel != null) greetingLabel.text = $"¡Hola, {PlayerHudUI.ResolveName()}!";
        }

        private static void Show(GameObject panel, bool visible)
        {
            if (panel != null) panel.SetActive(visible);
        }

        private void Play(bool showTutorial)
        {
            if (LoadingOverlay.IsBusy) return;
            // El tutorial se muestra una sola vez por cuenta: pedirlo desde el instructivo lo deja pendiente otra vez
            if (showTutorial) TutorialManager.ResetDone();
            LoadingOverlay.LoadScene(playScene, "Preparando tu aventura...", showCard: true, beforeLoad: ProgressSyncService.Fetch(), minSeconds: 0.8f);
        }

        private void Logout()
        {
            if (LoadingOverlay.IsBusy) return;
            // Igual que "Salir" del menú de pausa: cierra la sesión de la app y vuelve al login
            if (AuthService.Instance != null) AuthService.Instance.Logout();
            SceneManager.LoadScene(loginScene);
        }

        private void Update()
        {
            // Esc / atrás de Android cierra el panel abierto
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (howToPanel != null && howToPanel.activeSelf) Show(howToPanel, false);
                else if (creditsPanel != null && creditsPanel.activeSelf) Show(creditsPanel, false);
            }
        }
    }
}
