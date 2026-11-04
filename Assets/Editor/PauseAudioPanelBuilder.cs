#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VamosAprendiendo.Gameplay;
using VamosAprendiendo.UI;

namespace VamosAprendiendo.EditorTools
{
    /// <summary>
    /// Añade al menú de pausa de BaseScene el panel de audio (casilla + volumen de música y de efectos) como un hijo
    /// de MenuPause_Dialog, entre el título "Pausa" y los botones, y alarga la tarjeta para que todo quede dentro de la imagen.
    /// Las formas (rectángulo redondeado, círculo, check, nota, altavoz) se generan como sprites blancos y se tiñen.
    /// </summary>
    public static class PauseAudioPanelBuilder
    {
        private const string BASE_SCENE_PATH = "Assets/_Project/Scenes/BaseScene.unity";
        private const string DIR = "Assets/_Project/Art/images/ui_audio/";
        private const int SAMPLES = 3;

        private static readonly Color Violet = Hex("5B57D6");
        private static readonly Color VioletDeep = Hex("3B36A8");
        private static readonly Color VioletSoft = Hex("7A77C9");
        private static readonly Color PanelBg = Hex("EEEDFD");
        private static readonly Color Track = Hex("CFCDF5");
        private static readonly Color Fill = Hex("7C6CF0");

        private enum Shape { Round, Circle, Check, Music, Speaker }

        [MenuItem("Tools/UI/Add Audio Panel to Pause Menu", false, 2)]
        public static void Build()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.path.Contains("BaseScene")) scene = EditorSceneManager.OpenScene(BASE_SCENE_PATH, OpenSceneMode.Single);

            Sprite round = MakeSprite(Shape.Round, "ui_round.png", 64, 24);
            Sprite circle = MakeSprite(Shape.Circle, "ui_circle.png", 128, 0);
            Sprite check = MakeSprite(Shape.Check, "ui_check.png", 256, 0);
            Sprite music = MakeSprite(Shape.Music, "ui_music.png", 256, 0);
            Sprite speaker = MakeSprite(Shape.Speaker, "ui_speaker.png", 256, 0);

            var pause = UnityEngine.Object.FindFirstObjectByType<PauseMenuUI>(FindObjectsInactive.Include);
            if (pause == null) { Debug.LogError("[PauseAudio] No se encontró PauseMenuUI en la escena."); return; }
            var dialog = (RectTransform)new SerializedObject(pause).FindProperty("menuPanel").objectReferenceValue;
            if (dialog == null) { Debug.LogError("[PauseAudio] PauseMenuUI no tiene menuPanel asignado."); return; }

            var old = dialog.Find("Audio_Panel");
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);

            // La tarjeta se alarga (ver MakeTallCard) para que el panel quede entre el título y los botones, todo dentro de la imagen.
            Sprite tall = MakeTallCard();
            var dialogImg = dialog.GetComponent<Image>();
            if (tall == null || dialogImg == null) { Debug.LogError("[PauseAudio] No se pudo crear la imagen alargada del menú."); return; }
            dialogImg.sprite = tall;
            dialogImg.preserveAspect = true;

            // Las medidas están en píxeles de la imagen original (1536x1024); un píxel equivale a 720/1536 unidades del diálogo.
            const float U = 720f / 1536f;
            float centerFromTop = (OrigH + Extra) * 0.5f;
            PlaceHitbox(dialog, "Button_Reanudar", (centerFromTop - (665f + Extra)) * U);
            PlaceHitbox(dialog, "Button_Salir", (centerFromTop - (833f + Extra)) * U);

            const float W = 380f, H = 240f, PanelScale = 0.84f; // diseño de 380x240, reducido para caber en el ancho interior de la tarjeta
            float panelTop = 548f + 30f;                           // 30 px bajo "Pausa"
            var panel = NewRect("Audio_Panel", dialog, new Vector2(0f, (centerFromTop - (panelTop + H * PanelScale / U * 0.5f)) * U), new Vector2(W, H));
            panel.localScale = new Vector3(PanelScale, PanelScale, 1f);
            Sliced(panel.gameObject.AddComponent<Image>(), round, PanelBg, 0.9f);

            // Casilla de audio
            var box = NewRect("Audio_Check", panel, At(18f + 17f, 18f + 17f, W, H), new Vector2(34f, 34f));
            var boxImg = Sliced(box.gameObject.AddComponent<Image>(), round, Violet, 2.4f);
            var boxBtn = box.gameObject.AddComponent<Button>();
            boxBtn.targetGraphic = boxImg;
            boxBtn.transition = Selectable.Transition.None;
            var boxInner = NewRect("Inner", box, Vector2.zero, new Vector2(26f, 26f));
            var innerImg = Sliced(boxInner.gameObject.AddComponent<Image>(), round, Color.white, 3.2f);
            innerImg.raycastTarget = false;
            var tick = NewRect("Check", box, Vector2.zero, new Vector2(24f, 24f));
            Icon(tick, check, Color.white);
            var checkMarkHolder = tick.gameObject;
            boxInner.gameObject.SetActive(false); // estado inicial: audio activado (casilla marcada)

            var title = Text("Audio_Title", panel, "Audio activado", 24f, VioletDeep, TextAlignmentOptions.Left,
                At(66f + 130f, 18f + 11f, W, H), new Vector2(260f, 30f));
            var subtitle = Text("Audio_Subtitle", panel, "Música y efectos suenan", 15f, VioletSoft, TextAlignmentOptions.Left,
                At(66f + 130f, 18f + 31f, W, H), new Vector2(260f, 22f));

            // Filas de volumen (dentro de un grupo para atenuarlas juntas)
            var group = NewRect("Sliders", panel, Vector2.zero, new Vector2(W, H));
            var cg = group.gameObject.AddComponent<CanvasGroup>();

            var musicRow = Row(group, "Musica", "Música", music, 94f, W, H, round, circle);
            var sfxRow = Row(group, "Efectos", "Efectos", speaker, 164f, W, H, round, circle);

            var ui = panel.gameObject.AddComponent<PauseAudioPanel>();
            var so = new SerializedObject(ui);
            so.FindProperty("checkButton").objectReferenceValue = boxBtn;
            so.FindProperty("emptyBox").objectReferenceValue = boxInner.gameObject;
            so.FindProperty("checkMark").objectReferenceValue = checkMarkHolder;
            so.FindProperty("title").objectReferenceValue = title;
            so.FindProperty("subtitle").objectReferenceValue = subtitle;
            so.FindProperty("slidersGroup").objectReferenceValue = cg;
            so.FindProperty("musicSlider").objectReferenceValue = musicRow.slider;
            so.FindProperty("musicPercent").objectReferenceValue = musicRow.percent;
            so.FindProperty("sfxSlider").objectReferenceValue = sfxRow.slider;
            so.FindProperty("sfxPercent").objectReferenceValue = sfxRow.percent;
            so.ApplyModifiedProperties();

            // Con la música al 70 % suena como antes (0,6 de volumen).
            var player = UnityEngine.Object.FindFirstObjectByType<AmbientMusicPlayer>();
            if (player != null)
            {
                var pso = new SerializedObject(player);
                pso.FindProperty("volume").floatValue = 0.85f;
                pso.ApplyModifiedProperties();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[PauseAudio] Panel de audio añadido bajo MenuPause_Dialog.");
        }


        private const int OrigH = 1024;
        private const int Extra = 445;    // alto que se añade a la tarjeta (px de la imagen original)
        private const int CutRow = 581;   // fila (desde arriba) donde se abre la tarjeta
        private const int BandTop = 579;  // fila lisa que se repite para alargarla (una sola fila: los bordes quedan rectos, sin patrón)

        private static void PlaceHitbox(Transform dialog, string name, float y)
        {
            var t = dialog.Find(name) as RectTransform;
            if (t == null) { Debug.LogWarning("[PauseAudio] No se encontró " + name); return; }
            t.anchoredPosition = new Vector2(0f, y);
        }

        // Parte menu_pause.png por debajo del título, repite una franja lisa del fondo de la tarjeta y vuelve a pegar la
        // parte de los botones: la tarjeta queda más alta con el pulpo, el título y los botones exactamente como estaban.
        private static Sprite MakeTallCard()
        {
            string src = "Assets/_Project/Art/images/menu_pause.png";
            string dst = "Assets/_Project/Art/images/menu_pause_audio.png";
            var orig = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            orig.LoadImage(File.ReadAllBytes(src));
            if (orig.height != OrigH) { Debug.LogError("[PauseAudio] menu_pause.png ya no mide " + OrigH + " px de alto."); return null; }
            int w = orig.width, h = OrigH + Extra;
            var srcPx = orig.GetPixels();
            var dstPx = new Color[w * h];
            int band = CutRow - BandTop;
            for (int r = 0; r < h; r++)
            {
                int srcRow = r < CutRow ? r : r < CutRow + Extra ? BandTop + (r - CutRow) % band : r - Extra;
                int srcY = OrigH - 1 - srcRow, dstY = h - 1 - r;
                Array.Copy(srcPx, srcY * w, dstPx, dstY * w, w);
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels(dstPx);
            tex.Apply();
            File.WriteAllBytes(dst, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(orig);
            AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceUpdate);

            var imp = (TextureImporter)AssetImporter.GetAtPath(dst);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.maxTextureSize = 2048;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(dst);
        }

        private struct RowRefs { public Slider slider; public TMP_Text percent; }

        private static RowRefs Row(Transform parent, string id, string label, Sprite icon, float topY, float W, float H, Sprite round, Sprite circle)
        {
            var ic = NewRect("Icon_" + id, parent, At(22f + 12f, topY, W, H), new Vector2(26f, 26f));
            Icon(ic, icon, Violet);
            Text("Label_" + id, parent, label, 20f, VioletDeep, TextAlignmentOptions.Left, At(22f + 30f + 80f, topY, W, H), new Vector2(160f, 28f));

            var pill = NewRect("Pill_" + id, parent, At(W - 22f - 31f, topY, W, H), new Vector2(62f, 26f));
            Sliced(pill.gameObject.AddComponent<Image>(), round, Violet, 1.9f).raycastTarget = false;
            var pct = Text("Percent_" + id, pill, "70%", 16f, Color.white, TextAlignmentOptions.Center, Vector2.zero, new Vector2(62f, 26f));

            // Barra: fondo, relleno y perilla (círculo violeta con centro blanco).
            var root = NewRect("Slider_" + id, parent, At(W * 0.5f, topY + 32f, W, H), new Vector2(W - 44f, 34f));
            var bg = NewRect("Background", root, Vector2.zero, new Vector2(0f, 16f));
            bg.anchorMin = new Vector2(0f, 0.5f); bg.anchorMax = new Vector2(1f, 0.5f); bg.sizeDelta = new Vector2(0f, 16f);
            Sliced(bg.gameObject.AddComponent<Image>(), round, Track, 3f);

            var fillArea = NewRect("Fill Area", root, Vector2.zero, Vector2.zero);
            fillArea.anchorMin = new Vector2(0f, 0.5f); fillArea.anchorMax = new Vector2(1f, 0.5f);
            fillArea.offsetMin = new Vector2(15f, -8f); fillArea.offsetMax = new Vector2(-15f, 8f);
            var fill = NewRect("Fill", fillArea, Vector2.zero, Vector2.zero);
            fill.anchorMin = new Vector2(0f, 0f); fill.anchorMax = new Vector2(0f, 1f);
            fill.offsetMin = new Vector2(-15f, 0f); fill.offsetMax = new Vector2(0f, 0f);
            Sliced(fill.gameObject.AddComponent<Image>(), round, Fill, 3f);

            var slideArea = NewRect("Handle Slide Area", root, Vector2.zero, Vector2.zero);
            // Alto 0: el Slider estira la perilla en vertical a lo alto del área, así queda del tamaño de la perilla (30).
            slideArea.anchorMin = new Vector2(0f, 0.5f); slideArea.anchorMax = new Vector2(1f, 0.5f);
            slideArea.offsetMin = new Vector2(15f, 0f); slideArea.offsetMax = new Vector2(-15f, 0f);
            var handle = NewRect("Handle", slideArea, Vector2.zero, new Vector2(30f, 30f));
            var handleImg = handle.gameObject.AddComponent<Image>();
            handleImg.sprite = circle; handleImg.color = Violet;
            var dot = NewRect("Dot", handle, Vector2.zero, new Vector2(18f, 18f));
            var dotImg = dot.gameObject.AddComponent<Image>();
            dotImg.sprite = circle; dotImg.color = Color.white; dotImg.raycastTarget = false;

            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImg;
            slider.transition = Selectable.Transition.None;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f; slider.maxValue = 100f; slider.wholeNumbers = true;
            slider.SetValueWithoutNotify(70f);

            return new RowRefs { slider = slider, percent = pct };
        }

        // Posición dentro del panel a partir de la distancia al borde superior izquierdo.
        private static Vector2 At(float fromLeft, float fromTop, float w, float h) => new Vector2(fromLeft - w * 0.5f, h * 0.5f - fromTop);

        private static RectTransform NewRect(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        private static Image Sliced(Image img, Sprite sprite, Color color, float pixelsMultiplier)
        {
            img.sprite = sprite;
            img.color = color;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = pixelsMultiplier;
            return img;
        }

        private static void Icon(RectTransform rt, Sprite sprite, Color color)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }

        private static TMP_Text Text(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align, Vector2 pos, Vector2 rect)
        {
            var rt = NewRect(name, parent, pos, rect);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.fontStyle = FontStyles.Bold;
            t.color = color;
            t.alignment = align;
            t.enableWordWrapping = false;
            t.raycastTarget = false;
            return t;
        }

        // ---------- Sprites ----------

        private static Sprite MakeSprite(Shape shape, string file, int size, int border)
        {
            string path = DIR + file;
            Directory.CreateDirectory(DIR);
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

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.spriteBorder = border > 0 ? new Vector4(border, border, border, border) : Vector4.zero;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // u, v en 0..1 (v hacia arriba).
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
                case Shape.Check:
                    return Seg(u, v, 0.23f, 0.49f, 0.42f, 0.30f) <= 0.085f || Seg(u, v, 0.42f, 0.30f, 0.78f, 0.72f) <= 0.085f;
                case Shape.Music:
                {
                    bool head1 = Ellipse(u, v, 0.33f, 0.27f, 0.15f, 0.115f);
                    bool head2 = Ellipse(u, v, 0.74f, 0.20f, 0.15f, 0.115f);
                    bool stem1 = u >= 0.40f && u <= 0.47f && v >= 0.27f && v <= 0.80f;
                    bool stem2 = u >= 0.81f && u <= 0.88f && v >= 0.20f && v <= 0.72f;
                    bool beam = u >= 0.40f && u <= 0.88f && v <= 0.80f - (u - 0.40f) * 0.17f && v >= 0.80f - (u - 0.40f) * 0.17f - 0.13f;
                    return head1 || head2 || stem1 || stem2 || beam;
                }
                case Shape.Speaker:
                {
                    bool body = u >= 0.14f && u <= 0.35f && v >= 0.38f && v <= 0.62f;
                    bool cone = u >= 0.35f && u <= 0.58f && Mathf.Abs(v - 0.5f) <= 0.12f + (u - 0.35f) * 0.95f * 0.5f;
                    float dx = u - 0.55f, dy = v - 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    bool inWave = dx > 0.02f && Mathf.Abs(Mathf.Atan2(dy, dx)) < 0.75f;
                    bool wave1 = inWave && Mathf.Abs(dist - 0.22f) <= 0.04f;
                    bool wave2 = inWave && Mathf.Abs(dist - 0.38f) <= 0.04f;
                    return body || cone || wave1 || wave2;
                }
            }
            return false;
        }

        private static bool Ellipse(float u, float v, float cx, float cy, float rx, float ry)
        {
            float a = (u - cx) / rx, b = (v - cy) / ry;
            return a * a + b * b <= 1f;
        }

        private static float Seg(float px, float py, float ax, float ay, float bx, float by)
        {
            float abx = bx - ax, aby = by - ay;
            float t = Mathf.Clamp01(((px - ax) * abx + (py - ay) * aby) / (abx * abx + aby * aby));
            float dx = px - (ax + abx * t), dy = py - (ay + aby * t);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color c);
            return c;
        }
    }
}
#endif
