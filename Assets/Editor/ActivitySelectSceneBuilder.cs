#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using VamosAprendiendo.Challenges.Tracing;
using VamosAprendiendo.UI;

namespace VamosAprendiendo.EditorTools
{
    /// <summary>
    /// Construye la escena ActivitySelect ("Tu ruta"): cuatro tarjetas (números, vocales minúsculas, vocales mayúsculas y
    /// consonantes próximamente) con porcentaje de progreso. Todo está anclado por proporciones y el área de tarjetas la
    /// reparte ResponsiveCardGrid, así se adapta a móviles y tabletas en horizontal y vertical.
    /// También prepara los sprites compartidos (Resources) que usa la pantalla de carga.
    /// </summary>
    public static class ActivitySelectSceneBuilder
    {
        private const string SCENE_PATH = "Assets/_Project/Scenes/ActivitySelect.unity";
        private const string SPRITES_DIR = "Assets/_Project/Art/images/ui_activity/";
        private const string RESOURCES_DIR = "Assets/Resources/";
        private const int SAMPLES = 3;

        private static readonly Color Navy = Hex("1B1F4B");
        private static readonly Color Muted = Hex("6B7099");
        private static readonly Color Violet = Hex("5B57D6");

        private enum Shape { Round, Circle, Star, Lock, Arrow }

        private struct CardSpec
        {
            public ActivityKind kind; public string glyph, title, comment; public Color color; public bool comingSoon;
        }

        [MenuItem("Tools/Scenes/Build Activity Select Scene (Tu ruta)", false, 22)]
        public static void Build()
        {
            var round = MakeSprite(Shape.Round, SPRITES_DIR + "ui_round.png", 64, 24);
            var circle = MakeSprite(Shape.Circle, SPRITES_DIR + "ui_circle.png", 128, 0);
            var star = MakeSprite(Shape.Star, SPRITES_DIR + "ui_star.png", 128, 0);
            var padlock = MakeSprite(Shape.Lock, SPRITES_DIR + "ui_lock.png", 128, 0);
            var arrow = MakeSprite(Shape.Arrow, SPRITES_DIR + "ui_arrow.png", 128, 0);
            PrepareSharedSprites();

            Directory.CreateDirectory(Path.GetDirectoryName(SCENE_PATH));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camObj = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Hex("F4F5FD");
            cam.orthographic = true;
            camObj.transform.position = new Vector3(0f, 0f, -10f);
            camObj.AddComponent<AudioListener>();

            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            var module = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            es.AddComponent(module ?? typeof(UnityEngine.EventSystems.StandaloneInputModule));

            var canvasObj = new GameObject("ActivitySelectCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f; // equilibra ancho y alto: sirve en móviles y tabletas
            canvasObj.AddComponent<GraphicRaycaster>();
            Transform root = canvasObj.transform;

            // ---- fondo (ocupa todo, incluso fuera del área segura) ----
            var bg = NewImage("Background", root, Hex("F4F5FD"), null);
            Stretch(bg.rectTransform);
            bg.raycastTarget = false;
            var hill2 = NewImage("Hill_Back", root, Hex("CDEAB5"), circle);
            Anchors(hill2.rectTransform, new Vector2(0.25f, -1.0f), new Vector2(1.35f, 0.12f));
            var hill1 = NewImage("Hill_Front", root, Hex("A9DB8F"), circle);
            Anchors(hill1.rectTransform, new Vector2(-0.35f, -1.05f), new Vector2(0.75f, 0.09f));
            hill1.raycastTarget = hill2.raycastTarget = false;

            // ---- contenido dentro del área segura ----
            var safe = NewRect("SafeArea", root);
            Stretch(safe);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            // Título y subtítulo
            var title = NewText("Title", safe, "Tu <color=#5B57D6>ruta</color>", 96f, Navy, TextAlignmentOptions.Center, true, 48f, 110f);
            Anchors(title.rectTransform, new Vector2(0.2f, 0.82f), new Vector2(0.8f, 0.97f));
            var subtitle = NewText("Subtitle", safe, "Sigue aprendiendo paso a paso", 40f, Muted, TextAlignmentOptions.Center, false, 22f, 44f);
            Anchors(subtitle.rectTransform, new Vector2(0.2f, 0.76f), new Vector2(0.8f, 0.83f));

            // Botón Volver (arriba a la izquierda, con tamaño fijo cómodo para el dedo)
            var back = NewImage("Btn_Volver", safe, Color.white, round);
            back.type = Image.Type.Sliced;
            back.pixelsPerUnitMultiplier = 1.4f;
            var backRt = back.rectTransform;
            backRt.anchorMin = backRt.anchorMax = backRt.pivot = new Vector2(0f, 1f);
            backRt.anchoredPosition = new Vector2(28f, -28f);
            backRt.sizeDelta = new Vector2(210f, 100f);
            var backBtn = back.gameObject.AddComponent<Button>();
            backBtn.targetGraphic = back;
            var arrowImg = NewImage("Arrow", back.transform, Violet, arrow);
            arrowImg.raycastTarget = false;
            Anchors(arrowImg.rectTransform, new Vector2(0.1f, 0.2f), new Vector2(0.38f, 0.8f));
            arrowImg.preserveAspect = true;
            var backLabel = NewText("Label", back.transform, "Volver", 38f, Navy, TextAlignmentOptions.Left, true, 20f, 40f);
            Anchors(backLabel.rectTransform, new Vector2(0.4f, 0.1f), new Vector2(0.95f, 0.9f));

            // Área de tarjetas
            var area = NewRect("Cards", safe);
            Anchors(area, new Vector2(0.04f, 0.2f), new Vector2(0.96f, 0.74f));
            var grid = area.gameObject.AddComponent<GridLayoutGroup>();
            grid.childAlignment = TextAnchor.MiddleCenter;
            area.gameObject.AddComponent<ResponsiveCardGrid>();

            var specs = new[]
            {
                new CardSpec { kind = ActivityKind.Numbers, glyph = "123", title = "Números", comment = "Aprende a escribir los números", color = Hex("3B82F6") },
                new CardSpec { kind = ActivityKind.LowercaseVowels, glyph = "a", title = "Letras minúsculas", comment = "Aprende a hacer las vocales", color = Violet },
                new CardSpec { kind = ActivityKind.UppercaseVowels, glyph = "A", title = "Letras mayúsculas", comment = "Ahora aprende las vocales en mayúsculas", color = Hex("22A84B") },
                new CardSpec { kind = ActivityKind.Consonants, glyph = "B", title = "Consonantes", comment = "Aprende las demás letras del abecedario", color = Hex("F0883E") }
            };

            var views = new ActivityCardView[specs.Length];
            for (int i = 0; i < specs.Length; i++) views[i] = BuildCard(area, specs[i], round, circle, star, padlock);

            // Mensaje de ánimo
            var pill = NewImage("Footer", safe, new Color(1f, 1f, 1f, 0.9f), round);
            pill.type = Image.Type.Sliced;
            pill.pixelsPerUnitMultiplier = 2f;
            pill.raycastTarget = false;
            Anchors(pill.rectTransform, new Vector2(0.22f, 0.03f), new Vector2(0.78f, 0.1f));
            var footer = NewText("Text", pill.transform, "¡Sigue así! Cada paso te acerca a tus metas", 32f, Muted, TextAlignmentOptions.Center, false, 16f, 34f);
            Stretch(footer.rectTransform, 8f);

            // Controlador
            var controller = new GameObject("ActivitySelect");
            var ui = controller.AddComponent<ActivitySelectUI>();
            var so = new SerializedObject(ui);
            var arr = so.FindProperty("cards");
            arr.arraySize = views.Length;
            for (int i = 0; i < views.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = views[i];
            so.FindProperty("backButton").objectReferenceValue = backBtn;
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            AddToBuildSettings(SCENE_PATH);
            Debug.Log("[ActivitySelect] Escena creada: " + SCENE_PATH);
        }

        // ---------- tarjeta ----------

        private static ActivityCardView BuildCard(Transform parent, CardSpec spec, Sprite round, Sprite circle, Sprite star, Sprite padlock)
        {
            var card = NewImage("Card_" + spec.kind, parent, Hex("E4E6F6"), round); // borde fino: el hijo blanco queda inset
            card.type = Image.Type.Sliced;
            card.pixelsPerUnitMultiplier = 1.2f;
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card;
            var colors = button.colors;
            colors.highlightedColor = new Color(0.93f, 0.94f, 1f);
            colors.pressedColor = new Color(0.82f, 0.84f, 0.98f);
            colors.disabledColor = Color.white;
            button.colors = colors;

            var face = NewImage("Face", card.transform, spec.comingSoon ? Hex("F7F8FC") : Color.white, round);
            face.type = Image.Type.Sliced;
            face.pixelsPerUnitMultiplier = 1.2f;
            face.raycastTarget = false;
            Stretch(face.rectTransform, 3f);

            // Estrella (completada) o candado (próximamente), arriba a la derecha
            var starImg = NewImage("Star", face.transform, Hex("D6D8EA"), star);
            starImg.raycastTarget = false;
            starImg.preserveAspect = true;
            Anchors(starImg.rectTransform, new Vector2(0.78f, 0.84f), new Vector2(0.94f, 0.96f));
            var lockObj = NewImage("Lock", face.transform, Hex("A5A8D6"), padlock);
            lockObj.raycastTarget = false;
            lockObj.preserveAspect = true;
            Anchors(lockObj.rectTransform, new Vector2(0.78f, 0.84f), new Vector2(0.94f, 0.96f));

            // Insignia: aro suave + círculo de color con el glifo
            var badge = NewRect("Badge", face.transform);
            badge.anchorMin = new Vector2(0.27f, 0.62f);
            badge.anchorMax = new Vector2(0.73f, 0.62f);
            badge.pivot = new Vector2(0.5f, 0.5f);
            badge.sizeDelta = Vector2.zero;
            var fit = badge.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            fit.aspectRatio = 1f;
            var ring = NewImage("Ring", badge, new Color(spec.color.r, spec.color.g, spec.color.b, 0.16f), circle);
            ring.raycastTarget = false;
            Stretch(ring.rectTransform);
            var disc = NewImage("Disc", badge, spec.color, circle);
            disc.raycastTarget = false;
            Anchors(disc.rectTransform, new Vector2(0.14f, 0.14f), new Vector2(0.86f, 0.86f));
            var glyph = NewText("Glyph", disc.transform, spec.glyph, 120f, Color.white, TextAlignmentOptions.Center, true, 30f, 130f);
            Stretch(glyph.rectTransform, 6f);

            var title = NewText("Title", face.transform, spec.title, 40f, spec.comingSoon ? Muted : Navy, TextAlignmentOptions.Center, true, 18f, 44f);
            Anchors(title.rectTransform, new Vector2(0.05f, 0.33f), new Vector2(0.95f, 0.44f));
            var comment = NewText("Comment", face.transform, spec.comment, 28f, Muted, TextAlignmentOptions.Top, false, 13f, 30f);
            comment.enableWordWrapping = true;
            Anchors(comment.rectTransform, new Vector2(0.07f, 0.17f), new Vector2(0.93f, 0.33f));

            // Barra y porcentaje
            var track = NewImage("BarTrack", face.transform, Hex("E3E5F1"), round);
            track.type = Image.Type.Sliced;
            track.pixelsPerUnitMultiplier = 3f;
            track.raycastTarget = false;
            Anchors(track.rectTransform, new Vector2(0.1f, 0.115f), new Vector2(0.9f, 0.15f));
            var fill = NewImage("BarFill", track.transform, spec.color, round);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 0f;
            fill.raycastTarget = false;
            Stretch(fill.rectTransform);
            var percent = NewText("Percent", face.transform, "0%", 36f, spec.comingSoon ? Muted : spec.color, TextAlignmentOptions.Center, true, 16f, 38f);
            Anchors(percent.rectTransform, new Vector2(0.1f, 0.02f), new Vector2(0.9f, 0.105f));

            var view = card.gameObject.AddComponent<ActivityCardView>();
            var so = new SerializedObject(view);
            so.FindProperty("kind").enumValueIndex = (int)spec.kind;
            so.FindProperty("button").objectReferenceValue = button;
            so.FindProperty("barFill").objectReferenceValue = fill;
            so.FindProperty("percentText").objectReferenceValue = percent;
            so.FindProperty("star").objectReferenceValue = starImg;
            so.FindProperty("lockIcon").objectReferenceValue = lockObj.gameObject;
            so.FindProperty("comingSoon").boolValue = spec.comingSoon;
            so.FindProperty("accent").colorValue = spec.color;
            so.ApplyModifiedProperties();
            view.Refresh(false);
            return view;
        }

        // ---------- sprites compartidos ----------

        // La pantalla de carga se crea por código y toma estos dos sprites de Resources.
        private static void PrepareSharedSprites()
        {
            Directory.CreateDirectory(RESOURCES_DIR);
            foreach (var (name, border) in new[] { ("ui_round", 24), ("ui_circle", 0) })
            {
                string dst = RESOURCES_DIR + name + ".png";
                File.Copy(SPRITES_DIR + name + ".png", dst, true);
                AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceUpdate);
                ConfigureSprite(dst, border);
            }
        }

        private static void ConfigureSprite(string path, int border)
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.spriteBorder = border > 0 ? new Vector4(border, border, border, border) : Vector4.zero;
            imp.SaveAndReimport();
        }

        private static Sprite MakeSprite(Shape shape, string path, int size, int border)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float a = 0f;
                    for (int sy = 0; sy < SAMPLES; sy++)
                        for (int sx = 0; sx < SAMPLES; sx++)
                            if (Inside(shape, (x + (sx + 0.5f) / SAMPLES) / size, (y + (sy + 0.5f) / SAMPLES) / size, size, border)) a += 1f;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a / (SAMPLES * SAMPLES));
                }
            }
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            ConfigureSprite(path, border);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static bool Inside(Shape shape, float u, float v, int size, int radius)
        {
            switch (shape)
            {
                case Shape.Round:
                {
                    float r = radius / (float)size;
                    float cx = Mathf.Clamp(u, r, 1f - r), cy = Mathf.Clamp(v, r, 1f - r);
                    return (u - cx) * (u - cx) + (v - cy) * (v - cy) <= r * r;
                }
                case Shape.Circle:
                    return (u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f) <= 0.25f;
                case Shape.Star:
                    return InStar(u, v);
                case Shape.Lock:
                {
                    bool body = u >= 0.2f && u <= 0.8f && v >= 0.08f && v <= 0.52f;
                    float dx = u - 0.5f, dy = v - 0.52f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    bool shackle = v >= 0.52f && d <= 0.27f && d >= 0.16f;
                    bool keyhole = (u - 0.5f) * (u - 0.5f) + (v - 0.34f) * (v - 0.34f) <= 0.07f * 0.07f
                                   || (u >= 0.47f && u <= 0.53f && v >= 0.18f && v <= 0.34f);
                    return (body && !keyhole) || shackle;
                }
                default: // Arrow: flecha hacia la izquierda
                    return Seg(u, v, 0.8f, 0.5f, 0.22f, 0.5f) <= 0.065f
                           || Seg(u, v, 0.22f, 0.5f, 0.5f, 0.78f) <= 0.065f
                           || Seg(u, v, 0.22f, 0.5f, 0.5f, 0.22f) <= 0.065f;
            }
        }

        // Estrella de cinco puntas (polígono de 10 vértices, regla par-impar).
        private static bool InStar(float u, float v)
        {
            const int n = 10;
            bool inside = false;
            Vector2 prev = StarPoint(n - 1);
            for (int i = 0; i < n; i++)
            {
                Vector2 cur = StarPoint(i);
                if ((cur.y > v) != (prev.y > v) && u < (prev.x - cur.x) * (v - cur.y) / (prev.y - cur.y) + cur.x) inside = !inside;
                prev = cur;
            }
            return inside;
        }

        private static Vector2 StarPoint(int i)
        {
            float r = i % 2 == 0 ? 0.48f : 0.2f;
            float a = Mathf.PI / 2f + i * Mathf.PI / 5f;
            return new Vector2(0.5f + Mathf.Cos(a) * r, 0.52f + Mathf.Sin(a) * r);
        }

        private static float Seg(float px, float py, float ax, float ay, float bx, float by)
        {
            float abx = bx - ax, aby = by - ay;
            float t = Mathf.Clamp01(((px - ax) * abx + (py - ay) * aby) / (abx * abx + aby * aby));
            float dx = px - (ax + abx * t), dy = py - (ay + aby * t);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        // ---------- utilidades de interfaz ----------

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

        private static Image NewImage(string name, Transform parent, Color color, Sprite sprite)
        {
            var rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            return img;
        }

        private static TextMeshProUGUI NewText(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align, bool bold, float minSize, float maxSize)
        {
            var rt = NewRect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.enableAutoSizing = true;
            t.fontSizeMin = minSize;
            t.fontSizeMax = maxSize;
            t.raycastTarget = false;
            return t;
        }

        private static void Stretch(RectTransform rt, float padding = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }

        // Rectángulo definido solo por anclas proporcionales (sin desplazamientos): escala con su contenedor.
        private static void Anchors(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color c);
            return c;
        }
    }
}
#endif
