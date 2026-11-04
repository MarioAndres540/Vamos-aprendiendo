using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VamosAprendiendo.UI
{
    /// <summary>
    /// Pantalla de carga reutilizable con el mismo estilo que la del Login (fondo oscuro, tarjeta, aro que gira, barra
    /// de progreso y porcentaje). Se crea sola la primera vez que se usa y sobrevive a los cambios de escena, así
    /// que puede taparlos. Todo usa tiempo sin escala: funciona aunque el juego esté en pausa.
    /// </summary>
    public class LoadingOverlay : MonoBehaviour
    {
        private static LoadingOverlay _instance;

        private CanvasGroup _group;
        private GameObject _card;
        private RectTransform _spinner;
        private Image _barFill;
        private TMP_Text _title;
        private TMP_Text _tip;
        private TMP_Text _percent;
        private bool _spin;
        private Coroutine _running;

        public static LoadingOverlay Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("LoadingOverlay");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<LoadingOverlay>();
                    _instance.Build();
                }
                return _instance;
            }
        }

        /// <summary>True mientras hay una transición en curso.</summary>
        public static bool IsBusy => _instance != null && _instance._running != null;

        // ---------- API ----------

        /// <summary>
        /// Cambia de escena con transición. Con <paramref name="showCard"/> aparece la tarjeta con barra de progreso y mensajes
        /// (para cuando se guarda o sincroniza algo); sin ella es solo un fundido a oscuro. <paramref name="beforeLoad"/> es
        /// trabajo previo (por ejemplo, guardar en el backend) que debe terminar antes de salir de la escena.
        /// </summary>
        public static void LoadScene(string sceneName, string title = "Cargando...", bool showCard = true, IEnumerator beforeLoad = null, float minSeconds = 0.9f, string workTip = "Guardando tu progreso...")
        {
            if (IsBusy) return;
            var overlay = Instance;
            overlay._running = overlay.StartCoroutine(overlay.Run(sceneName, title, showCard, beforeLoad, minSeconds, workTip));
        }

        // ---------- secuencia ----------

        private IEnumerator Run(string sceneName, string title, bool showCard, IEnumerator beforeLoad, float minSeconds, string workTip)
        {
            Time.timeScale = 1f;
            _title.text = title;
            _tip.text = "";
            SetProgress(0f);
            _card.SetActive(showCard);
            _spin = showCard;
            _group.blocksRaycasts = true;

            yield return Fade(0f, 1f, 0.25f);

            float started = Time.unscaledTime;
            float shown = 0f;

            if (beforeLoad != null)
            {
                // El trabajo previo ocupa hasta el 60 % de la barra; avanza sola despacio mientras responde el servidor.
                bool workDone = false;
                StartCoroutine(RunWork(beforeLoad, () => workDone = true));
                _tip.text = workTip;
                while (!workDone)
                {
                    shown = Mathf.MoveTowards(shown, 0.55f, Time.unscaledDeltaTime * 0.35f);
                    SetProgress(shown);
                    yield return null;
                }
            }

            _tip.text = "Preparando la siguiente pantalla...";
            AsyncOperation op = null;
            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                op = SceneManager.LoadSceneAsync(sceneName);
                op.allowSceneActivation = false;
            }

            while (true)
            {
                float real = op != null ? Mathf.Clamp01(op.progress / 0.9f) : 1f;
                float target = Mathf.Lerp(0.6f, 1f, real);
                shown = Mathf.MoveTowards(shown, target, Time.unscaledDeltaTime * 0.9f);
                SetProgress(shown);
                bool minTime = Time.unscaledTime - started >= minSeconds;
                if (shown >= 0.999f && minTime && real >= 0.99f) break;
                yield return null;
            }

            _tip.text = "¡Listo!";
            yield return new WaitForSecondsRealtime(0.15f);
            if (op != null) op.allowSceneActivation = true;
            yield return null;
            yield return null;

            yield return Fade(1f, 0f, 0.3f);
            _group.blocksRaycasts = false;
            _spin = false;
            _running = null;
        }

        // Una corrutina anidada no avisa cuándo termina: se envuelve para marcarlo.
        private static IEnumerator RunWork(IEnumerator work, System.Action onDone)
        {
            yield return work;
            onDone();
        }

        private IEnumerator Fade(float from, float to, float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / seconds));
                yield return null;
            }
            _group.alpha = to;
        }

        private void SetProgress(float value)
        {
            _barFill.fillAmount = value;
            _percent.text = Mathf.RoundToInt(value * 100f) + "%";
        }

        private void Update()
        {
            if (_spin && _spinner != null) _spinner.Rotate(0f, 0f, -360f * Time.unscaledDeltaTime);
        }

        // ---------- interfaz construida por código ----------

        private void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            var round = Resources.Load<Sprite>("ui_round");
            var circle = Resources.Load<Sprite>("ui_circle");

            var back = NewImage("Backdrop", transform, new Color(0.04f, 0.07f, 0.16f, 0.94f), null);
            Stretch(back.rectTransform);

            _card = NewImage("Card", back.transform, new Color(0.10f, 0.14f, 0.28f, 0.98f), round).gameObject;
            var cardRt = (RectTransform)_card.transform;
            ((Image)_card.GetComponent<Image>()).type = Image.Type.Sliced;
            cardRt.sizeDelta = new Vector2(760f, 380f);

            var ring = NewImage("Spinner", _card.transform, new Color(0.45f, 0.55f, 1f, 1f), circle);
            _spinner = ring.rectTransform;
            _spinner.sizeDelta = new Vector2(84f, 84f);
            _spinner.anchoredPosition = new Vector2(0f, 105f);
            var hole = NewImage("Hole", ring.transform, new Color(0.10f, 0.14f, 0.28f, 1f), circle);
            hole.rectTransform.sizeDelta = new Vector2(60f, 60f);
            var notch = NewImage("Notch", ring.transform, Color.white, circle);
            notch.rectTransform.sizeDelta = new Vector2(20f, 20f);
            notch.rectTransform.anchoredPosition = new Vector2(0f, 34f);

            _title = NewText("Title", _card.transform, "Cargando...", 40f, Color.white, FontStyles.Bold);
            _title.rectTransform.anchoredPosition = new Vector2(0f, 20f);
            _tip = NewText("Tip", _card.transform, "", 26f, new Color(0.65f, 0.75f, 0.9f), FontStyles.Normal);
            _tip.rectTransform.anchoredPosition = new Vector2(0f, -30f);

            var track = NewImage("BarTrack", _card.transform, new Color(1f, 1f, 1f, 0.12f), round);
            track.type = Image.Type.Sliced;
            track.rectTransform.sizeDelta = new Vector2(600f, 18f);
            track.rectTransform.anchoredPosition = new Vector2(0f, -100f);
            track.pixelsPerUnitMultiplier = 3f;
            _barFill = NewImage("BarFill", track.transform, new Color(0.55f, 0.62f, 1f), round);
            _barFill.type = Image.Type.Filled;
            _barFill.fillMethod = Image.FillMethod.Horizontal;
            Stretch(_barFill.rectTransform);

            _percent = NewText("Percent", _card.transform, "0%", 24f, Color.white, FontStyles.Bold);
            _percent.rectTransform.anchoredPosition = new Vector2(0f, -140f);
        }

        private static Image NewImage(string name, Transform parent, Color color, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            img.raycastTarget = name == "Backdrop";
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            return img;
        }

        private static TMP_Text NewText(string name, Transform parent, string text, float size, Color color, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color; t.fontStyle = style;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(700f, 50f);
            return t;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
