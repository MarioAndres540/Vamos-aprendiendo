using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace VamosAprendiendo.UI
{
    /// <summary>
    /// Gestiona el menú de pausa utilizando directamente la imagen prediseñada (menu_pause.png),
    /// con oscurecimiento de pantalla y botones interactivos sobre 'Reanudar' y 'Salir'.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class PauseMenuUI : MonoBehaviour
    {
        public static PauseMenuUI Instance { get; private set; }

        [Header("UI Containers")]
        [Tooltip("Contenedor raíz del menú de pausa")]
        [SerializeField] private GameObject pauseMenuRoot;
        [Tooltip("Fondo translúcido para oscurecer la pantalla")]
        [SerializeField] private Image dimBackgroundOverlay;
        [Tooltip("Panel o imagen central con la ilustración de pausa")]
        [SerializeField] private RectTransform menuPanel;
        [Tooltip("Imagen principal del menú de pausa")]
        [SerializeField] private Image menuPauseImage;

        [Header("Buttons")]
        [Tooltip("Hitbox interactiva sobre el botón verde 'Reanudar'")]
        [SerializeField] private Button resumeButton;
        [Tooltip("Hitbox interactiva sobre el botón rojo 'Salir'")]
        [SerializeField] private Button mainMenuButton;

        [Header("Animation Settings")]
        [SerializeField] private float transitionDuration = 0.20f;
        [SerializeField] private Color dimOverlayColor = new Color(0.02f, 0.04f, 0.08f, 0.70f);

        [Header("Audio Settings")]
        [SerializeField] private bool muteSfxOnPause = false;

        private CanvasGroup _rootCanvasGroup;
        private bool _isPaused = false;
        private bool _isTransitioning = false;
        private Coroutine _transitionCoroutine;
        private CursorLockMode _previousCursorLockMode = CursorLockMode.None;
        private bool _previousCursorVisible = true;

        public bool IsPaused => _isPaused;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitializeForScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            // No instanciar en la pantalla de login
            if (activeScene.name.ToLowerInvariant().Contains("login"))
                return;

            // Si ya existe un PauseMenuUI en la escena, no duplicar
            if (Instance != null || Object.FindAnyObjectByType<PauseMenuUI>() != null || GameObject.Find("PauseMenuCanvas") != null)
                return;

            CreateRuntimePauseMenuCanvas();
        }

        public static PauseMenuUI CreateRuntimePauseMenuCanvas()
        {
            EnsureEventSystem();

            GameObject canvasObj = new GameObject("PauseMenuCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            CanvasGroup canvasGroup = canvasObj.AddComponent<CanvasGroup>();
            PauseMenuUI pauseUI = canvasObj.AddComponent<PauseMenuUI>();

            // 1. Root de Pausa (Pantalla Completa)
            GameObject rootObj = CreateUIElement("PauseMenu_Root", canvasObj.transform);
            SetFullStretch(rootObj.GetComponent<RectTransform>());

            // 2. Fondo Oscurecido (Dim Overlay)
            GameObject dimObj = CreateUIElement("Dim_Overlay", rootObj.transform);
            SetFullStretch(dimObj.GetComponent<RectTransform>());
            Image dimImg = dimObj.AddComponent<Image>();
            dimImg.color = new Color(0.02f, 0.04f, 0.08f, 0.70f);
            dimImg.raycastTarget = true;

            // 3. Diálogo con la imagen prediseñada (menu_pause.png) grande y centrada
            Sprite pauseSprite = Resources.Load<Sprite>("menu_pause");
            float dialogW = 750f;
            float dialogH = 512f;

            if (pauseSprite != null && pauseSprite.rect.width > 0)
            {
                dialogH = dialogW * (pauseSprite.rect.height / pauseSprite.rect.width);
            }

            GameObject dialogObj = CreateUIElement("MenuPause_Dialog", rootObj.transform);
            RectTransform dialogRect = dialogObj.GetComponent<RectTransform>();
            dialogRect.sizeDelta = new Vector2(dialogW, dialogH);
            dialogRect.anchoredPosition = Vector2.zero;

            Image dialogImg = dialogObj.AddComponent<Image>();
            if (pauseSprite != null)
            {
                dialogImg.sprite = pauseSprite;
                dialogImg.preserveAspect = true;
            }
            dialogImg.raycastTarget = false;

            // 4. Hitbox Interactiva sobre 'Reanudar' (Botón Verde)
            GameObject resumeObj = CreateButtonHitbox("Button_Reanudar", dialogObj.transform, new Vector2(0f, -dialogH * 0.17f), new Vector2(dialogW * 0.44f, dialogH * 0.19f));
            Button resumeBtn = resumeObj.GetComponent<Button>();

            // 5. Hitbox Interactiva sobre 'Salir' (Botón Rojo)
            GameObject exitObj = CreateButtonHitbox("Button_Salir", dialogObj.transform, new Vector2(0f, -dialogH * 0.38f), new Vector2(dialogW * 0.44f, dialogH * 0.19f));
            Button exitBtn = exitObj.GetComponent<Button>();

            // 6. Asignar referencias
            pauseUI.pauseMenuRoot = rootObj;
            pauseUI.dimBackgroundOverlay = dimImg;
            pauseUI.menuPanel = dialogRect;
            pauseUI.menuPauseImage = dialogImg;
            pauseUI.resumeButton = resumeBtn;
            pauseUI.mainMenuButton = exitBtn;

            return pauseUI;
        }

        private static GameObject CreateUIElement(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void SetFullStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static GameObject CreateButtonHitbox(string name, Transform parent, Vector2 localPos, Vector2 size)
        {
            GameObject btnObj = CreateUIElement(name, parent);
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = localPos;

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.18f);
            colors.pressedColor = new Color(0f, 0f, 0f, 0.22f);
            colors.selectedColor = new Color(1f, 1f, 1f, 0.10f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;

            return btnObj;
        }

        private static void EnsureEventSystem()
        {
            UnityEngine.EventSystems.EventSystem existing = UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (existing == null)
            {
                GameObject eventSysObj = new GameObject("EventSystem");
                eventSysObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                var inputModule = eventSysObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                var actionsAsset = Resources.Load<UnityEngine.InputSystem.InputActionAsset>("InputSystem_Actions");
                if (actionsAsset != null)
                {
                    inputModule.actionsAsset = actionsAsset;
                }
                inputModule.AssignDefaultActions();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _rootCanvasGroup = GetComponent<CanvasGroup>();
            if (_rootCanvasGroup == null)
            {
                _rootCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (pauseMenuRoot != null)
            {
                pauseMenuRoot.SetActive(false);
            }
            _rootCanvasGroup.alpha = 0f;
            _rootCanvasGroup.blocksRaycasts = false;
            _rootCanvasGroup.interactable = false;
        }

        private void Start()
        {
            if (resumeButton != null)
                resumeButton.onClick.AddListener(ResumeGame);

            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            bool escapePressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            bool gamepadPause = Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;

            if (escapePressed || gamepadPause)
            {
                TogglePause();
            }
#else
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
#endif
        }

        public void TogglePause()
        {
            if (_isTransitioning) return;

            if (_isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

        public void PauseGame()
        {
            if (_isPaused || _isTransitioning) return;

            _isPaused = true;
            _previousCursorLockMode = Cursor.lockState;
            _previousCursorVisible = Cursor.visible;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (muteSfxOnPause)
            {
                AudioListener.pause = true;
            }

            if (_transitionCoroutine != null)
                StopCoroutine(_transitionCoroutine);

            _transitionCoroutine = StartCoroutine(AnimatePauseMenu(true));
        }

        public void ResumeGame()
        {
            if (!_isPaused || _isTransitioning) return;

            _isPaused = false;

            Cursor.lockState = _previousCursorLockMode;
            Cursor.visible = _previousCursorVisible;

            if (muteSfxOnPause)
            {
                AudioListener.pause = false;
            }

            if (_transitionCoroutine != null)
                StopCoroutine(_transitionCoroutine);

            _transitionCoroutine = StartCoroutine(AnimatePauseMenu(false));
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SceneManager.LoadScene("LoginScene");
        }

        private IEnumerator AnimatePauseMenu(bool show)
        {
            _isTransitioning = true;

            if (show)
            {
                if (pauseMenuRoot != null)
                    pauseMenuRoot.SetActive(true);

                _rootCanvasGroup.blocksRaycasts = true;
                _rootCanvasGroup.interactable = true;
            }

            float startAlpha = _rootCanvasGroup.alpha;
            float targetAlpha = show ? 1f : 0f;

            Vector3 startScale = show ? Vector3.one * 0.85f : Vector3.one;
            Vector3 targetScale = show ? Vector3.one : Vector3.one * 0.85f;

            Color startOverlayColor = dimBackgroundOverlay != null ? dimBackgroundOverlay.color : dimOverlayColor;
            Color targetOverlayColor = show ? dimOverlayColor : new Color(dimOverlayColor.r, dimOverlayColor.g, dimOverlayColor.b, 0f);

            float elapsed = 0f;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                _rootCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, smoothT);

                if (menuPanel != null)
                {
                    menuPanel.localScale = Vector3.Lerp(startScale, targetScale, smoothT);
                }

                if (dimBackgroundOverlay != null)
                {
                    dimBackgroundOverlay.color = Color.Lerp(startOverlayColor, targetOverlayColor, smoothT);
                }

                yield return null;
            }

            _rootCanvasGroup.alpha = targetAlpha;

            if (show)
            {
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = 1f;
                _rootCanvasGroup.blocksRaycasts = false;
                _rootCanvasGroup.interactable = false;

                if (pauseMenuRoot != null)
                    pauseMenuRoot.SetActive(false);
            }

            _isTransitioning = false;
        }

        private void OnDestroy()
        {
            if (_isPaused)
            {
                Time.timeScale = 1f;
                AudioListener.pause = false;
            }
        }
    }
}
