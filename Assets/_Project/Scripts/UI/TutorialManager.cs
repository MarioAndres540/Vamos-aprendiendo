using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VamosAprendiendo.Core;

namespace VamosAprendiendo.UI
{
    /// <summary>
    /// Tutorial de la primera vez en el mundo. Primero una tarjeta de bienvenida con "Aceptar"; después una viñeta por cada control
    /// (joystick, cámara, salto, sonido, pausa) que ilumina el botón y atenúa el resto de la pantalla. El juego queda detenido
    /// (Time.timeScale = 0) y el paso no avanza hasta que el jugador usa justo ese control; el resto de la pantalla no recibe toques.
    /// Se muestra una sola vez por cuenta e hijo; "Saltar tutorial" lo cierra. Toda la interfaz se construye al ejecutar.
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        private const string DonePrefix = "tutorial.done.";

        [Header("Controles que se explican")]
        [SerializeField] private VirtualJoystick joystick;
        [SerializeField] private RectTransform joystickBackground;
        [SerializeField] private RectTransform cameraHint;
        [SerializeField] private Button jumpButton;
        [SerializeField] private Button audioButton;
        [SerializeField] private Button pauseButton;

        [Header("Dibujos")]
        [SerializeField] private Sprite roundSprite;
        [SerializeField] private Sprite circleSprite;
        [SerializeField] private Sprite arrowSprite;
        [SerializeField] private Sprite mascotSprite;

        [Tooltip("Para pruebas: muestra el tutorial aunque ya se haya completado")]
        [SerializeField] private bool showAlways;

        private static readonly Color Dim = new Color(0.07f, 0.05f, 0.2f, 0.74f);
        private static readonly Color Purple = new Color32(0x6B, 0x57, 0xDD, 255);
        private static readonly Color Navy = new Color32(0x1E, 0x1B, 0x5C, 255);
        private static readonly Color Green = new Color32(0x22, 0xC5, 0x5E, 255);
        private static readonly Color Gold = new Color32(0xFF, 0xC9, 0x33, 255);

        private struct Step
        {
            public string title, text;
            public Func<RectTransform> highlight;        // lo que se ilumina
            public Func<RectTransform> passThrough;      // zona que deja pasar los toques (el control mismo)
            public Func<bool> done;
            public Action begin, end;
        }

        private RectTransform _root;
        private RectTransform[] _dim;          // 4 barras visuales alrededor de lo iluminado
        private RectTransform[] _block;        // 4 barras invisibles que detienen los toques fuera del control
        private RectTransform[] _frame;        // marco dorado pulsante
        private RectTransform _bubble, _bubbleArrow, _skip;
        private TMP_Text _bubbleTitle, _bubbleText, _bubbleStep;
        private GameObject _welcome, _finish;
        private Canvas _canvas;
        private bool _running, _accepted, _finished, _skipped;
        private float _dragAccum;
        private bool _jumped, _audioToggled;
        private bool _audioWasEnabled;

        public static bool IsRunning { get; private set; }

        private static string DoneKey()
        {
            var session = PlayerSession.Instance;
            string user = session != null && session.CurrentUser != null && !string.IsNullOrEmpty(session.CurrentUser.id) ? session.CurrentUser.id : "guest";
            string student = session != null && !string.IsNullOrEmpty(session.CurrentStudentId) ? session.CurrentStudentId : "own";
            return $"{DonePrefix}{user}.{student}";
        }

        public static bool IsDone() => PlayerPrefs.GetInt(DoneKey(), 0) == 1;
        public static void ResetDone() { PlayerPrefs.DeleteKey(DoneKey()); PlayerPrefs.Save(); }

        private void Awake()
        {
            BuildUi();
            _root.gameObject.SetActive(false);
        }

        private IEnumerator Start()
        {
            // La sesión puede tardar unos instantes en estar lista al abrir la escena
            float wait = 0f;
            while ((PlayerSession.Instance == null || !PlayerSession.Instance.IsLoggedIn) && wait < 8f)
            {
                wait += 0.25f;
                yield return new WaitForSecondsRealtime(0.25f);
            }
            if (_running || (!showAlways && IsDone())) yield break;
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Run();
        }

        private void OnDestroy()
        {
            if (_running) Finish(false);
        }

        // ---------- guion ----------

        /// <summary>Lanza el tutorial ahora (por ejemplo desde un botón de ayuda).</summary>
        public void Begin()
        {
            if (!_running) StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            _running = true; IsRunning = true; _skipped = false;
            _root.gameObject.SetActive(true);
            SetFrameVisible(false);
            _bubble.gameObject.SetActive(false);
            _skip.gameObject.SetActive(false);
            SetBlockers(Rect.zero, true);

            // 1) Tarjeta de bienvenida
            _accepted = false;
            _welcome.SetActive(true);
            SetDim(null);
            yield return new WaitUntil(() => _accepted);
            _welcome.SetActive(false);

            // 2) Un paso por control
            var steps = BuildSteps();
            _skip.gameObject.SetActive(true);
            for (int i = 0; i < steps.Count && !_skipped; i++)
                yield return RunStep(steps[i], i + 1, steps.Count);

            // 3) Cierre
            if (!_skipped)
            {
                _bubble.gameObject.SetActive(false);
                SetFrameVisible(false);
                _skip.gameObject.SetActive(false);
                _finished = false;
                _finish.SetActive(true);
                SetDim(null);
                SetBlockers(Rect.zero, true);
                yield return new WaitUntil(() => _finished);
                _finish.SetActive(false);
            }
            Finish(true);
        }

        private List<Step> BuildSteps()
        {
            var steps = new List<Step>();
            if (joystick != null && joystickBackground != null)
                steps.Add(new Step
                {
                    title = "Joystick",
                    text = "Apoya el dedo y arrástralo para mover a tu personaje. ¡Pruébalo!",
                    highlight = () => joystickBackground,
                    passThrough = () => (RectTransform)joystick.transform,
                    done = () => joystick.IsPressed && joystick.Value.sqrMagnitude > 0.25f,
                });
            if (cameraHint != null)
                steps.Add(new Step
                {
                    title = "Cámara",
                    text = "Desliza el dedo por la parte derecha de la pantalla para girar la cámara y mirar a tu alrededor.",
                    highlight = () => cameraHint,
                    passThrough = () => cameraHint,
                    begin = () => { _dragAccum = 0f; TouchLookArea.Dragged += OnLookDrag; },
                    end = () => TouchLookArea.Dragged -= OnLookDrag,
                    done = () => _dragAccum >= 160f,
                });
            if (jumpButton != null)
                steps.Add(new Step
                {
                    title = "Saltar",
                    text = "Toca este botón para saltar. ¡Inténtalo!",
                    highlight = () => (RectTransform)jumpButton.transform,
                    passThrough = () => (RectTransform)jumpButton.transform,
                    begin = () => { _jumped = false; jumpButton.onClick.AddListener(OnJump); },
                    end = () => jumpButton.onClick.RemoveListener(OnJump),
                    done = () => _jumped,
                });
            if (audioButton != null)
                steps.Add(new Step
                {
                    title = "Sonido",
                    text = "Con este botón activas o silencias la música y los efectos. Tócalo una vez.",
                    highlight = () => (RectTransform)audioButton.transform,
                    passThrough = () => (RectTransform)audioButton.transform,
                    begin = () => { _audioToggled = false; _audioWasEnabled = GameAudio.Enabled; GameAudio.Changed += OnAudioChanged; },
                    end = () =>
                    {
                        GameAudio.Changed -= OnAudioChanged;
                        if (_audioWasEnabled && !GameAudio.Enabled) GameAudio.SetEnabled(true);   // el tutorial no deja el juego en silencio
                    },
                    done = () => _audioToggled,
                });
            if (pauseButton != null)
                steps.Add(new Step
                {
                    title = "Pausa",
                    text = "Toca aquí para pausar el juego. Cuando quieras seguir, toca el botón de play.",
                    highlight = () => (RectTransform)pauseButton.transform,
                    passThrough = () => (RectTransform)pauseButton.transform,
                    done = () => PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPaused,
                });
            return steps;
        }

        private IEnumerator RunStep(Step step, int number, int total)
        {
            _bubbleTitle.text = step.title;
            _bubbleText.text = step.text;
            _bubbleStep.text = $"Paso {number} de {total}";
            step.begin?.Invoke();

            yield return null;   // un cuadro para que la interfaz tome su tamaño antes de medir
            Canvas.ForceUpdateCanvases();
            Rect target = ToLocalRect(step.highlight());
            Rect hole = ToLocalRect(step.passThrough());
            SetDim(target);
            SetBlockers(hole, false);
            PlaceBubble(target);
            _bubble.gameObject.SetActive(true);
            SetFrameVisible(true);

            while (!_skipped && !step.done())
            {
                PulseFrame(target);
                yield return null;
            }
            step.end?.Invoke();

            // Pausa: mientras el menú está abierto el tutorial se esconde y, al reanudar, sigue
            if (!_skipped && PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPaused && step.title == "Pausa")
            {
                _root.gameObject.SetActive(false);
                yield return new WaitUntil(() => !PauseMenuUI.Instance.IsPaused);
                _root.gameObject.SetActive(true);
            }
        }

        private void OnLookDrag(Vector2 delta) => _dragAccum += delta.magnitude;
        private void OnJump() => _jumped = true;
        private void OnAudioChanged() => _audioToggled = true;

        private void Update()
        {
            if (!_running) return;
            // El juego queda detenido mientras dura el tutorial; el menú de pausa maneja el tiempo por su cuenta
            bool menuPaused = PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPaused;
            if (!menuPaused) Time.timeScale = 0f;
        }

        private void Finish(bool completed)
        {
            _running = false; IsRunning = false;
            if (_root != null) _root.gameObject.SetActive(false);
            bool menuPaused = PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPaused;
            if (!menuPaused) Time.timeScale = 1f;
            if (completed || _skipped)
            {
                PlayerPrefs.SetInt(DoneKey(), 1);
                PlayerPrefs.Save();
            }
        }

        // ---------- geometría ----------

        // Rectángulo de un elemento de la interfaz en coordenadas de la raíz del tutorial (centro = 0,0)
        private Rect ToLocalRect(RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, corners[0], null, out Vector2 min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, corners[2], null, out Vector2 max);
            return Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
        }

        // Cuatro barras alrededor de un hueco; sin hueco, la pantalla entera
        private void Surround(RectTransform[] bars, Rect hole, bool all)
        {
            Rect canvas = _root.rect;
            float W = canvas.width, H = canvas.height;
            if (all)
            {
                for (int i = 0; i < 4; i++) bars[i].gameObject.SetActive(i == 0);
                Inset(bars[0], 0, 0, 0, 0);
                return;
            }
            float x0 = Mathf.Clamp(hole.xMin - canvas.xMin, 0f, W), x1 = Mathf.Clamp(hole.xMax - canvas.xMin, 0f, W);
            float y0 = Mathf.Clamp(hole.yMin - canvas.yMin, 0f, H), y1 = Mathf.Clamp(hole.yMax - canvas.yMin, 0f, H);
            for (int i = 0; i < 4; i++) bars[i].gameObject.SetActive(true);
            Inset(bars[0], 0, y1, 0, 0);                  // arriba
            Inset(bars[1], 0, 0, 0, H - y0);              // abajo
            Inset(bars[2], 0, y0, W - x0, H - y1);        // izquierda
            Inset(bars[3], x1, y0, 0, H - y1);            // derecha
        }

        // left, bottom, right, top medidos desde cada borde del lienzo
        private static void Inset(RectTransform rt, float left, float bottom, float right, float top)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        private void SetDim(Rect? hole)
        {
            if (hole.HasValue)
            {
                Rect h = hole.Value;
                h.xMin -= 14f; h.yMin -= 14f; h.xMax += 14f; h.yMax += 14f;
                Surround(_dim, h, false);
            }
            else Surround(_dim, Rect.zero, true);
        }

        private void SetBlockers(Rect hole, bool all)
        {
            if (!all) { hole.xMin -= 14f; hole.yMin -= 14f; hole.xMax += 14f; hole.yMax += 14f; }
            Surround(_block, hole, all);
        }

        private void SetFrameVisible(bool visible)
        {
            foreach (var f in _frame) f.gameObject.SetActive(visible);
        }

        private void PulseFrame(Rect target)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);
            float grow = 14f + pulse * 8f;
            Rect r = Rect.MinMaxRect(target.xMin - grow, target.yMin - grow, target.xMax + grow, target.yMax + grow);
            const float t = 7f;
            Place(_frame[0], r.xMin, r.yMax - t, r.width, t);
            Place(_frame[1], r.xMin, r.yMin, r.width, t);
            Place(_frame[2], r.xMin, r.yMin, t, r.height);
            Place(_frame[3], r.xMax - t, r.yMin, t, r.height);
            var color = new Color(Gold.r, Gold.g, Gold.b, 0.6f + 0.4f * pulse);
            foreach (var f in _frame) f.GetComponent<Image>().color = color;
        }

        private static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x + w * 0.5f, y + h * 0.5f);
        }

        // La viñeta va al lado del control con más espacio y una flecha que apunta a él
        private void PlaceBubble(Rect target)
        {
            Rect canvas = _root.rect;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_bubble);
            float w = _bubble.rect.width, h = _bubble.rect.height;
            bool toRight = target.center.x < 0f;
            float gap = 64f;
            float x = toRight ? target.xMax + gap + w * 0.5f : target.xMin - gap - w * 0.5f;
            x = Mathf.Clamp(x, canvas.xMin + w * 0.5f + 24f, canvas.xMax - w * 0.5f - 24f);
            float y = Mathf.Clamp(target.center.y, canvas.yMin + h * 0.5f + 24f, canvas.yMax - h * 0.5f - 24f);
            _bubble.anchoredPosition = new Vector2(x, y);

            // Flecha pegada al borde de la viñeta que mira al control, a su altura
            float arrowY = Mathf.Clamp(target.center.y - y, -h * 0.5f + 50f, h * 0.5f - 50f);
            _bubbleArrow.anchorMin = _bubbleArrow.anchorMax = new Vector2(toRight ? 0f : 1f, 0.5f);
            _bubbleArrow.anchoredPosition = new Vector2(toRight ? -22f : 22f, arrowY);
            _bubbleArrow.localRotation = Quaternion.Euler(0f, 0f, toRight ? 180f : 0f);
        }

        // ---------- construcción de la interfaz ----------

        private void BuildUi()
        {
            var canvasGo = new GameObject("TutorialCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 300;   // sobre todos los controles (la pausa y el audio usan 150)
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            canvasGo.AddComponent<GraphicRaycaster>();
            _root = (RectTransform)canvasGo.transform;

            _dim = MakeBars("Dim", Dim, false);
            _block = MakeBars("Block", new Color(0f, 0f, 0f, 0f), true);

            _frame = new RectTransform[4];
            for (int i = 0; i < 4; i++)
            {
                var f = NewRect("Frame" + i, _root);
                var img = f.gameObject.AddComponent<Image>();
                img.color = Gold; img.raycastTarget = false;
                _frame[i] = f;
            }

            BuildBubble();
            BuildSkip();
            _welcome = BuildCard("Welcome",
                "¡Bienvenido a Vamos aprendiendo!",
                "Una plataforma para grandes y pequeños que quieren mejorar sus capacidades.\n\n" +
                "Los más pequeños aprenderán desde los números y las letras hasta distinguir objetos, formar frases completas y resolver operaciones matemáticas.\n\n" +
                "Para los más grandes habrá una serie de retos que ejercitan la actividad cognitiva, para mantener el cerebro muy activo cada día.",
                "Aceptar", () => _accepted = true, 1320f, 800f, true);
            _finish = BuildCard("Finish",
                "¡Listo!",
                "Ya conoces los controles. Explora el mundo y acércate al pulpo para empezar a aprender.",
                "¡Vamos!", () => _finished = true, 1100f, 560f, true);
            _welcome.SetActive(false);
            _finish.SetActive(false);
        }

        private RectTransform[] MakeBars(string name, Color color, bool raycast)
        {
            var bars = new RectTransform[4];
            for (int i = 0; i < 4; i++)
            {
                var b = NewRect(name + i, _root);
                var img = b.gameObject.AddComponent<Image>();
                img.color = color;
                img.raycastTarget = raycast;
                bars[i] = b;
            }
            return bars;
        }

        private void BuildBubble()
        {
            _bubble = NewRect("Bubble", _root);
            _bubble.anchorMin = _bubble.anchorMax = _bubble.pivot = new Vector2(0.5f, 0.5f);
            _bubble.sizeDelta = new Vector2(660f, 200f);
            var bg = _bubble.gameObject.AddComponent<Image>();
            bg.sprite = roundSprite; bg.type = Image.Type.Sliced; bg.color = Color.white; bg.raycastTarget = false;
            var layout = _bubble.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(34, 34, 28, 26);
            layout.spacing = 10f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            var fitter = _bubble.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _bubbleTitle = Label("Title", _bubble, 48, FontStyles.Bold, Purple, TextAlignmentOptions.TopLeft);
            _bubbleText = Label("Text", _bubble, 36, FontStyles.Normal, Navy, TextAlignmentOptions.TopLeft);
            _bubbleStep = Label("Step", _bubble, 26, FontStyles.Bold, new Color32(0x8A, 0x8F, 0xB5, 255), TextAlignmentOptions.TopLeft);

            _bubbleArrow = NewRect("Arrow", _bubble);
            _bubbleArrow.sizeDelta = new Vector2(56f, 56f);
            var ignore = _bubbleArrow.gameObject.AddComponent<LayoutElement>();
            ignore.ignoreLayout = true;
            var arrow = _bubbleArrow.gameObject.AddComponent<Image>();
            arrow.sprite = arrowSprite; arrow.color = Color.white; arrow.raycastTarget = false;
        }

        private void BuildSkip()
        {
            _skip = NewRect("Skip", _root);
            // Arriba al centro: abajo chocaría con las viñetas de los botones de salto y joystick
            _skip.anchorMin = _skip.anchorMax = _skip.pivot = new Vector2(0.5f, 1f);
            _skip.anchoredPosition = new Vector2(0f, -36f);
            _skip.sizeDelta = new Vector2(380f, 76f);
            var img = _skip.gameObject.AddComponent<Image>();
            img.sprite = roundSprite; img.type = Image.Type.Sliced; img.color = new Color(1f, 1f, 1f, 0.9f);
            var btn = _skip.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => _skipped = true);
            var t = Label("T", _skip, 32, FontStyles.Bold, Purple, TextAlignmentOptions.Center);
            t.text = "Saltar tutorial";
            Stretch(t.rectTransform);
        }

        // Tarjeta grande centrada: mascota a la izquierda, título + texto y un botón verde
        private GameObject BuildCard(string name, string title, string body, string button, Action onClick, float width, float height, bool mascot)
        {
            var holder = NewRect(name, _root);
            Stretch(holder);
            var shade = NewRect("Shade", holder);
            Stretch(shade);
            var shadeImg = shade.gameObject.AddComponent<Image>();
            shadeImg.color = new Color(0f, 0f, 0f, 0f); shadeImg.raycastTarget = true;   // el oscurecido lo dan las barras de Dim

            var card = NewRect("Card", holder);
            card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(width, height);
            var cardImg = card.gameObject.AddComponent<Image>();
            cardImg.sprite = roundSprite; cardImg.type = Image.Type.Sliced; cardImg.color = Color.white;

            var header = NewRect("Header", card);
            header.anchorMin = new Vector2(0f, 1f); header.anchorMax = Vector2.one;
            header.pivot = new Vector2(0.5f, 1f);
            header.offsetMin = new Vector2(0f, -120f); header.offsetMax = Vector2.zero;
            var headerImg = header.gameObject.AddComponent<Image>();
            headerImg.sprite = roundSprite; headerImg.type = Image.Type.Sliced; headerImg.color = Purple;
            var titleText = Label("Title", header, 58, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            titleText.text = title;
            Stretch(titleText.rectTransform);

            float textLeft = 60f;
            if (mascot && mascotSprite != null)
            {
                var m = NewRect("Mascot", card);
                m.anchorMin = m.anchorMax = new Vector2(0f, 0.5f);
                m.pivot = new Vector2(0f, 0.5f);
                m.sizeDelta = new Vector2(380f, 380f);
                m.anchoredPosition = new Vector2(40f, -20f);
                var mi = m.gameObject.AddComponent<Image>();
                mi.sprite = mascotSprite; mi.preserveAspect = true; mi.raycastTarget = false;
                textLeft = 450f;
            }

            var bodyText = Label("Body", card, 36, FontStyles.Normal, Navy, TextAlignmentOptions.TopLeft);
            bodyText.text = body;
            bodyText.lineSpacing = 6f;
            var br = bodyText.rectTransform;
            br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one;
            br.offsetMin = new Vector2(textLeft, 150f);
            br.offsetMax = new Vector2(-60f, -150f);

            var buttonRt = NewRect("Button", card);
            buttonRt.anchorMin = buttonRt.anchorMax = buttonRt.pivot = new Vector2(0.5f, 0f);
            buttonRt.anchoredPosition = new Vector2(0f, 36f);
            buttonRt.sizeDelta = new Vector2(380f, 96f);
            var bi = buttonRt.gameObject.AddComponent<Image>();
            bi.sprite = roundSprite; bi.type = Image.Type.Sliced; bi.color = Green;
            var btn = buttonRt.gameObject.AddComponent<Button>();
            btn.targetGraphic = bi;
            btn.onClick.AddListener(() => onClick());
            var bt = Label("T", buttonRt, 46, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            bt.text = button;
            Stretch(bt.rectTransform);
            return holder.gameObject;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static TMP_Text Label(string name, Transform parent, float size, FontStyles style, Color color, TextAlignmentOptions align)
        {
            var rt = NewRect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.fontSize = size; t.fontStyle = style; t.color = color; t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            return t;
        }
    }
}
