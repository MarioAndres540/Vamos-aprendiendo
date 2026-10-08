#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using VamosAprendiendo.Challenges.Tracing;

namespace VamosAprendiendo.EditorTools
{
    /// <summary>Construye las escenas 2D de trazado: LetterTrace_2D (vocales y consonantes) y NumberTrace_2D (0-9).</summary>
    public static class TraceSceneBuilder
    {
        private const string LETTER_SCENE = "Assets/_Project/Scenes/Challenges/LetterTrace_2D.unity";
        private const string NUMBER_SCENE = "Assets/_Project/Scenes/Challenges/NumberTrace_2D.unity";
        private const string IMAGES_DIR = "Assets/_Project/Art/images";

        private static Sprite _rounded;
        private static Sprite _circle;

        [MenuItem("Tools/Scenes/Build Trace Scenes (Letters + Numbers)", false, 21)]
        public static void BuildBoth()
        {
            Build(false);
            Build(true);
        }

        public static void Build(bool numbers)
        {
            string scenePath = numbers ? NUMBER_SCENE : LETTER_SCENE;
            _rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            _circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            var background = AssetDatabase.LoadAssetAtPath<Sprite>($"{IMAGES_DIR}/fondo.png");
            var mascot = AssetDatabase.LoadAssetAtPath<Sprite>($"{IMAGES_DIR}/avatar.png");

            Directory.CreateDirectory(Path.GetDirectoryName(scenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camObj = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.92f, 0.95f, 0.99f);
            cam.orthographic = true;
            camObj.transform.position = new Vector3(0, 0, -10);
            camObj.AddComponent<AudioListener>();

            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            var inputModule = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            es.AddComponent(inputModule ?? typeof(UnityEngine.EventSystems.StandaloneInputModule));

            var canvasObj = new GameObject(numbers ? "NumberTraceCanvas" : "LetterTraceCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 10f;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f; // ancho variable (16:9 a 20:9), alto fijo
            canvasObj.AddComponent<GraphicRaycaster>();
            Transform root = canvasObj.transform;

            // ---- elementos comunes ----
            var bg = NewRect("Background", root);
            Stretch(bg);
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.sprite = background;
            bgImg.color = background != null ? Color.white : new Color(0.55f, 0.8f, 1f);
            bgImg.raycastTarget = false;

            var mascotRt = NewRect("Mascot", root);
            Place(mascotRt, new Vector2(0, 1), new Vector2(50, -30), new Vector2(230, 230));
            var mascotImg = mascotRt.gameObject.AddComponent<Image>();
            mascotImg.sprite = mascot;
            mascotImg.preserveAspect = true;
            mascotImg.raycastTarget = false;

            var title = NewText("Title", root, "", 78, new Color(0.1f, 0.22f, 0.55f), TextAlignmentOptions.Center, true);
            Place(title.rectTransform, new Vector2(0.5f, 1), new Vector2(110, -40), new Vector2(1400, 120));

            var back = NewButton("Btn_Back", root, "Volver", new Color(0.2f, 0.4f, 0.85f), 36);
            Place((RectTransform)back.transform, new Vector2(1, 1), new Vector2(-40, -40), new Vector2(220, 80));

            // ---- menú de selección ----
            var menu = NewRect("Menu_Panel", root);
            Stretch(menu);

            var tabs = NewRect("Tabs", menu);
            Place(tabs, new Vector2(0.5f, 1), new Vector2(0, -210), new Vector2(700, 80));
            var tabVowels = NewButton("Tab_Vocales", tabs, "Vocales", new Color(0.25f, 0.45f, 0.9f), 38);
            Place((RectTransform)tabVowels.transform, new Vector2(0.5f, 0.5f), new Vector2(-175, 0), new Vector2(330, 80));
            var tabConsonants = NewButton("Tab_Consonantes", tabs, "Consonantes", new Color(0.55f, 0.58f, 0.66f), 38);
            Place((RectTransform)tabConsonants.transform, new Vector2(0.5f, 0.5f), new Vector2(175, 0), new Vector2(330, 80));

            var gridRt = NewRect("Grid", menu);
            Place(gridRt, new Vector2(0.5f, 0.5f), new Vector2(0, numbers ? -20 : -90), new Vector2(1700, 640));
            var grid = gridRt.gameObject.AddComponent<GridLayoutGroup>();
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.cellSize = new Vector2(200, 200);
            grid.spacing = new Vector2(34, 34);

            // ---- pantalla de trazo ----
            var trace = NewRect("Trace_Panel", root);
            Stretch(trace);

            var panel = NewRect("Panel_Instrucciones", trace);
            Place(panel, new Vector2(0, 1), new Vector2(70, -290), new Vector2(450, 700));
            var panelImg = panel.gameObject.AddComponent<Image>();
            panelImg.sprite = _rounded;
            panelImg.type = Image.Type.Sliced;
            panelImg.color = new Color(0.3f, 0.7f, 0.2f);

            var glyphTitle = NewText("Glyph_Title", panel, "", 38, Color.white, TextAlignmentOptions.Center, true);
            Place(glyphTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(420, 100));

            var steps = NewText("Steps", panel, "", 34, Color.white, TextAlignmentOptions.TopLeft, true);
            Place(steps.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -130), new Vector2(390, 210));

            var previewRt = NewRect("Preview", panel);
            Place(previewRt, new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(330, 330));
            var previewBg = previewRt.gameObject.AddComponent<Image>();
            previewBg.sprite = _rounded;
            previewBg.type = Image.Type.Sliced;
            previewBg.color = new Color(1f, 1f, 1f, 0.25f);
            previewBg.raycastTarget = false;
            var preview = NewStrokeGraphic("Preview_Strokes", previewRt, new Color(0.75f, 0.95f, 1f), 26f);

            var paper = NewRect("Paper", trace);
            Place(paper, new Vector2(0.5f, 0.5f), new Vector2(150, 0), new Vector2(1100, 640));
            var paperImg = paper.gameObject.AddComponent<Image>();
            paperImg.sprite = _rounded;
            paperImg.type = Image.Type.Sliced;
            paperImg.color = new Color(0.98f, 0.96f, 0.9f);
            paperImg.raycastTarget = false;
            var paperOutline = paper.gameObject.AddComponent<Outline>();
            paperOutline.effectColor = new Color(1f, 1f, 1f, 0.9f);
            paperOutline.effectDistance = new Vector2(6, -6);

            var area = NewRect("Trace_Area", paper);
            Stretch(area, 30);
            var areaImg = area.gameObject.AddComponent<Image>();
            areaImg.color = new Color(1f, 1f, 1f, 0f);

            var guide = NewStrokeGraphic("Guide_Strokes", area, new Color(0.6f, 0.62f, 0.68f, 0.6f), 64f);
            var user = NewStrokeGraphic("User_Strokes", area, new Color(0.9f, 0.18f, 0.18f, 1f), 26f);
            var markers = NewRect("Markers", area);
            Stretch(markers);

            var bannerRt = NewRect("Feedback_Banner", trace);
            Place(bannerRt, new Vector2(0.5f, 0), new Vector2(150, 125), new Vector2(760, 76));
            var bannerImg = bannerRt.gameObject.AddComponent<Image>();
            bannerImg.sprite = _rounded;
            bannerImg.type = Image.Type.Sliced;
            bannerImg.color = new Color(0.25f, 0.45f, 0.9f);
            bannerImg.raycastTarget = false;
            var feedback = NewText("Feedback_Text", bannerRt, "", 38, Color.white, TextAlignmentOptions.Center, true);
            Stretch(feedback.rectTransform);

            var clear = NewButton("Btn_Clear", trace, "Borrar", new Color(0.2f, 0.45f, 0.9f), 38);
            Place((RectTransform)clear.transform, new Vector2(0.5f, 0), new Vector2(-130, 30), new Vector2(260, 80));
            var check = NewButton("Btn_Check", trace, "Comprobar", new Color(0.3f, 0.7f, 0.25f), 38);
            Place((RectTransform)check.transform, new Vector2(0.5f, 0), new Vector2(150, 30), new Vector2(260, 80));
            var next = NewButton("Btn_Next", trace, "Siguiente", new Color(0.97f, 0.5f, 0.15f), 38);
            Place((RectTransform)next.transform, new Vector2(0.5f, 0), new Vector2(430, 30), new Vector2(260, 80));
            var nextLabel = next.transform.Find("Label").GetComponent<TMP_Text>();

            // ---- controlador ----
            var controllerObj = new GameObject("TraceChallenge");
            var challenge = controllerObj.AddComponent<TraceChallenge>();

            var surface = area.gameObject.AddComponent<TraceInputSurface>();
            var surfaceSo = new SerializedObject(surface);
            surfaceSo.FindProperty("challenge").objectReferenceValue = challenge;
            surfaceSo.ApplyModifiedProperties();

            var so = new SerializedObject(challenge);
            so.FindProperty("numbersMode").boolValue = numbers;
            so.FindProperty("menuRoot").objectReferenceValue = menu.gameObject;
            so.FindProperty("gridRoot").objectReferenceValue = gridRt;
            so.FindProperty("tabsRoot").objectReferenceValue = tabs.gameObject;
            so.FindProperty("tabVowelsButton").objectReferenceValue = tabVowels;
            so.FindProperty("tabConsonantsButton").objectReferenceValue = tabConsonants;
            so.FindProperty("roundedSprite").objectReferenceValue = _rounded;
            so.FindProperty("traceRoot").objectReferenceValue = trace.gameObject;
            so.FindProperty("drawArea").objectReferenceValue = area;
            so.FindProperty("guideGraphic").objectReferenceValue = guide;
            so.FindProperty("userGraphic").objectReferenceValue = user;
            so.FindProperty("previewGraphic").objectReferenceValue = preview;
            so.FindProperty("markersRoot").objectReferenceValue = markers;
            so.FindProperty("circleSprite").objectReferenceValue = _circle;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("glyphTitleText").objectReferenceValue = glyphTitle;
            so.FindProperty("stepsText").objectReferenceValue = steps;
            so.FindProperty("feedbackText").objectReferenceValue = feedback;
            so.FindProperty("feedbackBanner").objectReferenceValue = bannerImg;
            so.FindProperty("clearButton").objectReferenceValue = clear;
            so.FindProperty("checkButton").objectReferenceValue = check;
            so.FindProperty("nextButton").objectReferenceValue = next;
            so.FindProperty("nextLabel").objectReferenceValue = nextLabel;
            so.FindProperty("backButton").objectReferenceValue = back;
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, scenePath);
            AddToBuildSettings(scenePath);
            Debug.Log($"[TraceSceneBuilder] Escena creada: {scenePath}");
        }

        [MenuItem("Tools/Progress/Reset Trace Progress", false, 40)]
        public static void ResetProgress()
        {
            TraceProgress.ResetAll();
            Debug.Log("[TraceProgress] Progreso de trazado reiniciado.");
        }

        [MenuItem("Tools/Progress/Complete All Trace Items (test)", false, 41)]
        public static void CompleteAll()
        {
            TraceProgress.CompleteAll();
            Debug.Log("[TraceProgress] Todo el trazado marcado como completo.");
        }

        private static void AddToBuildSettings(string path)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == path))
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rt, float padding = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }

        // anchor = ancla y pivote a la vez.
        private static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static TextMeshProUGUI NewText(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align, bool bold)
        {
            var rt = NewRect(name, parent);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Button NewButton(string name, Transform parent, string label, Color color, float fontSize)
        {
            var rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = _rounded;
            img.type = Image.Type.Sliced;
            img.color = color;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;

            var text = NewText("Label", rt, label, fontSize, Color.white, TextAlignmentOptions.Center, true);
            Stretch(text.rectTransform);
            return btn;
        }

        private static TraceStrokeGraphic NewStrokeGraphic(string name, Transform parent, Color color, float thickness)
        {
            var rt = NewRect(name, parent);
            Stretch(rt);
            var g = rt.gameObject.AddComponent<TraceStrokeGraphic>();
            g.color = color;
            g.Thickness = thickness;
            g.raycastTarget = false;
            return g;
        }
    }
}
#endif
