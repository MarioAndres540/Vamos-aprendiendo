using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VamosAprendiendo.UI
{
    /// <summary>
    /// Mensaje breve en pantalla (tarjeta con título y texto) para avisos del mundo: «portal cerrado», «Nivel 2», «en construcción».
    /// Se crea sola la primera vez que se usa. No bloquea los toques ni detiene el juego.
    /// </summary>
    public class LevelToast : MonoBehaviour
    {
        private static LevelToast _instance;

        private CanvasGroup _group;
        private TMP_Text _title, _body;
        private Coroutine _running;

        public static void Show(string title, string body, float seconds = 3f)
        {
            if (_instance == null)
            {
                var go = new GameObject("LevelToast");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<LevelToast>();
                _instance.Build();
            }
            _instance.Display(title, body, seconds);
        }

        private void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 260;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(transform, false);
            var rt = (RectTransform)card.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 110f);
            rt.sizeDelta = new Vector2(1100f, 330f);
            var img = card.GetComponent<Image>();
            img.sprite = Resources.Load<Sprite>("ui_round");
            img.type = Image.Type.Sliced;
            img.color = new Color32(0xFF, 0xFF, 0xFF, 245);
            img.raycastTarget = false;

            var header = new GameObject("Header", typeof(RectTransform), typeof(Image));
            header.transform.SetParent(card.transform, false);
            var hr = (RectTransform)header.transform;
            hr.anchorMin = new Vector2(0f, 1f); hr.anchorMax = Vector2.one;
            hr.offsetMin = new Vector2(0f, -110f); hr.offsetMax = Vector2.zero;
            var himg = header.GetComponent<Image>();
            himg.sprite = img.sprite; himg.type = Image.Type.Sliced; himg.color = new Color32(0x6B, 0x57, 0xDD, 255); himg.raycastTarget = false;
            _title = NewText("Title", header.transform, 60, Color.white);
            Stretch(_title.rectTransform);

            _body = NewText("Body", card.transform, 42, new Color32(0x1E, 0x1B, 0x5C, 255));
            _body.fontStyle = FontStyles.Normal;
            var br = _body.rectTransform;
            br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one;
            br.offsetMin = new Vector2(50f, 20f); br.offsetMax = new Vector2(-50f, -125f);
        }

        private static TMP_Text NewText(string name, Transform parent, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.fontSize = size; t.color = color; t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            return t;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private void Display(string title, string body, float seconds)
        {
            _title.text = title;
            _body.text = body;
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(Run(seconds));
        }

        private IEnumerator Run(float seconds)
        {
            for (float t = 0f; t < 0.25f; t += Time.unscaledDeltaTime) { _group.alpha = t / 0.25f; yield return null; }
            _group.alpha = 1f;
            yield return new WaitForSecondsRealtime(seconds);
            for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime) { _group.alpha = 1f - t / 0.4f; yield return null; }
            _group.alpha = 0f;
            _running = null;
        }
    }
}
