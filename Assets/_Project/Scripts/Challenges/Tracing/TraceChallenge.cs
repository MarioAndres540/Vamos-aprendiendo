using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VamosAprendiendo.UI;

namespace VamosAprendiendo.Challenges.Tracing
{
    /// <summary>
    /// Reto 2D de trazado (letras o números): menú de selección con bloqueo por progreso + pantalla de trazo.
    /// </summary>
    public class TraceChallenge : MonoBehaviour
    {
        [Header("Modo")]
        [SerializeField] private bool numbersMode;
        [SerializeField] private string returnSceneName = "BaseScene";

        [Header("Menú")]
        [SerializeField] private GameObject menuRoot;
        [SerializeField] private RectTransform gridRoot;
        [SerializeField] private GameObject tabsRoot;
        [SerializeField] private Button tabVowelsButton;
        [SerializeField] private Button tabConsonantsButton;
        [SerializeField] private Sprite roundedSprite;

        [Header("Trazo")]
        [SerializeField] private GameObject traceRoot;
        [SerializeField] private RectTransform drawArea;
        [SerializeField] private TraceStrokeGraphic guideGraphic;
        [SerializeField] private TraceStrokeGraphic userGraphic;
        [SerializeField] private TraceStrokeGraphic previewGraphic;
        [SerializeField] private RectTransform markersRoot;
        [SerializeField] private Sprite circleSprite;

        [Header("Textos")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text glyphTitleText;
        [SerializeField] private TMP_Text stepsText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private Image feedbackBanner;

        [Header("Botones")]
        [SerializeField] private Button clearButton;
        [SerializeField] private Button checkButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text nextLabel;
        [SerializeField] private Button backButton;

        [Header("Pantalla de trazo (diseño nuevo, opcional)")]
        [Tooltip("Adornos del menú (mascota grande, título) que se ocultan mientras se traza: la barra nueva los reemplaza")]
        [SerializeField] private GameObject[] hideWhileTracing;
        [SerializeField] private TMP_Text headerTitle;
        [SerializeField] private Button traceBackButton;
        [SerializeField] private Image[] stepChips;          // MIRA, TRAZA, PRACTICA
        [SerializeField] private Color chipOnColor = Color.white;
        [SerializeField] private Color chipOffColor = new Color(1f, 1f, 1f, 0.55f);
        [SerializeField] private float chipOnScale = 1.12f;
        [SerializeField] private Button startButton;         // Comenzar a trazar
        [SerializeField] private Button repeatButton;        // Repetir modelo
        [SerializeField] private Button listenButton;
        [SerializeField] private TMP_Text listenLabel;
        [SerializeField] private TMP_Text stepPillText;
        [SerializeField] private RectTransform instructionsList;
        [SerializeField] private TraceSoundPanel soundPanel;
        [SerializeField] private Sprite arrowSprite;

        [Tooltip("Fracción del lado menor de la zona de dibujo que ocupa la forma")]
        [Range(0.5f, 1f)]
        [SerializeField] private float glyphScale = 0.8f;

        private static readonly Color BannerInfo = new Color(0.25f, 0.45f, 0.9f);
        private static readonly Color BannerFail = new Color(0.97f, 0.5f, 0.15f);
        private static readonly Color BannerOk = new Color(0.3f, 0.75f, 0.25f);
        private static readonly Color CellPending = new Color(0.25f, 0.45f, 0.9f);
        private static readonly Color CellDone = new Color(0.3f, 0.72f, 0.25f);
        private static readonly Color CellLocked = new Color(0.55f, 0.58f, 0.66f);

        private readonly List<List<Vector2>> _userStrokes = new List<List<Vector2>>();
        private TraceGroup _group;
        private TraceCase _case = TraceCase.Both;
        private bool _startOnConsonants;
        private TraceItem _item;
        private int _glyphIndex;
        private bool _completed;
        private bool _rejecting;
        private List<Vector2> _templateSamples = new List<Vector2>();

        private enum Phase { Watch, Trace, Practice }
        private Phase _phase;
        private bool _demoRunning;
        private Coroutine _demo;
        private RectTransform _demoDot;
        private bool NewUi => startButton != null;
        private bool InputAllowed => !NewUi || (_phase != Phase.Watch && !_demoRunning);

        private static readonly Color[] StrokeColors =
        {
            new Color32(0x22, 0xB0, 0x4E, 255), new Color32(0xF5, 0x9A, 0x0E, 255), new Color32(0x3B, 0x82, 0xF6, 255),
            new Color32(0xD9, 0x46, 0xA8, 255), new Color32(0x8B, 0x5C, 0xF6, 255), new Color32(0x14, 0xB8, 0xA6, 255)
        };

        private List<TraceGlyph> ItemGlyphs => TraceProgress.ActiveGlyphs(_item, _case);
        private TraceGlyph CurrentGlyph => ItemGlyphs[_glyphIndex];

        private void Awake()
        {
            // La pantalla de actividades indica si se trazan solo minúsculas o mayúsculas y a dónde volver.
            var route = ActivityRoute.Take();
            _case = route.traceCase;
            if (!string.IsNullOrEmpty(route.returnScene)) returnSceneName = route.returnScene;
            _startOnConsonants = ActivityRoute.TakeStartOnConsonants();

            if (clearButton != null) clearButton.onClick.AddListener(ClearUserTrace);
            if (checkButton != null) checkButton.onClick.AddListener(CheckTrace);
            if (nextButton != null) nextButton.onClick.AddListener(OnNext);
            if (backButton != null) backButton.onClick.AddListener(OnBack);
            if (traceBackButton != null) traceBackButton.onClick.AddListener(OnBack);
            if (startButton != null) startButton.onClick.AddListener(OnStartTracing);
            if (repeatButton != null) repeatButton.onClick.AddListener(PlayDemo);
            if (listenButton != null) listenButton.onClick.AddListener(OnListen);
            if (tabVowelsButton != null) tabVowelsButton.onClick.AddListener(() => ShowMenu(TraceGroup.Vowels));
            if (tabConsonantsButton != null) tabConsonantsButton.onClick.AddListener(() => ShowMenu(TraceGroup.Consonants));
        }

        private void Start()
        {
            ShowMenu(numbersMode ? TraceGroup.Digits : (_startOnConsonants ? TraceGroup.Consonants : TraceGroup.Vowels));
        }

        // ---------- menú ----------

        public void ShowMenu(TraceGroup group)
        {
            _group = _case == TraceCase.Both ? group : TraceGroup.Vowels;
            _item = null;
            menuRoot.SetActive(true);
            traceRoot.SetActive(false);
            if (tabsRoot != null) tabsRoot.SetActive(!numbersMode && _case == TraceCase.Both);

            StopDemo();
            if (hideWhileTracing != null) foreach (var go in hideWhileTracing) if (go != null) go.SetActive(true);
            if (NewUi) SetMenuChromeVisible(true);
            SetTitle(MenuTitle());
            SetTabColors();
            BuildGrid();
        }

        private string MenuTitle()
        {
            if (numbersMode) return "¡Elige un <color=#F5A000>número</color>!";
            switch (_case)
            {
                case TraceCase.Lowercase: return "¡Aprende a hacer las <color=#F5A000>vocales</color>!";
                case TraceCase.Uppercase: return "¡Ahora las vocales en <color=#F5A000>mayúsculas</color>!";
                default: return "¡Elige una <color=#F5A000>letra</color>!";
            }
        }

        private void SetTabColors()
        {
            if (tabVowelsButton != null)
                tabVowelsButton.targetGraphic.color = _group == TraceGroup.Vowels ? CellPending : CellLocked;
            if (tabConsonantsButton != null)
                tabConsonantsButton.targetGraphic.color = _group == TraceGroup.Consonants ? CellPending : CellLocked;
        }

        private void BuildGrid()
        {
            for (int i = gridRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(gridRoot.GetChild(i).gameObject);
            }

            var grid = gridRoot.GetComponent<GridLayoutGroup>();
            bool dense = _group == TraceGroup.Consonants;
            if (grid != null)
            {
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = dense ? 8 : 5;
                grid.cellSize = dense ? new Vector2(170f, 170f) : new Vector2(200f, 200f);
                grid.spacing = dense ? new Vector2(24f, 22f) : new Vector2(34f, 34f);
            }

            foreach (var item in TraceCatalog.GetGroup(_group))
            {
                CreateCell(item, dense);
            }
        }

        private void CreateCell(TraceItem item, bool dense)
        {
            bool unlocked = TraceProgress.IsUnlocked(item, _case);
            bool done = TraceProgress.IsItemDone(item, _case);

            var go = new GameObject($"Cell_{item.Id}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(gridRoot, false);

            var img = go.GetComponent<Image>();
            img.sprite = roundedSprite;
            img.type = Image.Type.Sliced;
            img.color = !unlocked ? CellLocked : (done ? CellDone : CellPending);

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.interactable = unlocked;
            btn.onClick.AddListener(() => StartItem(item));

            var main = NewLabel("Main", go.transform, item.Display, dense ? 64 : 84, FontStyles.Bold);
            main.rectTransform.anchorMin = new Vector2(0f, 0.3f);
            main.rectTransform.anchorMax = Vector2.one;
            main.rectTransform.offsetMin = main.rectTransform.offsetMax = Vector2.zero;

            string caption = !unlocked ? "Bloqueado" : (done ? "Completo" : "Pendiente");
            var cap = NewLabel("Caption", go.transform, caption, dense ? 22 : 28, FontStyles.Normal);
            cap.rectTransform.anchorMin = Vector2.zero;
            cap.rectTransform.anchorMax = new Vector2(1f, 0.3f);
            cap.rectTransform.offsetMin = cap.rectTransform.offsetMax = Vector2.zero;
            if (!unlocked) { main.color = new Color(1f, 1f, 1f, 0.55f); cap.color = new Color(1f, 1f, 1f, 0.7f); }
        }

        private static TextMeshProUGUI NewLabel(string name, Transform parent, string text, float size, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return tmp;
        }

        // ---------- trazo ----------

        private void StartItem(TraceItem item)
        {
            _item = item;
            int first = ItemGlyphs.FindIndex(g => !TraceProgress.IsGlyphDone(g));
            ShowGlyph(first < 0 ? 0 : first);
        }

        private void ShowGlyph(int index)
        {
            _glyphIndex = index;
            _completed = false;
            _rejecting = false;
            var glyph = CurrentGlyph;
            _templateSamples = TraceValidator.Sample(glyph.Strokes);

            menuRoot.SetActive(false);
            traceRoot.SetActive(true);
            if (NewUi)
            {
                if (hideWhileTracing != null) foreach (var go in hideWhileTracing) if (go != null) go.SetActive(false);
                SetMenuChromeVisible(false);
            }
            SetTitle(numbersMode
                ? "¡Aprende a escribir el <color=#F5A000>número</color>!"
                : "¡Aprende a escribir la <color=#F5A000>letra</color>!");

            if (glyphTitleText != null) glyphTitleText.text = glyph.Label;
            if (stepsText != null)
            {
                var sb = new System.Text.StringBuilder();
                var steps = glyph.Steps;
                for (int i = 0; i < steps.Length; i++) sb.AppendLine($"{i + 1}. {steps[i]}");
                stepsText.text = sb.ToString();
            }

            guideGraphic.SetStrokes(ToLocal(glyph.Strokes, drawArea));
            if (previewGraphic != null)
            {
                previewGraphic.SetStrokes(ToLocal(glyph.Strokes, (RectTransform)previewGraphic.transform));
            }

            BuildMarkers(glyph);
            ClearUserTrace();
            SetFeedback("Traza con el dedo", BannerInfo);
            if (nextButton != null) nextButton.gameObject.SetActive(false);

            if (NewUi) EnterWatchPhase(glyph);
        }

        // ---------- diseño nuevo: Mira > Traza > Practica ----------

        // Lo que comparten el menú y la pantalla de trazo (título, volver) se oculta mientras se traza: la barra nueva los reemplaza
        private void SetMenuChromeVisible(bool visible)
        {
            if (titleText != null) titleText.gameObject.SetActive(visible);
            if (backButton != null) backButton.gameObject.SetActive(visible);
        }

        private void EnterWatchPhase(TraceGlyph glyph)
        {
            _phase = Phase.Watch;
            string kind = numbersMode ? "el número" : "la letra";
            string shape = glyph.Id.Substring(glyph.Id.IndexOf('_') + 1);
            string shown = glyph.Id.StartsWith("up_") ? shape.ToUpperInvariant() : shape;
            if (headerTitle != null) headerTitle.text = $"Aprende {kind} <color=#FFC933>{shown}</color>";
            if (listenLabel != null) listenLabel.text = numbersMode ? $"Escuchar {shape}" : $"Escuchar /{shape.ToLowerInvariant()}/";

            BuildInstructions(glyph);
            if (startButton != null) startButton.gameObject.SetActive(true);
            if (clearButton != null) clearButton.gameObject.SetActive(false);
            if (checkButton != null) checkButton.gameObject.SetActive(false);
            SetChips(0);
            UpdateStepPill();
            SetFeedback("Mira cómo se traza y luego te toca a ti", BannerInfo);
            if (soundPanel != null) soundPanel.OnGlyphShown(glyph.Id);
            PlayDemo();
        }

        private void OnStartTracing()
        {
            StopDemo();
            _phase = Phase.Trace;
            if (startButton != null) startButton.gameObject.SetActive(false);
            if (clearButton != null) clearButton.gameObject.SetActive(true);
            if (checkButton != null) checkButton.gameObject.SetActive(true);
            SetChips(1);
            ClearUserTrace();
            UpdateStepPill();
        }

        private void SetChips(int active)
        {
            if (stepChips == null) return;
            for (int i = 0; i < stepChips.Length; i++)
            {
                if (stepChips[i] == null) continue;
                bool on = i == active;
                stepChips[i].color = on ? chipOnColor : chipOffColor;
                stepChips[i].transform.localScale = on ? Vector3.one * chipOnScale : Vector3.one;
            }
        }

        private void UpdateStepPill()
        {
            if (stepPillText == null || _item == null) return;
            int total = CurrentGlyph.Strokes.Count;
            int step = Mathf.Min(_userStrokes.Count + 1, total);
            stepPillText.text = _phase == Phase.Practice ? "¡Completo!" : $"Paso {step} de {total}";
        }

        private void OnListen()
        {
            if (_item == null) return;
            if (soundPanel == null || !soundPanel.PlayVoice(CurrentGlyph.Id))
                SetFeedback("La voz guía de esta forma llegará pronto", BannerInfo);
        }

        private void PlayDemo()
        {
            if (_item == null) return;
            StopDemo();
            _demo = StartCoroutine(DemoRoutine(CurrentGlyph));
        }

        private void StopDemo()
        {
            if (_demo != null) StopCoroutine(_demo);
            _demo = null;
            _demoRunning = false;
            if (_demoDot != null) _demoDot.gameObject.SetActive(false);
        }

        // Una bolita recorre cada trazo en orden, con una pausa en el número de cada uno: es el "modelo" que el niño imita
        private System.Collections.IEnumerator DemoRoutine(TraceGlyph glyph)
        {
            _demoRunning = true;
            if (markersRoot == null || circleSprite == null) { _demoRunning = false; yield break; }

            if (_demoDot == null)
            {
                var go = new GameObject("Demo_Dot", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(markersRoot, false);
                _demoDot = (RectTransform)go.transform;
                _demoDot.sizeDelta = new Vector2(46f, 46f);
                var img = go.GetComponent<Image>();
                img.sprite = circleSprite;
                img.raycastTarget = false;
            }
            _demoDot.SetAsLastSibling();
            _demoDot.gameObject.SetActive(true);
            var dotImg = _demoDot.GetComponent<Image>();

            float size = SquareSize(drawArea, glyphScale);
            const float speed = 520f;   // píxeles por segundo
            for (int s = 0; s < glyph.Strokes.Count; s++)
            {
                dotImg.color = StrokeColors[s % StrokeColors.Length];
                var pts = glyph.Strokes[s];
                _demoDot.anchoredPosition = new Vector2((pts[0].x - 0.5f) * size, (pts[0].y - 0.5f) * size);
                yield return new WaitForSecondsRealtime(0.35f);
                for (int i = 1; i < pts.Count; i++)
                {
                    Vector2 a = new Vector2((pts[i - 1].x - 0.5f) * size, (pts[i - 1].y - 0.5f) * size);
                    Vector2 b = new Vector2((pts[i].x - 0.5f) * size, (pts[i].y - 0.5f) * size);
                    float duration = Mathf.Max(0.01f, Vector2.Distance(a, b) / speed);
                    for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
                    {
                        _demoDot.anchoredPosition = Vector2.Lerp(a, b, t / duration);
                        yield return null;
                    }
                }
                yield return new WaitForSecondsRealtime(0.3f);
            }
            _demoDot.gameObject.SetActive(false);
            _demoRunning = false;
            _demo = null;
        }

        private void BuildInstructions(TraceGlyph glyph)
        {
            if (instructionsList == null) return;
            for (int i = instructionsList.childCount - 1; i >= 0; i--) Destroy(instructionsList.GetChild(i).gameObject);

            for (int i = 0; i < glyph.Strokes.Count; i++)
            {
                var row = new GameObject($"Step_{i + 1}", typeof(RectTransform), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
                row.transform.SetParent(instructionsList, false);
                row.GetComponent<LayoutElement>().minHeight = 96f;
                var hl = row.GetComponent<HorizontalLayoutGroup>();
                hl.spacing = 18f;
                hl.childAlignment = TextAnchor.MiddleLeft;
                hl.childControlWidth = hl.childControlHeight = true;
                hl.childForceExpandWidth = false;
                hl.childForceExpandHeight = false;

                var badge = new GameObject("Badge", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                badge.transform.SetParent(row.transform, false);
                var badgeImg = badge.GetComponent<Image>();
                badgeImg.sprite = circleSprite;
                badgeImg.color = StrokeColors[i % StrokeColors.Length];
                badgeImg.raycastTarget = false;
                var le = badge.GetComponent<LayoutElement>();
                le.minWidth = le.preferredWidth = 64f;
                le.minHeight = le.preferredHeight = 64f;
                var num = NewLabel("Number", badge.transform, (i + 1).ToString(), 40, FontStyles.Bold);
                num.rectTransform.anchorMin = Vector2.zero;
                num.rectTransform.anchorMax = Vector2.one;
                num.rectTransform.offsetMin = num.rectTransform.offsetMax = Vector2.zero;

                var text = NewLabel("Text", row.transform, StrokeHint(glyph.Strokes[i]), 36, FontStyles.Bold);
                text.color = new Color32(0x1E, 0x1B, 0x5C, 255);
                text.alignment = TextAlignmentOptions.MidlineLeft;
                text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            }
        }

        /// <summary>Texto corto de cómo se hace un trazo, a partir de su forma (círculo, baja, sube, derecha, izquierda, curva, esquinas).</summary>
        private static string StrokeHint(List<Vector2> stroke)
        {
            Vector2 a = stroke[0], b = stroke[stroke.Count - 1];
            float length = 0f;
            for (int i = 1; i < stroke.Count; i++) length += Vector2.Distance(stroke[i - 1], stroke[i]);
            float chord = Vector2.Distance(a, b);
            if (length > 0.5f && chord < length * 0.2f) return "Forma el círculo";

            // Una esquina marcada (como el 4 o la 'A' en un solo trazo) se cuenta en dos tiempos
            int corner = FindCorner(stroke);
            if (corner > 0) return Direction(a, stroke[corner]) + " y luego " + Direction(stroke[corner], b).ToLowerInvariant();

            bool curved = chord > 0.001f && length / chord > 1.25f;
            string dir = Direction(a, b);
            return curved ? dir + " con una curva" : dir;
        }

        private static string Direction(Vector2 from, Vector2 to)
        {
            Vector2 d = to - from;
            if (Mathf.Abs(d.y) > Mathf.Abs(d.x) * 1.6f) return d.y < 0f ? "Baja" : "Sube";
            if (Mathf.Abs(d.x) > Mathf.Abs(d.y) * 1.6f) return d.x > 0f ? "Ve a la derecha" : "Ve a la izquierda";
            return (d.y < 0f ? "Baja" : "Sube") + (d.x > 0f ? " a la derecha" : " a la izquierda");
        }

        // Índice del punto donde el trazo gira más de 60 grados entre segmentos largos; -1 si no hay esquina
        private static int FindCorner(List<Vector2> stroke)
        {
            int best = -1;
            float bestAngle = 60f;
            for (int i = 1; i < stroke.Count - 1; i++)
            {
                Vector2 in1 = stroke[i] - stroke[i - 1], out1 = stroke[i + 1] - stroke[i];
                if (in1.magnitude < 0.08f || out1.magnitude < 0.08f) continue;
                float angle = Vector2.Angle(in1, out1);
                if (angle > bestAngle) { bestAngle = angle; best = i; }
            }
            return best;
        }

        public void OnSurfaceDown(PointerEventData eventData)
        {
            _rejecting = false;
            if (_completed || _item == null || !InputAllowed) return;
            if (!TryGetLocal(eventData, out Vector2 local)) return;

            Vector2 n = LocalToGlyph(local);
            if (!TraceValidator.IsOnPath(n, _templateSamples))
            {
                RejectStroke(false);
                return;
            }

            _userStrokes.Add(new List<Vector2> { n });
            userGraphic.BeginStroke(local);
            UpdateStepPill();
        }

        public void OnSurfaceDrag(PointerEventData eventData)
        {
            if (_rejecting || _completed || _userStrokes.Count == 0 || !InputAllowed) return;
            if (!TryGetLocal(eventData, out Vector2 local)) return;

            var current = _userStrokes[_userStrokes.Count - 1];
            Vector2 n = LocalToGlyph(local);
            Vector2 last = current[current.Count - 1];
            if ((n - last).sqrMagnitude < 0.0001f) return;

            // No se puede salir de la forma: el trazo se descarta y hay que repetirlo.
            if (!TraceValidator.IsSegmentOnPath(last, n, _templateSamples))
            {
                RejectStroke(true);
                return;
            }

            current.Add(n);
            userGraphic.AddPoint(local);
        }

        private void RejectStroke(bool hadStroke)
        {
            _rejecting = true;
            if (hadStroke && _userStrokes.Count > 0)
            {
                _userStrokes.RemoveAt(_userStrokes.Count - 1);
                userGraphic.RemoveLastStroke();
            }
            SetFeedback("¡Te saliste de la línea! Intenta de nuevo.", BannerFail);
        }

        public void ClearUserTrace()
        {
            _userStrokes.Clear();
            if (userGraphic != null) userGraphic.Clear();
            UpdateStepPill();
            if (!_completed) SetFeedback("Traza con el dedo", BannerInfo);
        }

        public void CheckTrace()
        {
            if (_item == null) return;
            if (_completed || _userStrokes.Count == 0)
            {
                SetFeedback("Primero haz el trazo", BannerInfo);
                return;
            }

            var result = TraceValidator.Evaluate(CurrentGlyph.Strokes, _userStrokes);

            // Cada intento (aprobado o no) se envía al backend; él decide si aprobó y entrega el XP una sola vez.
            ProgressSyncService.ReportAttempt(CurrentGlyph.Id, result.Coverage, result.Accuracy);

            if (!result.Passed)
            {
                SetFeedback("¡Ups! Intenta de nuevo.", BannerFail);
                return;
            }

            _completed = true;
            _phase = Phase.Practice;
            SetChips(2);
            if (stepPillText != null) stepPillText.text = "¡Completo!";
            TraceProgress.MarkGlyphDone(CurrentGlyph);

            bool moreInItem = _glyphIndex < ItemGlyphs.Count - 1;
            if (moreInItem)
            {
                SetFeedback("¡Muy bien! Ahora la minúscula", BannerOk);
                if (nextLabel != null) nextLabel.text = "Minúscula";
            }
            else
            {
                var unlocked = FindUnlockedBy(_item);
                SetFeedback(unlocked != null ? $"¡Muy bien! Se desbloqueó el {unlocked.Id}" : "¡Muy bien!", BannerOk);
                if (nextLabel != null) nextLabel.text = "Continuar";
            }
            if (nextButton != null) nextButton.gameObject.SetActive(true);
        }

        private TraceItem FindUnlockedBy(TraceItem finished)
        {
            foreach (var group in new[] { TraceGroup.Vowels, TraceGroup.Consonants, TraceGroup.Digits })
            {
                foreach (var item in TraceCatalog.GetGroup(group))
                {
                    if (item.RequiresId == finished.Id && TraceProgress.IsUnlocked(item, _case)) return item;
                }
            }
            return null;
        }

        private void OnNext()
        {
            if (_item != null && _glyphIndex < ItemGlyphs.Count - 1)
            {
                ShowGlyph(_glyphIndex + 1);
            }
            else
            {
                ShowMenu(_item != null ? _item.Group : _group);
            }
        }

        private void OnBack()
        {
            if (traceRoot.activeSelf)
            {
                ShowMenu(_item != null ? _item.Group : _group);
            }
            else
            {
                // Al salir se guarda el progreso en el backend con la pantalla de carga.
                LoadingOverlay.LoadScene(returnSceneName, "Guardando tu progreso...", true, ProgressSyncService.Push());
            }
        }

        // ---------- utilidades ----------

        private void SetTitle(string text)
        {
            if (titleText != null) titleText.text = text;
        }

        private void SetFeedback(string message, Color color)
        {
            if (feedbackText != null) feedbackText.text = message;
            if (feedbackBanner != null) feedbackBanner.color = color;
        }

        private bool TryGetLocal(PointerEventData e, out Vector2 local)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                drawArea, e.position, e.pressEventCamera, out local);
        }

        private static float SquareSize(RectTransform area, float scale)
        {
            return Mathf.Min(area.rect.width, area.rect.height) * scale;
        }

        private Vector2 LocalToGlyph(Vector2 local)
        {
            float size = SquareSize(drawArea, glyphScale);
            return new Vector2(local.x / size + 0.5f, local.y / size + 0.5f);
        }

        private List<List<Vector2>> ToLocal(List<List<Vector2>> strokes, RectTransform area)
        {
            float size = SquareSize(area, glyphScale);
            var result = new List<List<Vector2>>();
            foreach (var stroke in strokes)
            {
                var pts = new List<Vector2>();
                foreach (var p in stroke)
                {
                    pts.Add(new Vector2((p.x - 0.5f) * size, (p.y - 0.5f) * size));
                }
                result.Add(pts);
            }
            return result;
        }

        private void BuildMarkers(TraceGlyph glyph)
        {
            if (markersRoot == null) return;
            // Al limpiar los marcadores también se va la bolita del modelo: se detiene la demostración y se vuelve a crear después
            StopDemo();
            _demoDot = null;
            for (int i = markersRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(markersRoot.GetChild(i).gameObject);
            }

            float size = SquareSize(drawArea, glyphScale);
            for (int i = 0; i < glyph.Strokes.Count; i++)
            {
                Vector2 start = glyph.Strokes[i][0];
                var go = new GameObject($"Marker_{i + 1}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(markersRoot, false);
                var rt = (RectTransform)go.transform;
                rt.sizeDelta = new Vector2(56f, 56f);
                rt.anchoredPosition = new Vector2((start.x - 0.5f) * size, (start.y - 0.5f) * size);

                var img = go.GetComponent<Image>();
                img.sprite = circleSprite;
                img.color = NewUi ? StrokeColors[i % StrokeColors.Length] : new Color(1f, 0.75f, 0.1f);
                img.raycastTarget = false;

                var number = NewLabel("Number", go.transform, (i + 1).ToString(), 34, FontStyles.Bold);
                number.rectTransform.anchorMin = Vector2.zero;
                number.rectTransform.anchorMax = Vector2.one;
                number.rectTransform.offsetMin = number.rectTransform.offsetMax = Vector2.zero;

                if (NewUi && arrowSprite != null) BuildArrows(glyph.Strokes[i], StrokeColors[i % StrokeColors.Length], size);
            }
        }

        // Flechitas sobre el camino que muestran hacia dónde se traza (a 30 %, 55 % y 80 % del recorrido)
        private void BuildArrows(List<Vector2> stroke, Color color, float size)
        {
            float total = 0f;
            for (int i = 1; i < stroke.Count; i++) total += Vector2.Distance(stroke[i - 1], stroke[i]);
            if (total < 0.15f) return;

            foreach (float fraction in new[] { 0.3f, 0.55f, 0.8f })
            {
                float target = total * fraction, walked = 0f;
                for (int i = 1; i < stroke.Count; i++)
                {
                    float seg = Vector2.Distance(stroke[i - 1], stroke[i]);
                    if (walked + seg < target) { walked += seg; continue; }
                    Vector2 dir = (stroke[i] - stroke[i - 1]).normalized;
                    Vector2 pos = Vector2.Lerp(stroke[i - 1], stroke[i], seg > 0f ? (target - walked) / seg : 0f);

                    var go = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(markersRoot, false);
                    var rt = (RectTransform)go.transform;
                    rt.sizeDelta = new Vector2(34f, 34f);
                    rt.anchoredPosition = new Vector2((pos.x - 0.5f) * size, (pos.y - 0.5f) * size);
                    rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                    var img = go.GetComponent<Image>();
                    img.sprite = arrowSprite;
                    img.color = color;
                    img.raycastTarget = false;
                    break;
                }
            }
        }
    }
}
