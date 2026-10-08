using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
        private TraceItem _item;
        private int _glyphIndex;
        private bool _completed;
        private bool _rejecting;
        private List<Vector2> _templateSamples = new List<Vector2>();

        private TraceGlyph CurrentGlyph => _item.Glyphs[_glyphIndex];

        private void Awake()
        {
            if (clearButton != null) clearButton.onClick.AddListener(ClearUserTrace);
            if (checkButton != null) checkButton.onClick.AddListener(CheckTrace);
            if (nextButton != null) nextButton.onClick.AddListener(OnNext);
            if (backButton != null) backButton.onClick.AddListener(OnBack);
            if (tabVowelsButton != null) tabVowelsButton.onClick.AddListener(() => ShowMenu(TraceGroup.Vowels));
            if (tabConsonantsButton != null) tabConsonantsButton.onClick.AddListener(() => ShowMenu(TraceGroup.Consonants));
        }

        private void Start()
        {
            ShowMenu(numbersMode ? TraceGroup.Digits : TraceGroup.Vowels);
        }

        // ---------- menú ----------

        public void ShowMenu(TraceGroup group)
        {
            _group = group;
            _item = null;
            menuRoot.SetActive(true);
            traceRoot.SetActive(false);
            if (tabsRoot != null) tabsRoot.SetActive(!numbersMode);

            SetTitle(numbersMode ? "¡Elige un <color=#F5A000>número</color>!" : "¡Elige una <color=#F5A000>letra</color>!");
            SetTabColors();
            BuildGrid();
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
            bool unlocked = TraceProgress.IsUnlocked(item);
            bool done = TraceProgress.IsItemDone(item);

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
            int first = item.Glyphs.FindIndex(g => !TraceProgress.IsGlyphDone(g));
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
        }

        public void OnSurfaceDown(PointerEventData eventData)
        {
            _rejecting = false;
            if (_completed || _item == null) return;
            if (!TryGetLocal(eventData, out Vector2 local)) return;

            Vector2 n = LocalToGlyph(local);
            if (!TraceValidator.IsOnPath(n, _templateSamples))
            {
                RejectStroke(false);
                return;
            }

            _userStrokes.Add(new List<Vector2> { n });
            userGraphic.BeginStroke(local);
        }

        public void OnSurfaceDrag(PointerEventData eventData)
        {
            if (_rejecting || _completed || _userStrokes.Count == 0) return;
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
            if (!result.Passed)
            {
                SetFeedback("¡Ups! Intenta de nuevo.", BannerFail);
                return;
            }

            _completed = true;
            TraceProgress.MarkGlyphDone(CurrentGlyph);

            bool moreInItem = _glyphIndex < _item.Glyphs.Count - 1;
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

        private static TraceItem FindUnlockedBy(TraceItem finished)
        {
            foreach (var group in new[] { TraceGroup.Vowels, TraceGroup.Consonants, TraceGroup.Digits })
            {
                foreach (var item in TraceCatalog.GetGroup(group))
                {
                    if (item.RequiresId == finished.Id && TraceProgress.IsUnlocked(item)) return item;
                }
            }
            return null;
        }

        private void OnNext()
        {
            if (_item != null && _glyphIndex < _item.Glyphs.Count - 1)
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
                SceneManager.LoadScene(returnSceneName);
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
                img.color = new Color(1f, 0.75f, 0.1f);
                img.raycastTarget = false;

                var number = NewLabel("Number", go.transform, (i + 1).ToString(), 34, FontStyles.Bold);
                number.rectTransform.anchorMin = Vector2.zero;
                number.rectTransform.anchorMax = Vector2.one;
                number.rectTransform.offsetMin = number.rectTransform.offsetMax = Vector2.zero;
            }
        }
    }
}
