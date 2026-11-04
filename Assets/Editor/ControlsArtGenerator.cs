#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VamosAprendiendo.EditorTools
{
    /// <summary>
    /// Genera las imágenes de los controles táctiles con la misma paleta del botón de pausa (índigo, aro blanco, cara
    /// violeta-azul con brillo): base y mando del joystick de caminar, botón de salto y señal de mover la cámara.
    /// </summary>
    public static class ControlsArtGenerator
    {
        private const string DIR = "Assets/_Project/Art/images/";
        private const int SIZE = 512;
        private const int SAMPLES = 3;
        private const float C = SIZE * 0.5f;

        private static readonly Color Indigo = Hex("3B3DB3");
        private static readonly Color IndigoDark = Hex("2A2B8F");
        private static readonly Color RingTop = Hex("FFFFFF");
        private static readonly Color RingBottom = Hex("D5DAFF");
        private static readonly Color RimDark = Hex("4A4CC6");
        private static readonly Color FaceTop = Hex("9A86FF");
        private static readonly Color FaceBottom = Hex("4752DD");
        private static readonly Color Glow = Hex("C9BEFF");

        private enum Kind { JoyBase, JoyKnob, Jump, Camera, AudioOn, AudioOff }

        [MenuItem("Tools/Art/Generate Control Buttons (Joystick, Jump, Camera)", false, 51)]
        public static void Generate()
        {
            Render(Kind.JoyBase, "joy_base.png");
            Render(Kind.JoyKnob, "joy_knob.png");
            Render(Kind.Jump, "btn_jump.png");
            Render(Kind.Camera, "btn_camera.png");
            Render(Kind.AudioOn, "btn_audio_on.png");
            Render(Kind.AudioOff, "btn_audio_off.png");
        }

        // Pone las imágenes en los controles que ya existen en BaseScene, sin tocar su posición ni su tamaño.
        [MenuItem("Tools/Mobile/Apply Control Art to BaseScene", false, 4)]
        public static void ApplyToScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.path.Contains("BaseScene")) scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/BaseScene.unity", OpenSceneMode.Single);

            var joyBase = Load("joy_base.png");
            var joyKnob = Load("joy_knob.png");
            var jump = Load("btn_jump.png");
            var cam = Load("btn_camera.png");
            if (joyBase == null || joyKnob == null || jump == null || cam == null)
            {
                Debug.LogError("[ControlsArt] Faltan imágenes. Ejecuta Tools/Art/Generate Control Buttons.");
                return;
            }

            var root = GameObject.Find("Controls_Root");
            if (root == null) { Debug.LogError("[ControlsArt] No existe Controls_Root en la escena."); return; }

            SetImage(root.transform.Find("Joystick_Area/Joystick_Background"), joyBase, Color.white);
            SetImage(root.transform.Find("Joystick_Area/Joystick_Background/Joystick_Handle"), joyKnob, Color.white);

            var jumpRt = root.transform.Find("Btn_Jump");
            if (jumpRt != null)
            {
                SetImage(jumpRt, jump, Color.white);
                var label = jumpRt.Find("Label") as RectTransform;
                if (label != null)
                {
                    // La flecha ocupa la parte alta del botón; el texto queda debajo.
                    label.anchorMin = new Vector2(0f, 0.2f);
                    label.anchorMax = new Vector2(1f, 0.4f);
                    label.offsetMin = label.offsetMax = Vector2.zero;
                    var tmp = label.GetComponent<TMPro.TextMeshProUGUI>();
                    if (tmp != null) { tmp.fontSize = 26f; tmp.outlineWidth = 0.2f; tmp.outlineColor = new Color32(42, 43, 143, 255); }
                }
            }

            // Señal de "mover la cámara": solo dibujo, no recibe toques (la cámara se mueve arrastrando en la mitad derecha).
            var old = root.transform.Find("Cam_Hint");
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var hint = new GameObject("Cam_Hint", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            hint.SetParent(root.transform, false);
            hint.anchorMin = hint.anchorMax = hint.pivot = new Vector2(1f, 0f);
            hint.anchoredPosition = new Vector2(-180f, 260.7f); // centrado sobre el botón de salto (su borde superior está a 260 px del borde)
            hint.sizeDelta = new Vector2(120f, 120f);
            var img = hint.GetComponent<Image>();
            img.sprite = cam;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = new Color(1f, 1f, 1f, 0.8f);

            // El centro del salto queda a 240 px del borde derecho y a 160 px del inferior; el joystick en reposo se alinea con él
            // (VirtualJoystick: restMargin 110 a la izquierda y restMarginBottom 30 abajo, más su radio de 130).
            if (jumpRt != null) ((RectTransform)jumpRt).anchoredPosition = new Vector2(-140f, 60f);
            var joyArea = root.transform.Find("Joystick_Area");
            if (joyArea != null)
            {
                // La zona táctil es la mitad izquierda (hasta el 70 % de alto), sin desplazamientos: si se arrastra con la
                // herramienta de rectángulo, el mando en reposo queda fuera de la esquina.
                var jar = (RectTransform)joyArea;
                jar.anchorMin = new Vector2(0f, 0f);
                jar.anchorMax = new Vector2(0.5f, 0.7f);
                jar.pivot = new Vector2(0.5f, 0.5f);
                jar.offsetMin = jar.offsetMax = Vector2.zero;
            }
            var joy = joyArea != null ? joyArea.GetComponent<VamosAprendiendo.UI.VirtualJoystick>() : null;
            if (joy != null)
            {
                var jso = new SerializedObject(joy);
                jso.FindProperty("restMargin").floatValue = 110f;
                jso.FindProperty("restMarginBottom").floatValue = 30f;
                jso.ApplyModifiedProperties();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ControlsArt] Imágenes aplicadas a los controles de BaseScene.");
        }

        [MenuItem("Tools/Mobile/Add Audio Button to BaseScene", false, 5)]
        public static void AddAudioButton()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.path.Contains("BaseScene")) scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/BaseScene.unity", OpenSceneMode.Single);

            var on = Load("btn_audio_on.png");
            var off = Load("btn_audio_off.png");
            if (on == null || off == null) { Debug.LogError("[ControlsArt] Faltan btn_audio_on/off. Ejecuta Tools/Art/Generate Control Buttons."); return; }

            foreach (var r in scene.GetRootGameObjects())
            {
                if (r.name == "AudioButtonCanvas") UnityEngine.Object.DestroyImmediate(r);
            }

            var canvasObj = new GameObject("AudioButtonCanvas");
            Undo.RegisterCreatedObjectUndo(canvasObj, "Audio Button");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150; // como el botón de pausa: se puede usar con el menú de pausa abierto
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;
            canvasObj.AddComponent<GraphicRaycaster>();

            var rt = new GameObject("Btn_Audio", typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(canvasObj.transform, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-40f, -40f); // espejo del botón de pausa
            rt.sizeDelta = new Vector2(130f, 130f);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = on;
            img.preserveAspect = true;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;

            var ui = canvasObj.AddComponent<VamosAprendiendo.UI.AudioToggleButton>();
            var so = new SerializedObject(ui);
            so.FindProperty("button").objectReferenceValue = btn;
            so.FindProperty("icon").objectReferenceValue = img;
            so.FindProperty("onSprite").objectReferenceValue = on;
            so.FindProperty("offSprite").objectReferenceValue = off;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ControlsArt] AudioButtonCanvas añadido a BaseScene.");
        }

        private static Sprite Load(string file) => AssetDatabase.LoadAssetAtPath<Sprite>(DIR + file);

        private static void SetImage(Transform t, Sprite sprite, Color color)
        {
            if (t == null) return;
            var img = t.GetComponent<Image>();
            if (img == null) return;
            UnityEditor.Undo.RecordObject(img, "Control art");
            img.sprite = sprite;
            img.color = color;
            img.preserveAspect = true;
            EditorUtility.SetDirty(img);
        }

        private static void Render(Kind kind, string file)
        {
            var pixels = new Color[SIZE * SIZE];
            for (int y = 0; y < SIZE; y++)
            {
                for (int x = 0; x < SIZE; x++)
                {
                    Color acc = Color.clear;
                    for (int sy = 0; sy < SAMPLES; sy++)
                    {
                        for (int sx = 0; sx < SAMPLES; sx++)
                        {
                            acc += Shade(kind, x + (sx + 0.5f) / SAMPLES, y + (sy + 0.5f) / SAMPLES);
                        }
                    }
                    acc /= SAMPLES * SAMPLES;
                    if (acc.a > 0.0001f) acc = new Color(acc.r / acc.a, acc.g / acc.a, acc.b / acc.a, acc.a);
                    pixels[y * SIZE + x] = acc;
                }
            }

            var tex = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32, false);
            tex.SetPixels(pixels);
            tex.Apply();
            string path = DIR + file;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            Debug.Log("[ControlsArt] Imagen generada: " + path);
        }

        private static Color Shade(Kind kind, float px, float py)
        {
            float dx = px - C, dy = py - C;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float v = Mathf.Clamp01((py - 16f) / (SIZE - 32f));
            Color col = Color.clear;

            if (kind == Kind.JoyBase)
            {
                // Base translúcida para que se vea el paisaje detrás; aro blanco y flechas de dirección.
                if (r < 246f) col = Over(col, Premul(Color.Lerp(IndigoDark, Indigo, v), Edge(246f - r) * 0.9f));
                if (r < 232f) col = Over(col, Premul(Color.Lerp(RingBottom, RingTop, v), Edge(232f - r) * 0.95f));
                if (r < 214f) col = Over(col, Premul(new Color(0.16f, 0.17f, 0.55f), Edge(214f - r) * 0.55f));
                if (r < 205f) col = Over(col, Premul(Color.Lerp(new Color(0.28f, 0.3f, 0.85f), new Color(0.5f, 0.45f, 0.95f), v), Edge(205f - r) * 0.32f));
                // Guía circular interior donde reposa el mando
                float guide = Mathf.Clamp01(0.5f - (Mathf.Abs(r - 104f) - 3f)) * 0.4f;
                col = Over(col, Premul(Color.white, guide));
                // Cuatro flechas
                float a = 0f;
                a = Mathf.Max(a, Chevron(px, py, new Vector2(C, C + 160f), Vector2.up, 28f, 9f));
                a = Mathf.Max(a, Chevron(px, py, new Vector2(C, C - 160f), Vector2.down, 28f, 9f));
                a = Mathf.Max(a, Chevron(px, py, new Vector2(C - 160f, C), Vector2.left, 28f, 9f));
                a = Mathf.Max(a, Chevron(px, py, new Vector2(C + 160f, C), Vector2.right, 28f, 9f));
                col = Over(col, Premul(Color.white, a * 0.92f));
                return col;
            }

            // Botones con la misma construcción que el de pausa
            float scale = kind == Kind.JoyKnob ? 1f : 1f;
            float sr = Mathf.Sqrt(dx * dx + (dy + 12f) * (dy + 12f));
            col = Over(col, Premul(new Color(0.08f, 0.08f, 0.3f, 1f), Mathf.Clamp01((246f - sr) / 22f) * 0.28f));
            if (r < 238f * scale) col = Over(col, Premul(Color.Lerp(IndigoDark, Indigo, v), Edge(238f - r)));
            if (r < 224f) col = Over(col, Premul(Color.Lerp(RingBottom, RingTop, v), Edge(224f - r)));
            if (r < 186f) col = Over(col, Premul(RimDark, Edge(186f - r)));
            if (r < 176f)
            {
                Color face = kind == Kind.AudioOff ? Color.Lerp(Hex("5E6190"), Hex("9396BD"), v) : Color.Lerp(FaceBottom, FaceTop, v);
                float lx = px - (C - 55f), ly = py - (C + 70f);
                face = Color.Lerp(face, Glow, Mathf.Clamp01(1f - Mathf.Sqrt(lx * lx + ly * ly) / 190f) * 0.45f);
                col = Over(col, Premul(face, Edge(176f - r)));

                float ex = dx / 118f, ey = (py - (C + 112f)) / 52f;
                float e = 1f - Mathf.Sqrt(ex * ex + ey * ey);
                float gloss = Mathf.Clamp01(e * 6f) * Mathf.Lerp(0.15f, 0.42f, Mathf.Clamp01(e));
                col = Over(col, new Color(gloss, gloss, gloss, gloss));
            }

            float icon = 0f, iconShadow = 0f;
            if (kind == Kind.Jump)
            {
                icon = Mathf.Max(
                    ChevronLines(px, py, new Vector2(C, C + 96f), 58f, 17f),
                    ChevronLines(px, py, new Vector2(C, C + 24f), 58f, 17f));
                iconShadow = Mathf.Max(
                    ChevronLines(px, py + 8f, new Vector2(C, C + 96f), 58f, 17f),
                    ChevronLines(px, py + 8f, new Vector2(C, C + 24f), 58f, 17f));
            }
            else if (kind == Kind.JoyKnob)
            {
                // Cruz de cuatro flechas pequeñas: indica que se arrastra en cualquier dirección.
                icon = Mathf.Max(
                    Mathf.Max(Chevron(px, py, new Vector2(C, C + 100f), Vector2.up, 24f, 9f), Chevron(px, py, new Vector2(C, C - 100f), Vector2.down, 24f, 9f)),
                    Mathf.Max(Chevron(px, py, new Vector2(C - 100f, C), Vector2.left, 24f, 9f), Chevron(px, py, new Vector2(C + 100f, C), Vector2.right, 24f, 9f)));
                float ring = Mathf.Clamp01(0.5f - (Mathf.Abs(r - 52f) - 8f));
                icon = Mathf.Max(icon, ring);
            }
            else if (kind == Kind.AudioOn || kind == Kind.AudioOff)
            {
                icon = SpeakerIcon(px, py, kind == Kind.AudioOn);
                iconShadow = SpeakerIcon(px, py + 8f, kind == Kind.AudioOn);
            }
            else if (kind == Kind.Camera)
            {
                icon = CameraIcon(px, py, 0f);
                iconShadow = CameraIcon(px, py + 8f, 0f);
            }

            if (iconShadow > 0f) col = Over(col, Premul(new Color(0.1f, 0.1f, 0.45f), iconShadow * 0.4f));
            if (icon > 0f)
            {
                float bv = Mathf.Clamp01((py - (C - 90f)) / 180f);
                col = Over(col, Premul(Color.Lerp(Hex("DCE0FF"), Color.white, bv), icon));
            }

            float hx = px - (C - 108f), hy = py - (C + 112f);
            float dot = Mathf.Clamp01((13f - Mathf.Sqrt(hx * hx + hy * hy)) / 2f) * 0.9f;
            col = Over(col, new Color(dot, dot, dot, dot));
            return col;
        }

        // Flecha en "V" (apunta hacia dir) con punta en tip, brazos de length y grosor hw.
        private static float Chevron(float px, float py, Vector2 tip, Vector2 dir, float length, float hw)
        {
            Vector2 perp = new Vector2(-dir.y, dir.x);
            Vector2 a = tip - dir * length + perp * length;
            Vector2 b = tip - dir * length - perp * length;
            Vector2 p = new Vector2(px, py);
            return Mathf.Max(Seg(p, a, tip, hw), Seg(p, b, tip, hw));
        }

        // Chevron ancho hacia arriba centrado en c: punta arriba, brazos hacia abajo.
        private static float ChevronLines(float px, float py, Vector2 c, float halfWidth, float hw)
        {
            var p = new Vector2(px, py);
            var tip = c + new Vector2(0f, halfWidth * 0.62f);
            var l = c + new Vector2(-halfWidth, -halfWidth * 0.34f);
            var r = c + new Vector2(halfWidth, -halfWidth * 0.34f);
            return Mathf.Max(Seg(p, l, tip, hw), Seg(p, r, tip, hw));
        }

        // Dos flechas curvas que giran alrededor de un círculo central: "rotar la cámara".
        private static float CameraIcon(float px, float py, float unused)
        {
            var p = new Vector2(px, py);
            float cover = 0f;
            const float radius = 92f, hw = 15f;
            float dx = px - C, dy = py - C;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg; // -180..180
            // Arco superior: de 25° a 150°, con punta en 25° (sentido horario). Arco inferior: de 205° a 330°, punta en 205°.
            float arcTop = InArc(ang, 40f, 150f) ? Mathf.Clamp01(0.5f - (Mathf.Abs(dist - radius) - hw)) : 0f;
            float arcBot = InArc(ang, -140f, -30f) ? Mathf.Clamp01(0.5f - (Mathf.Abs(dist - radius) - hw)) : 0f;
            // Los extremos redondos del arco
            cover = Mathf.Max(arcTop, arcBot);
            cover = Mathf.Max(cover, Disc(p, AtAngle(150f, radius), hw), Disc(p, AtAngle(-140f, radius), hw));
            // Puntas de flecha (tangentes) en los extremos 40° y -30°
            cover = Mathf.Max(cover, ArrowHead(p, 40f, radius, -1f));
            cover = Mathf.Max(cover, ArrowHead(p, -30f, radius, -1f));
            // Punto central ("ojo" de la cámara)
            cover = Mathf.Max(cover, Disc(p, new Vector2(C, C), 26f));
            return cover;
        }

        // Altavoz con ondas (sonido activo) o con una cruz (silenciado).
        private static float SpeakerIcon(float px, float py, bool on)
        {
            var p = new Vector2(px, py);
            float body = Mathf.Max(
                Mathf.Clamp01(0.5f - (TriangleDistance(p, new Vector2(C - 98f, C + 34f), new Vector2(C - 98f, C - 34f), new Vector2(C - 46f, C - 34f)) - 8f)),
                Mathf.Clamp01(0.5f - (TriangleDistance(p, new Vector2(C - 98f, C + 34f), new Vector2(C - 46f, C - 34f), new Vector2(C - 46f, C + 34f)) - 8f)));
            float cone = Mathf.Max(
                Mathf.Clamp01(0.5f - (TriangleDistance(p, new Vector2(C - 46f, C + 34f), new Vector2(C - 46f, C - 34f), new Vector2(C + 22f, C - 84f)) - 8f)),
                Mathf.Clamp01(0.5f - (TriangleDistance(p, new Vector2(C - 46f, C + 34f), new Vector2(C + 22f, C - 84f), new Vector2(C + 22f, C + 84f)) - 8f)));
            float cover = Mathf.Max(body, cone);

            if (on)
            {
                float dx = px - (C + 22f), dy = py - C, d = Mathf.Sqrt(dx * dx + dy * dy);
                float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                if (Mathf.Abs(ang) < 40f)
                {
                    cover = Mathf.Max(cover, Mathf.Clamp01(0.5f - (Mathf.Abs(d - 58f) - 11f)));
                    cover = Mathf.Max(cover, Mathf.Clamp01(0.5f - (Mathf.Abs(d - 104f) - 11f)));
                }
            }
            else
            {
                cover = Mathf.Max(cover, Seg(p, new Vector2(C + 54f, C - 40f), new Vector2(C + 112f, C + 40f), 12f));
                cover = Mathf.Max(cover, Seg(p, new Vector2(C + 54f, C + 40f), new Vector2(C + 112f, C - 40f), 12f));
            }
            return cover;
        }

        private static bool InArc(float ang, float from, float to) => ang >= from && ang <= to;
        private static Vector2 AtAngle(float deg, float radius) => new Vector2(C, C) + new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad)) * radius;
        private static float Disc(Vector2 p, Vector2 c, float rad) => Mathf.Clamp01(0.5f - (Vector2.Distance(p, c) - rad));

        // Punta de flecha en el ángulo deg del círculo, apuntando en sentido tangente (sign -1 = horario).
        private static float ArrowHead(Vector2 p, float deg, float radius, float sign)
        {
            Vector2 pos = AtAngle(deg, radius);
            float rad = deg * Mathf.Deg2Rad;
            Vector2 tangent = new Vector2(-Mathf.Sin(rad), Mathf.Cos(rad)) * sign; // sentido de giro
            Vector2 outward = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            Vector2 tip = pos + tangent * 38f;
            Vector2 a = pos + outward * 40f - tangent * 4f;
            Vector2 b = pos - outward * 40f - tangent * 4f;
            // Triángulo relleno redondeado
            float d = TriangleDistance(p, a, b, tip) - 6f;
            return Mathf.Clamp01(0.5f - d);
        }

        private static float TriangleDistance(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            Vector2 e0 = b - a, e1 = c - b, e2 = a - c;
            Vector2 v0 = p - a, v1 = p - b, v2 = p - c;
            Vector2 pq0 = v0 - e0 * Mathf.Clamp01(Vector2.Dot(v0, e0) / Vector2.Dot(e0, e0));
            Vector2 pq1 = v1 - e1 * Mathf.Clamp01(Vector2.Dot(v1, e1) / Vector2.Dot(e1, e1));
            Vector2 pq2 = v2 - e2 * Mathf.Clamp01(Vector2.Dot(v2, e2) / Vector2.Dot(e2, e2));
            float s = Mathf.Sign(e0.x * e2.y - e0.y * e2.x);
            float d0 = Mathf.Min(Vector2.Dot(pq0, pq0), Mathf.Min(Vector2.Dot(pq1, pq1), Vector2.Dot(pq2, pq2)));
            float s0 = Mathf.Min(s * (v0.x * e0.y - v0.y * e0.x), Mathf.Min(s * (v1.x * e1.y - v1.y * e1.x), s * (v2.x * e2.y - v2.y * e2.x)));
            return -Mathf.Sqrt(d0) * Mathf.Sign(s0);
        }

        // Cobertura de un segmento con extremos redondos.
        private static float Seg(Vector2 p, Vector2 a, Vector2 b, float hw)
        {
            Vector2 pa = p - a, ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            float d = (pa - ba * h).magnitude - hw;
            return Mathf.Clamp01(0.5f - d);
        }

        private static float Edge(float d) => Mathf.Clamp01(d);

        private static Color Premul(Color c, float coverage)
        {
            float a = c.a * coverage;
            return new Color(c.r * a, c.g * a, c.b * a, a);
        }

        private static Color Over(Color dst, Color src)
        {
            float inv = 1f - src.a;
            return new Color(src.r + dst.r * inv, src.g + dst.g * inv, src.b + dst.b * inv, src.a + dst.a * inv);
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color c);
            return c;
        }
    }
}
#endif
