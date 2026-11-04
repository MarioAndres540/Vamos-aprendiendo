using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace VamosAprendiendo.UI
{
    public class LoginUI : MonoBehaviour
    {
        [Header("Scene Navigation")]
        [SerializeField] private string targetSceneName = "BaseScene";
        [Tooltip("Escena para el contenido de niños (audience = ninos). Vacío: se usa targetSceneName.")]
        [SerializeField] private string kidsSceneName = "";
        [Tooltip("Escena para el contenido de adultos (audience = adultos). Vacío: se usa targetSceneName.")]
        [SerializeField] private string adultsSceneName = "";
        [SerializeField] private float minimumLoadingDuration = 2.2f;

        [Header("Inputs & Controls")]
        [SerializeField] private TMP_InputField usernameInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button togglePasswordButton;
        [SerializeField] private Image passwordToggleIcon;
        [SerializeField] private Button forgotPasswordButton;
        [SerializeField] private Button googleLoginButton;
        [SerializeField] private Button appleLoginButton;
        [SerializeField] private Button registerButton;
        [Tooltip("Página web donde se hace el registro (el tutor llena sus datos y los de su hijo). Se abre en el navegador del dispositivo. Vacío: aún no está publicada.")]
        [SerializeField] private string registerUrl = "";

        [Header("Feedback / Toast")]
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private CanvasGroup feedbackCanvasGroup;

        [Header("Loading Overlay & Progress Bar")]
        [SerializeField] private GameObject loadingOverlay;
        [SerializeField] private CanvasGroup loadingCanvasGroup;
        [SerializeField] private RectTransform spinnerTransform;
        [SerializeField] private Image progressBarFill;
        [SerializeField] private TMP_Text progressPercentageText;
        [SerializeField] private TMP_Text loadingStatusText;

        private bool _isPasswordVisible = false;
        private bool _isLoading = false;

        private void Start()
        {
            if (loginButton != null)
                loginButton.onClick.AddListener(OnLoginClicked);

            if (usernameInput != null)
                usernameInput.onSubmit.AddListener((_) => { if (!_isLoading) OnLoginClicked(); });

            if (passwordInput != null)
                passwordInput.onSubmit.AddListener((_) => { if (!_isLoading) OnLoginClicked(); });

            if (togglePasswordButton != null)
                togglePasswordButton.onClick.AddListener(TogglePasswordVisibility);

            if (forgotPasswordButton != null)
                forgotPasswordButton.onClick.AddListener(() => ShowFeedback("Función de recuperación enviada a tu correo."));

            if (googleLoginButton != null)
                googleLoginButton.onClick.AddListener(() => QuickLogin("Google"));

            if (appleLoginButton != null)
                appleLoginButton.onClick.AddListener(() => QuickLogin("Apple"));

            if (registerButton != null)
                registerButton.onClick.AddListener(OpenRegistration);

            if (feedbackCanvasGroup != null)
            {
                feedbackCanvasGroup.alpha = 0f;
            }

            // Ocultar overlay de carga al inicio
            if (loadingOverlay != null)
            {
                loadingOverlay.SetActive(false);
            }
            if (loadingCanvasGroup != null)
            {
                loadingCanvasGroup.alpha = 0f;
                loadingCanvasGroup.blocksRaycasts = false;
            }
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
                {
                    if (!_isLoading) OnLoginClicked();
                }
            }
#endif

            // Rotación constante del spinner cuando está activo
            if (_isLoading && spinnerTransform != null)
            {
                spinnerTransform.Rotate(0f, 0f, -360f * Time.deltaTime);
            }
        }

        public void OnLoginClicked()
        {
            if (_isLoading) return;

            string email = usernameInput != null ? usernameInput.text.Trim() : "";
            string password = passwordInput != null ? passwordInput.text : "";

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowFeedback("Ingresa tu correo y contraseña.");
                return;
            }

            StartCoroutine(PerformLoginSequence(email, password));
        }

        private void QuickLogin(string provider)
        {
            if (_isLoading) return;
            ShowFeedback($"El inicio de sesión con {provider} estará disponible próximamente.");
        }

        private IEnumerator PerformLoginSequence(string email, string password)
        {
            _isLoading = true;

            if (loginButton != null)
                StartCoroutine(ButtonPulseRoutine(loginButton.transform));

            yield return new WaitForSeconds(0.15f);

            // --- INICIO: bloque nuevo, llamada real al backend ---
            bool loginOk = false;
            string loginError = null;

            AuthService.Instance.Login(email, password,
                onSuccess: (response) => { loginOk = true; },
                onError: (error) => { loginError = error; });

            yield return new WaitUntil(() => loginOk || loginError != null);

            if (!loginOk)
            {
                _isLoading = false;
                ShowFeedback(loginError ?? "No se pudo iniciar sesión. Verifica tus datos.");
                yield break;
            }
            
            // El tutor juega con un hijo: elegirlo antes de abrir la sesión para que quede a su nombre
            yield return StudentsService.PrepareCurrentStudent();

            if (PresenceService.Instance != null)
                PresenceService.Instance.StartAppSession();

            // Escena según el contenido que corresponde al perfil (niños o adultos)
            string sceneToLoad = ResolveTargetScene();

            // Activar y mostrar pantalla de carga
            if (loadingOverlay != null)
            {
                loadingOverlay.SetActive(true);
            }

            if (progressBarFill != null)
            {
                progressBarFill.fillAmount = 0f;
            }

            if (progressPercentageText != null)
            {
                progressPercentageText.text = "0%";
            }

            if (loadingStatusText != null)
            {
                // CORREGIDO: "userName" ya no existe; usamos el email real del usuario autenticado.
                string displayName = PlayerSession.Instance.CurrentUser != null
                    ? PlayerSession.Instance.CurrentUser.email
                    : email;
                loadingStatusText.text = $"Preparando aventura para {displayName}...";
            }

            // Fade in del overlay de carga
            if (loadingCanvasGroup != null)
            {
                loadingCanvasGroup.blocksRaycasts = true;
                float fadeElapsed = 0f;
                float fadeDuration = 0.35f;
                while (fadeElapsed < fadeDuration)
                {
                    fadeElapsed += Time.deltaTime;
                    loadingCanvasGroup.alpha = Mathf.Lerp(0f, 1f, fadeElapsed / fadeDuration);
                    yield return null;
                }
                loadingCanvasGroup.alpha = 1f;
            }

            // Con el login hecho se baja el progreso de las actividades (letras y números) para que la pantalla de
            // actividades lo muestre al instante. Si no hay red se sigue con lo guardado en el dispositivo.
            if (loadingStatusText != null) loadingStatusText.text = "Descargando tu progreso...";
            yield return ProgressSyncService.Fetch();

            // Iniciar carga asíncrona de la escena
            AsyncOperation asyncOp = null;
            if (!string.IsNullOrEmpty(sceneToLoad) && Application.CanStreamedLevelBeLoaded(sceneToLoad))
            {
                asyncOp = SceneManager.LoadSceneAsync(sceneToLoad);
            }
            else if (SceneManager.sceneCountInBuildSettings > 1)
            {
                asyncOp = SceneManager.LoadSceneAsync(1);
            }
            else
            {
                asyncOp = SceneManager.LoadSceneAsync(0);
            }

            if (asyncOp != null)
            {
                asyncOp.allowSceneActivation = false;
            }

            // Simulación suave de progreso 0% -> 100%
            float elapsed = 0f;
            float simulatedProgress = 0f;

            string[] loadingTips = new string[]
            {
                "Conectando con el aula virtual...",
                "Cargando mapa 3D y entorno interactivo...",
                "Configurando desafíos de matemáticas y memoria...",
                "¡Todo listo! Ingresando al mundo..."
            };

            while (simulatedProgress < 1.0f)
            {
                elapsed += Time.deltaTime;
                float timeRatio = Mathf.Clamp01(elapsed / minimumLoadingDuration);

                // Progreso real de Unity va de 0 a 0.9 cuando allowSceneActivation es false
                float realProgress = asyncOp != null ? Mathf.Clamp01(asyncOp.progress / 0.9f) : 1f;

                // El progreso simulado avanza de manera suave con interpolación
                float targetProgress = Mathf.Min(timeRatio, realProgress);
                simulatedProgress = Mathf.MoveTowards(simulatedProgress, targetProgress, Time.deltaTime * 0.8f);

                // Si ya pasó el tiempo mínimo y Unity terminó la carga, completar al 100%
                if (elapsed >= minimumLoadingDuration && realProgress >= 0.99f)
                {
                    simulatedProgress = Mathf.MoveTowards(simulatedProgress, 1.0f, Time.deltaTime * 3.0f);
                }

                if (progressBarFill != null)
                {
                    progressBarFill.fillAmount = simulatedProgress;
                }

                if (progressPercentageText != null)
                {
                    int pct = Mathf.RoundToInt(simulatedProgress * 100f);
                    progressPercentageText.text = $"{pct}%";
                }

                if (loadingStatusText != null)
                {
                    int tipIndex = Mathf.Clamp(Mathf.FloorToInt(simulatedProgress * loadingTips.Length), 0, loadingTips.Length - 1);
                    loadingStatusText.text = loadingTips[tipIndex];
                }

                yield return null;
            }

            // Asegurar 100% visual
            if (progressBarFill != null) progressBarFill.fillAmount = 1f;
            if (progressPercentageText != null) progressPercentageText.text = "100%";
            if (loadingStatusText != null) loadingStatusText.text = "¡Bienvenido a Vamos Aprendiendo!";

            // Breve pausa para que el usuario aprecie el 100% completado
            yield return new WaitForSeconds(0.35f);

            // CORREGIDO: liberar el bloqueo de _isLoading también en el camino exitoso.
            _isLoading = false;

            // Activar la escena cargada
            if (asyncOp != null)
            {
                asyncOp.allowSceneActivation = true;
            }
            else
            {
                SceneManager.LoadScene(sceneToLoad);
            }
        }

        /// <summary>
        /// Escena de destino según PlayerSession.Audience: niños o adultos si están configuradas,
        /// si no (o si el tester aún no elige versión) la escena general.
        /// </summary>
        private string ResolveTargetScene()
        {
            var session = PlayerSession.Instance;
            if (session != null)
            {
                if (session.IsKidsAudience && !string.IsNullOrEmpty(kidsSceneName)) return kidsSceneName;
                if (session.IsAdultsAudience && !string.IsNullOrEmpty(adultsSceneName)) return adultsSceneName;
            }
            return targetSceneName;
        }

        /// <summary>
        /// El registro se hace en la página web: el tutor llena el formulario con sus datos y los de su hijo y después inicia
        /// sesión aquí. Abre esa página en el navegador predeterminado del dispositivo.
        /// </summary>
        private void OpenRegistration()
        {
            bool valid = !string.IsNullOrWhiteSpace(registerUrl)
                         && (registerUrl.StartsWith("https://") || registerUrl.StartsWith("http://"));
            if (!valid)
            {
                ShowFeedback("El registro se hace en nuestra página web. Estará disponible muy pronto.");
                return;
            }
            ShowFeedback("Abriendo el registro en tu navegador. Cuando termines, inicia sesión aquí.");
            Application.OpenURL(registerUrl.Trim());
        }

        public void TogglePasswordVisibility()
        {
            if (passwordInput == null) return;

            _isPasswordVisible = !_isPasswordVisible;
            passwordInput.contentType = _isPasswordVisible
                ? TMP_InputField.ContentType.Standard
                : TMP_InputField.ContentType.Password;

            passwordInput.ForceLabelUpdate();

            if (passwordToggleIcon != null)
            {
                passwordToggleIcon.color = _isPasswordVisible
                    ? new Color(0.2f, 0.45f, 0.9f, 1f)
                    : new Color(0.5f, 0.5f, 0.6f, 0.8f);
            }
        }

        public void ShowFeedback(string message)
        {
            if (feedbackText != null) feedbackText.text = message;
            if (feedbackCanvasGroup != null)
            {
                StopCoroutine("FeedbackRoutine");
                StartCoroutine(FeedbackRoutine());
            }
        }

        private IEnumerator FeedbackRoutine()
        {
            float elapsed = 0f;
            while (elapsed < 0.25f)
            {
                elapsed += Time.deltaTime;
                feedbackCanvasGroup.alpha = elapsed / 0.25f;
                yield return null;
            }
            feedbackCanvasGroup.alpha = 1f;

            yield return new WaitForSeconds(2.5f);

            elapsed = 0f;
            while (elapsed < 0.35f)
            {
                elapsed += Time.deltaTime;
                feedbackCanvasGroup.alpha = 1f - (elapsed / 0.35f);
                yield return null;
            }
            feedbackCanvasGroup.alpha = 0f;
        }

        private IEnumerator ButtonPulseRoutine(Transform btnTransform)
        {
            Vector3 original = btnTransform.localScale;
            btnTransform.localScale = original * 0.95f;
            yield return new WaitForSeconds(0.1f);
            btnTransform.localScale = original;
        }
    }
}