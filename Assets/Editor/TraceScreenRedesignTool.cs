using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using VamosAprendiendo.Challenges.Tracing;

namespace VamosAprendiendo.EditorTools
{
    /// <summary>
    /// Rehace la pantalla de trazo (letras y números) con el diseño nuevo: barra superior con MIRA &gt; TRAZA &gt; PRACTICA, panel de
    /// instrucciones por trazo, zona de trazo con números y flechas, "Comenzar a trazar" / "Repetir modelo", mascota y panel de
    /// sonido de apoyo. Reutiliza los objetos que ya tiene el reto (zona de trazo, botones, aviso) y conecta los campos nuevos.
    /// </summary>
    public static class TraceScreenRedesignTool
    {
        private static readonly Color Purple = new Color32(0x6B, 0x57, 0xDD, 255);
        private static readonly Color PurpleLight = new Color32(0x84, 0x73, 0xEC, 255);
        private static readonly Color Navy = new Color32(0x1E, 0x1B, 0x5C, 255);
        private static readonly Color Green = new Color32(0x22, 0xC5, 0x5E, 255);
        private static readonly Color Pale = new Color32(0xF1, 0xEF, 0xFF, 255);

        private static Sprite _round, _circle, _arrow;

        [MenuItem("Tools/UI/Redesign Trace Screens (letters + numbers)")]
        public static void ApplyBoth()
        {
            Apply("Assets/_Project/Scenes/Challenges/LetterTrace_2D.unity");
            Apply("Assets/_Project/Scenes/Challenges/NumberTrace_2D.unity");
        }

        private static void Apply(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var tc = Object.FindFirstObjectByType<TraceChallenge>();
            if (tc == null) { Debug.LogError($"[TraceRedesign] {scenePath} no tiene TraceChallenge."); return; }

            _round = Resources.Load<Sprite>("ui_round");
            _circle = Resources.Load<Sprite>("ui_circle");
            _arrow = EnsureArrowSprite();

            var so = new SerializedObject(tc);
            var traceRoot = (RectTransform)((GameObject)so.FindProperty("traceRoot").objectReferenceValue).transform;
            var paper = ((RectTransform)so.FindProperty("drawArea").objectReferenceValue).parent as RectTransform;
            var banner = ((Image)so.FindProperty("feedbackBanner").objectReferenceValue).rectTransform;
            var clear = ((Button)so.FindProperty("clearButton").objectReferenceValue);
            var check = ((Button)so.FindProperty("checkButton").objectReferenceValue);
            var next = ((Button)so.FindProperty("nextButton").objectReferenceValue);
            var oldInstructions = traceRoot.Find("Panel_Instrucciones");
            var content = traceRoot.parent;

            // Lo nuevo se reconstruye cada vez: se borra lo que creó una pasada anterior
            var oldBorder = paper.Find("NT_Border");   // de la primera versión del diseño, cuando el borde era hijo del papel
            if (oldBorder != null) Object.DestroyImmediate(oldBorder.gameObject);
            foreach (var name in new[] { "NT_Border", "NT_Header", "NT_Instructions", "NT_Sound", "NT_Mascot", "NT_Start", "NT_Repeat", "NT_StepPill" })
            {
                var old = traceRoot.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }
            if (oldInstructions != null) oldInstructions.gameObject.SetActive(false);   // la referencia serializada se mantiene

            // --- zona de trazo ---
            Place(paper, new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(900f, 560f));
            var paperImg = paper.GetComponent<Image>();
            paperImg.sprite = _round; paperImg.type = Image.Type.Sliced; paperImg.color = Color.white;
            // El borde va detrás del papel (un hermano anterior): si fuera hijo taparía el papel
            var border = NewRect("NT_Border", traceRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(924f, 584f));
            border.SetSiblingIndex(paper.GetSiblingIndex());
            Img(border.gameObject, _round, new Color32(0xC9, 0xC2, 0xF5, 255), true);
            so.FindProperty("glyphScale").floatValue = 0.88f;

            // --- barra superior ---
            var header = NewRect("NT_Header", traceRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(1840f, 112f));
            header.pivot = new Vector2(0.5f, 1f);
            Img(header.gameObject, _round, Purple, true);

            var backBtn = NewRect("Back", header, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(66f, 0f), new Vector2(76f, 76f));
            var backArt = Art("btn_atras.png");
            if (backArt != null)
            {
                Img(backBtn.gameObject, backArt, Color.white, false).preserveAspect = true;
            }
            else
            {
                Img(backBtn.gameObject, _circle, new Color(1f, 1f, 1f, 0.25f), false);
                var backArrow = NewRect("Arrow", backBtn, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(40f, 40f));
                backArrow.localRotation = Quaternion.Euler(0f, 0f, 180f);
                Img(backArrow.gameObject, _arrow, Color.white, false);
            }
            var backButton = backBtn.gameObject.AddComponent<Button>();
            backButton.targetGraphic = backBtn.GetComponent<Image>();

            var title = Text("Title", header, "Aprende la letra a", 56, Color.white, TextAlignmentOptions.MidlineLeft);
            Place(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(128f, 0f), new Vector2(590f, 100f));
            title.rectTransform.pivot = new Vector2(0f, 0.5f);
            title.enableWordWrapping = false;
            title.enableAutoSizing = true;
            title.fontSizeMin = 30f;
            title.fontSizeMax = 56f;

            var chips = new Image[3];
            string[] chipNames = { "MIRA", "TRAZA", "PRACTICA" };
            var stepsArt = Art("info.png");
            bool artSteps = stepsArt != null;
            if (artSteps)
            {
                // La imagen trae MIRA > TRAZA > PRACTICA completo; encima van tres resplandores y solo el del paso actual se enciende
                const float sx = 770f, sw = 686f, sh = 73f;
                var steps = NewRect("Steps", header, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(sx, 0f), new Vector2(sw, sh));
                steps.pivot = new Vector2(0f, 0.5f);
                Img(steps.gameObject, stepsArt, Color.white, false).preserveAspect = true;
                float[] from = { 0f, 0.31f, 0.66f }, to = { 0.30f, 0.65f, 1f };
                for (int i = 0; i < 3; i++)
                {
                    var glow = NewRect("Glow_" + chipNames[i], steps, new Vector2(from[i], 0f), new Vector2(to[i], 1f), Vector2.zero, Vector2.zero);
                    glow.offsetMin = new Vector2(-4f, -2f); glow.offsetMax = new Vector2(4f, 2f);
                    chips[i] = Img(glow.gameObject, _round, new Color(1f, 1f, 1f, 0f), true);
                }
            }
            else
            for (int i = 0; i < 3; i++)
            {
                float x = 770f + i * 225f;
                var chip = NewRect("Chip_" + chipNames[i], header, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(x, 0f), new Vector2(62f, 62f));
                chips[i] = Img(chip.gameObject, _circle, Color.white, false);
                var n = Text("N", chip, (i + 1).ToString(), 38, Purple, TextAlignmentOptions.Center);
                Stretch(n.rectTransform);
                var l = Text("L", header, chipNames[i], 32, Color.white, TextAlignmentOptions.MidlineLeft);
                Place(l.rectTransform, new Vector2(0f, 0.5f), new Vector2(x + 150f, 0f), new Vector2(170f, 60f));
                l.fontSize = 28;
                l.enableWordWrapping = false;
                if (i < 2)
                {
                    var sep = NewRect("Sep", header, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(x + 188f, 0f), new Vector2(20f, 20f));
                    Img(sep.gameObject, _arrow, new Color(1f, 1f, 1f, 0.7f), false);
                }
            }

            var listen = NewRect("Listen", header, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(300f, 76f));
            listen.pivot = new Vector2(1f, 0.5f);
            Img(listen.gameObject, _round, new Color32(0xF1, 0xEF, 0xFF, 255), true);
            var listenBtn = listen.gameObject.AddComponent<Button>();
            listenBtn.targetGraphic = listen.GetComponent<Image>();
            var listenIcon = NewRect("Icon", listen, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(46f, 0f), new Vector2(30f, 30f));
            var listenArt = Art("purple_icon_sonido.png");
            if (listenArt != null) { listenIcon.sizeDelta = new Vector2(48f, 42f); Img(listenIcon.gameObject, listenArt, Color.white, false).preserveAspect = true; }
            else Img(listenIcon.gameObject, _arrow, Purple, false);
            var listenLabel = Text("Label", listen, "Escuchar /a/", 34, Purple, TextAlignmentOptions.MidlineLeft);
            Place(listenLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(34f, 0f), new Vector2(220f, 60f));
            listenLabel.fontSize = 32;

            // --- instrucciones (izquierda) ---
            var instr = NewRect("NT_Instructions", traceRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(76f, -190f), new Vector2(380f, 470f));
            instr.pivot = new Vector2(0f, 1f);
            Img(instr.gameObject, _round, Color.white, true);
            PanelHeader(instr, "Instrucciones", 380f);
            var list = NewRect("List", instr, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            list.offsetMin = new Vector2(24f, 20f); list.offsetMax = new Vector2(-24f, -96f);
            var vl = list.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 14f; vl.childAlignment = TextAnchor.UpperLeft;
            vl.childControlWidth = vl.childControlHeight = true;
            vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;

            // --- sonido de apoyo (derecha) ---
            var sound = NewRect("NT_Sound", traceRoot, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-76f, -190f), new Vector2(420f, 560f));
            sound.pivot = new Vector2(1f, 1f);
            Img(sound.gameObject, _round, Color.white, true);
            PanelHeader(sound, "Sonido de apoyo", 420f);
            var soundPanel = sound.gameObject.AddComponent<TraceSoundPanel>();
            string[] modeNames = { "Silencio", "Música suave", "Voz guía" };
            var modeButtons = new Button[3];
            string[] modeArt = { "btn_silencio-removebg-preview.png", "low_music.png", "voz_guia.png" };
            for (int i = 0; i < 3; i++)
            {
                var b = NewRect("Mode_" + i, sound, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f + i * 134f, -108f), new Vector2(122f, 142f));
                b.pivot = new Vector2(0f, 1f);
                var art = Art(modeArt[i]);
                modeButtons[i] = b.gameObject.AddComponent<Button>();
                if (art != null)
                {
                    Img(b.gameObject, art, Color.white, false).preserveAspect = true;
                    b.GetComponent<Image>().raycastTarget = true;
                }
                else
                {
                    Img(b.gameObject, _round, Pale, true).raycastTarget = true;
                    var t = Text("L", b, modeNames[i], 26, Navy, TextAlignmentOptions.Center);
                    Stretch(t.rectTransform);
                    t.enableWordWrapping = true;
                }
                modeButtons[i].targetGraphic = b.GetComponent<Image>();
            }
            var volLabel = Text("VolumeLabel", sound, "Volumen medio", 28, Navy, TextAlignmentOptions.MidlineLeft);
            Place(volLabel.rectTransform, new Vector2(0f, 1f), new Vector2(22f, -300f), new Vector2(340f, 44f));
            volLabel.rectTransform.pivot = new Vector2(0f, 0.5f);
            volLabel.fontStyle = FontStyles.Bold;
            var slider = BuildSlider(sound);
            var footerArt = Art("footer_info.png");
            if (footerArt != null)
            {
                var footer = NewRect("Footer", sound, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 56f), new Vector2(348f, 63f));
                Img(footer.gameObject, footerArt, Color.white, false).preserveAspect = true;
            }
            else
            {
                var note = Text("Note", sound, "El sonido es opcional y se puede desactivar.", 24, new Color32(0x6B, 0x70, 0x99, 255), TextAlignmentOptions.Center);
                Place(note.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(340f, 90f));
                note.enableWordWrapping = true;
            }

            var sso = new SerializedObject(soundPanel);
            sso.FindProperty("silenceButton").objectReferenceValue = modeButtons[0];
            sso.FindProperty("musicButton").objectReferenceValue = modeButtons[1];
            sso.FindProperty("voiceButton").objectReferenceValue = modeButtons[2];
            sso.FindProperty("volumeSlider").objectReferenceValue = slider;
            sso.FindProperty("volumeLabel").objectReferenceValue = volLabel;
            sso.FindProperty("musicClip").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/25 Rpg Game Tracks/Ambient 1.wav");
            if (Art("low_music.png") != null)
            {
                // Con imágenes, el modo elegido se ve en blanco pleno y los otros un poco apagados
                sso.FindProperty("selectedColor").colorValue = Color.white;
                sso.FindProperty("normalColor").colorValue = Color.white;
            }
            sso.ApplyModifiedProperties();

            // --- mascota ---
            var mascotSrc = content.Find("Mascot");
            RectTransform mascot = null;
            if (mascotSrc != null)
            {
                mascot = Object.Instantiate(mascotSrc.gameObject, traceRoot).GetComponent<RectTransform>();
                mascot.name = "NT_Mascot";
                mascot.gameObject.SetActive(true);
                mascot.anchorMin = mascot.anchorMax = mascot.pivot = new Vector2(0f, 0f);
                mascot.anchoredPosition = new Vector2(30f, 10f);
                mascot.sizeDelta = new Vector2(340f, 340f);
            }

            // --- botones de la fila inferior ---
            var startArt = Art("btn_inicio_actividad-removebg-preview.png");
            var repeatArt = Art("btn_repet.png");
            var start = startArt != null
                ? MakeSpriteButton(traceRoot, "NT_Start", startArt, new Vector2(-265f, 250f), new Vector2(520f, 99f))
                : MakeButton(traceRoot, "NT_Start", "Comenzar a trazar", Green, Color.white, new Vector2(-265f, 250f), new Vector2(520f, 104f), 44);
            var repeat = repeatArt != null
                ? MakeSpriteButton(traceRoot, "NT_Repeat", repeatArt, new Vector2(275f, 250f), new Vector2(350f, 99f))
                : MakeButton(traceRoot, "NT_Repeat", "Repetir modelo", Color.white, Purple, new Vector2(275f, 250f), new Vector2(330f, 104f), 34);
            var clearRt = (RectTransform)clear.transform;
            var checkRt = (RectTransform)check.transform;
            var nextRt = (RectTransform)next.transform;
            Restyle(clearRt, "Borrar", PurpleLight, new Vector2(-380f, 250f), new Vector2(260f, 104f));
            Restyle(checkRt, "Comprobar", Green, new Vector2(-60f, 250f), new Vector2(300f, 104f));
            Restyle(nextRt, null, Green, new Vector2(275f, 250f), new Vector2(330f, 104f));
            repeat.transform.SetSiblingIndex(traceRoot.childCount - 1);
            // "Repetir modelo" y "Siguiente" comparten sitio: Siguiente se encima solo cuando ya se aprobó, y entonces Repetir queda debajo
            nextRt.SetAsLastSibling();

            var pill = NewRect("NT_StepPill", traceRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 146f), new Vector2(320f, 52f));
            Img(pill.gameObject, _round, new Color32(0xE6, 0xE1, 0xFF, 255), true);
            var pillText = Text("T", pill, "Paso 1 de 2", 34, Purple, TextAlignmentOptions.Center);
            Stretch(pillText.rectTransform);
            pillText.fontStyle = FontStyles.Bold;

            Place(banner, new Vector2(0.5f, 0f), new Vector2(0f, 52f), new Vector2(940f, 76f));
            banner.GetComponent<Image>().sprite = _round;
            banner.GetComponent<Image>().type = Image.Type.Sliced;

            // --- conexiones ---
            so.FindProperty("headerTitle").objectReferenceValue = title;
            so.FindProperty("traceBackButton").objectReferenceValue = backButton;
            var chipsProp = so.FindProperty("stepChips");
            chipsProp.arraySize = 3;
            for (int i = 0; i < 3; i++) chipsProp.GetArrayElementAtIndex(i).objectReferenceValue = chips[i];
            so.FindProperty("startButton").objectReferenceValue = start;
            so.FindProperty("repeatButton").objectReferenceValue = repeat;
            so.FindProperty("listenButton").objectReferenceValue = listenBtn;
            so.FindProperty("listenLabel").objectReferenceValue = listenLabel;
            so.FindProperty("stepPillText").objectReferenceValue = pillText;
            so.FindProperty("instructionsList").objectReferenceValue = list;
            so.FindProperty("soundPanel").objectReferenceValue = soundPanel;
            so.FindProperty("arrowSprite").objectReferenceValue = _arrow;
            if (artSteps)
            {
                so.FindProperty("chipOnColor").colorValue = new Color(1f, 1f, 1f, 0.3f);
                so.FindProperty("chipOffColor").colorValue = new Color(1f, 1f, 1f, 0f);
                so.FindProperty("chipOnScale").floatValue = 1f;
            }
            var hide = so.FindProperty("hideWhileTracing");
            hide.arraySize = mascotSrc != null ? 1 : 0;
            if (mascotSrc != null) hide.GetArrayElementAtIndex(0).objectReferenceValue = mascotSrc.gameObject;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[TraceRedesign] Diseño nuevo aplicado a {scenePath}");
        }

        // ---------- ayudas de construcción ----------

        private const string ArtFolder = "Assets/_Project/Art/images/";

        // Las imágenes del diseño (Art/images) se importan como sprite la primera vez; devuelve null si el archivo no está
        private static Sprite Art(string file)
        {
            string path = ArtFolder + file;
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                }
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return null;
        }

        private static Sprite EnsureArrowSprite()
        {
            const string path = "Assets/_Project/Art/images/ui_trace/arrow.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            // Punta de flecha apuntando a la derecha, con borde suave
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float fx = x / (n - 1f), fy = Mathf.Abs(y / (n - 1f) - 0.5f) * 2f;   // 0..1 / 0..1 desde el eje
                    float limit = 1f - fx;                                                  // el triángulo se estrecha hacia la derecha
                    float edge = Mathf.Clamp01((limit - fy) * n * 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, edge * Mathf.Clamp01(fx * 12f)));
                }
            tex.Apply();
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static RectTransform NewRect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        private static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static Image Img(GameObject go, Sprite sprite, Color color, bool sliced)
        {
            var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            img.sprite = sprite;
            img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            img.color = color;
            img.raycastTarget = go.GetComponent<Button>() != null;
            return img;
        }

        private static TextMeshProUGUI Text(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align)
        {
            var rt = NewRect(name, parent, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(300f, 60f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
            t.fontStyle = FontStyles.Bold;
            t.raycastTarget = false;
            return t;
        }

        private static void PanelHeader(RectTransform panel, string text, float width)
        {
            var h = NewRect("Header", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 76f));
            h.pivot = new Vector2(0.5f, 1f);
            h.offsetMin = new Vector2(0f, -76f); h.offsetMax = Vector2.zero;
            Img(h.gameObject, _round, PurpleLight, true);
            var t = Text("T", h, text, 38, Color.white, TextAlignmentOptions.Center);
            Stretch(t.rectTransform);
        }

        private static Button MakeSpriteButton(Transform parent, string name, Sprite sprite, Vector2 pos, Vector2 size)
        {
            var rt = NewRect(name, parent, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), pos, size);
            var img = Img(rt.gameObject, sprite, Color.white, false);
            img.preserveAspect = true;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            img.raycastTarget = true;
            return btn;
        }

        private static Button MakeButton(Transform parent, string name, string label, Color fill, Color textColor, Vector2 pos, Vector2 size, float fontSize)
        {
            var rt = NewRect(name, parent, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), pos, size);
            var img = Img(rt.gameObject, _round, fill, true);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            img.raycastTarget = true;
            var t = Text("Label", rt, label, fontSize, textColor, TextAlignmentOptions.Center);
            Stretch(t.rectTransform);
            return btn;
        }

        // Deja el botón que ya existía (Borrar, Comprobar, Siguiente) con el estilo nuevo, sin perder su conexión con el reto
        private static void Restyle(RectTransform rt, string label, Color fill, Vector2 pos, Vector2 size)
        {
            Place(rt, new Vector2(0.5f, 0f), pos, size);
            rt.pivot = new Vector2(0.5f, 0.5f);
            var img = rt.GetComponent<Image>();
            img.sprite = _round; img.type = Image.Type.Sliced; img.color = fill;
            var t = rt.GetComponentInChildren<TMP_Text>(true);
            if (t != null)
            {
                if (label != null) t.text = label;
                t.fontSize = 40;
                t.color = Color.white;
            }
        }

        private static Slider BuildSlider(RectTransform parent)
        {
            var root = NewRect("Volume", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -380f), new Vector2(320f, 44f));
            root.pivot = new Vector2(0f, 0.5f);
            var track = NewRect("Track", root, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(0f, 16f));
            Img(track.gameObject, _round, new Color32(0xDD, 0xDD, 0xE8, 255), true);
            var fillArea = NewRect("Fill Area", root, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(-30f, 16f));
            var fill = NewRect("Fill", fillArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            Img(fill.gameObject, _round, Purple, true);
            var handleArea = NewRect("Handle Slide Area", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            handleArea.offsetMin = new Vector2(15f, 0f); handleArea.offsetMax = new Vector2(-15f, 0f);
            var handle = NewRect("Handle", handleArea, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(44f, 44f));
            var handleImg = Img(handle.gameObject, _circle, Purple, false);
            handleImg.raycastTarget = true;

            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f; slider.maxValue = 1f; slider.value = 0.7f;
            return slider;
        }
    }
}
