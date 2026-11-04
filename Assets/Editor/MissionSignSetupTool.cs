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
    /// Deja listo el signo de fin de misión "?" de BaseScene: le agrega <see cref="MissionSign"/> y su collider, lo enlaza con el
    /// cofre y con los efectos de Magic Effects FREE, y crea el prefab del panel de recompensa (con arte de madera generado
    /// por código, porque el paquete Wooden GUI del proyecto no trae sus imágenes). Se puede repetir sin duplicar nada.
    /// </summary>
    public static class MissionSignSetupTool
    {
        private const string BASE_SCENE_PATH = "Assets/_Project/Scenes/BaseScene.unity";
        private const string ART_DIR = "Assets/_Project/Art/images/ui_reward";
        private const string PANEL_PREFAB_PATH = "Assets/_Project/Prefabs/UI/MissionRewardPanel.prefab";
        private const string FX_DIR = "Assets/_ThirdParty/VFX/Hovl Studio/Magic effects pack/Prefabs/";

        // ---------------------------------------------------------------- escena

        [MenuItem("Tools/Scenes/Setup Mission Sign (BaseScene)", false, 43)]
        public static void SetupScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.path.Contains("BaseScene")) scene = EditorSceneManager.OpenScene(BASE_SCENE_PATH, OpenSceneMode.Single);

            var panelPrefab = BuildPanelPrefab();

            var signObject = FindInScene(scene, "finish_mision");
            var chestObject = FindInScene(scene, "Chest");
            if (signObject == null || chestObject == null) return;

            // Collider generoso (el modelo es pequeño) para poder tocarlo con el dedo
            var box = signObject.GetComponent<BoxCollider>();
            if (box == null) box = Undo.AddComponent<BoxCollider>(signObject.gameObject);
            var mesh = signObject.GetComponent<MeshFilter>().sharedMesh;
            Vector3 scale = signObject.lossyScale;
            box.center = mesh.bounds.center;
            box.size = new Vector3(1.3f / scale.x, 1.7f / scale.y, 1.1f / scale.z);

            // Punto sobre el cofre abierto: adonde vuela el signo y donde estalla la magia
            var targetName = "Chest_MagicPoint";
            var target = chestObject.Find(targetName);
            if (target == null)
            {
                var go = new GameObject(targetName);
                Undo.RegisterCreatedObjectUndo(go, "Chest magic point");
                go.transform.SetParent(chestObject, false);
                target = go.transform;
            }
            var bounds = new Bounds(chestObject.position, Vector3.zero);
            foreach (var r in chestObject.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);
            target.position = new Vector3(bounds.center.x, bounds.center.y + 0.6f, bounds.center.z);

            var sign = signObject.GetComponent<MissionSign>();
            if (sign == null) sign = Undo.AddComponent<MissionSign>(signObject.gameObject);

            var so = new SerializedObject(sign);
            so.FindProperty("chest").objectReferenceValue = chestObject.GetComponent<HingeOpener>();
            so.FindProperty("chestTarget").objectReferenceValue = target;
            so.FindProperty("panelPrefab").objectReferenceValue = panelPrefab;
            so.FindProperty("auraPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(FX_DIR + "Character auras/Star aura.prefab");
            so.FindProperty("appearPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(FX_DIR + "Hits and explosions/Holy hit.prefab");
            so.FindProperty("openPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(FX_DIR + "Hits and explosions/Star hit.prefab");
            so.FindProperty("maxDistance").floatValue = 14f;
            so.ApplyModifiedProperties();

            // El cofre ahora se abre desde el signo (así entrega su recompensa): se desactiva el toque directo sobre el cofre
            var chestTap = chestObject.GetComponent<TapToOpen>();
            if (chestTap != null) chestTap.enabled = false;

            SetupNewMissionSign(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[MissionSign] Listo: la '?' aparece al completar una ruta y abre el cofre con la recompensa de XP; la '!' anuncia la misión nueva.");
        }

        // "!" de misión nueva (send mission), junto al pulpo
        private static void SetupNewMissionSign(Scene scene)
        {
            var signObject = FindInScene(scene, "send mission");
            var octopus = FindInScene(scene, "Octopus_Rigged_Unity");
            if (signObject == null || octopus == null) return;

            // Collider en la malla (el modelo es pequeño y está girado): al menos ~1,1 m para poder tocarlo con el dedo
            var meshFilter = signObject.GetComponentInChildren<MeshFilter>();
            var box = meshFilter.GetComponent<BoxCollider>();
            if (box == null) box = Undo.AddComponent<BoxCollider>(meshFilter.gameObject);
            Vector3 scale = meshFilter.transform.lossyScale;
            Vector3 size = meshFilter.sharedMesh.bounds.size * 1.6f;
            box.center = meshFilter.sharedMesh.bounds.center;
            box.size = new Vector3(
                Mathf.Max(size.x, 1.1f / scale.x), Mathf.Max(size.y, 1.1f / scale.y), Mathf.Max(size.z, 1.1f / scale.z));

            var sign = signObject.GetComponent<NewMissionSign>();
            if (sign == null) sign = Undo.AddComponent<NewMissionSign>(signObject.gameObject);

            var so = new SerializedObject(sign);
            so.FindProperty("octopus").objectReferenceValue = octopus.GetComponent<TapToLoadScene>();
            // Aura amarilla ("Buff") y destello dorado; la "?" usa estrellas moradas para distinguirse
            so.FindProperty("auraPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(FX_DIR + "Character auras/Buff.prefab");
            so.FindProperty("appearPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(FX_DIR + "Hits and explosions/Holy hit.prefab");
            // Rebota y se balancea (no gira) para que se vea siempre de frente; es más inquieta que la "?"
            so.FindProperty("auraScale").floatValue = 0.45f;
            so.FindProperty("bounce").boolValue = true;
            so.FindProperty("bobHeight").floatValue = 0.28f;
            so.FindProperty("bobSpeed").floatValue = 3.2f;
            so.FindProperty("spinDegreesPerSecond").floatValue = 0f;
            so.FindProperty("swayDegrees").floatValue = 22f;
            so.FindProperty("pulse").floatValue = 0.04f;
            so.FindProperty("centerHeight").floatValue = 0f; // el pivote del modelo está en su centro
            so.FindProperty("maxDistance").floatValue = 18f;
            so.ApplyModifiedProperties();
        }

        private static Transform FindInScene(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name) return t;
            Debug.LogError("[MissionSign] No se encontró " + name + " en la escena.");
            return null;
        }

        // ---------------------------------------------------------------- panel de recompensa

        public static MissionRewardPanel BuildPanelPrefab()
        {
            Directory.CreateDirectory(ART_DIR);
            var panelSprite = SaveSprite("reward_panel", MakePanel(256, 256), new Vector4(86, 86, 86, 86));
            var ribbonSprite = SaveSprite("reward_ribbon", MakeRibbon(512, 160), new Vector4(80, 50, 80, 50));
            var buttonSprite = SaveSprite("reward_button", MakeButton(320, 120), new Vector4(60, 40, 60, 40));
            var starSprite = SaveSprite("reward_star", MakeStar(128), Vector4.zero);

            var root = new GameObject("MissionRewardPanel", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 600; // sobre los controles táctiles y el menú de pausa
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();
            var group = root.AddComponent<CanvasGroup>();

            // Velo oscuro que también bloquea los toques al juego
            var dim = NewRect("Velo", root.transform);
            Stretch(dim);
            var dimImage = dim.gameObject.AddComponent<Image>();
            dimImage.color = new Color(0.06f, 0.04f, 0.14f, 0.62f);

            var card = NewRect("Tarjeta", root.transform);
            card.sizeDelta = new Vector2(900f, 700f);
            var cardImage = card.gameObject.AddComponent<Image>();
            cardImage.sprite = panelSprite;
            cardImage.type = Image.Type.Sliced;

            var ribbon = NewRect("Cinta", card);
            ribbon.anchorMin = ribbon.anchorMax = new Vector2(0.5f, 1f);
            ribbon.sizeDelta = new Vector2(780f, 150f);
            ribbon.anchoredPosition = new Vector2(0f, -8f);
            var ribbonImage = ribbon.gameObject.AddComponent<Image>();
            ribbonImage.sprite = ribbonSprite;
            ribbonImage.type = Image.Type.Sliced;
            var title = NewText("Título", ribbon, "¡Cofre del tesoro!", 62, Color.white, FontStyles.Bold);
            Stretch(title.rectTransform);
            title.rectTransform.offsetMin = new Vector2(30f, 18f);
            title.rectTransform.offsetMax = new Vector2(-30f, -10f);

            var brown = new Color(0.31f, 0.16f, 0.08f);
            var softBrown = new Color(0.45f, 0.29f, 0.17f);
            var route = NewText("Ruta", card, "Ruta", 54, brown, FontStyles.Bold);
            Place(route.rectTransform, new Vector2(0f, 150f), new Vector2(780f, 70f));
            var caption = NewText("Descripción", card, "Experiencia ganada", 34, softBrown, FontStyles.Normal);
            Place(caption.rectTransform, new Vector2(0f, 78f), new Vector2(780f, 50f));

            var star = NewRect("Estrella", card);
            star.sizeDelta = new Vector2(130f, 130f);
            star.anchoredPosition = new Vector2(-250f, -22f);
            var starImage = star.gameObject.AddComponent<Image>();
            starImage.sprite = starSprite;
            starImage.preserveAspect = true;
            var xp = NewText("XP", card, "...", 128, new Color(0.88f, 0.42f, 0.05f), FontStyles.Bold);
            Place(xp.rectTransform, new Vector2(60f, -22f), new Vector2(620f, 150f));
            var total = NewText("Total", card, "", 38, brown, FontStyles.Normal);
            Place(total.rectTransform, new Vector2(0f, -132f), new Vector2(780f, 56f));

            var buttonRect = NewRect("Botón ¡Genial!", card);
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.sizeDelta = new Vector2(400f, 118f);
            buttonRect.anchoredPosition = new Vector2(0f, 98f);
            var buttonImage = buttonRect.gameObject.AddComponent<Image>();
            buttonImage.sprite = buttonSprite;
            buttonImage.type = Image.Type.Sliced;
            var button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            var buttonText = NewText("Texto", buttonRect, "¡Genial!", 54, Color.white, FontStyles.Bold);
            Stretch(buttonText.rectTransform);

            var panel = root.AddComponent<MissionRewardPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("group").objectReferenceValue = group;
            so.FindProperty("card").objectReferenceValue = card;
            so.FindProperty("routeText").objectReferenceValue = route;
            so.FindProperty("captionText").objectReferenceValue = caption;
            so.FindProperty("xpText").objectReferenceValue = xp;
            so.FindProperty("totalText").objectReferenceValue = total;
            so.FindProperty("starIcon").objectReferenceValue = star.gameObject;
            so.FindProperty("closeButton").objectReferenceValue = button;
            so.ApplyModifiedProperties();

            Directory.CreateDirectory(Path.GetDirectoryName(PANEL_PREFAB_PATH));
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PANEL_PREFAB_PATH);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            return prefab.GetComponent<MissionRewardPanel>();
        }

        // ---------------------------------------------------------------- utilidades de interfaz

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rt, Vector2 position, Vector2 size)
        {
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        private static TMP_Text NewText(string name, Transform parent, string content, float size, Color color, FontStyles style)
        {
            var rt = NewRect(name, parent);
            var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.enableWordWrapping = false;
            return text;
        }

        // ---------------------------------------------------------------- arte de madera (procedural)

        private static Sprite SaveSprite(string name, Texture2D texture, Vector4 border)
        {
            string path = $"{ART_DIR}/{name}.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private delegate Color Shader2D(float x, float y);

        // Muestreo 3×3 por píxel para que los bordes salgan suaves
        private static Texture2D Render(int w, int h, Shader2D shade)
        {
            const int ss = 3;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float r = 0f, g = 0f, b = 0f, a = 0f;
                    for (int sy = 0; sy < ss; sy++)
                        for (int sx = 0; sx < ss; sx++)
                        {
                            Color c = shade(x + (sx + 0.5f) / ss, y + (sy + 0.5f) / ss);
                            r += c.r * c.a; g += c.g * c.a; b += c.b * c.a; a += c.a;
                        }
                    float n = ss * ss;
                    pixels[y * w + x] = a > 0f ? new Color(r / a, g / a, b / a, a / n) : Color.clear;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static float Hash(int x, int y)
        {
            unchecked
            {
                int n = x * 374761393 + y * 668265263;
                n = (n ^ (n >> 13)) * 1274126177;
                return ((n ^ (n >> 16)) & 0x7fffffff) / (float)0x7fffffff;
            }
        }

        private static float Noise(float x, float y)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Hash(xi, yi), b = Hash(xi + 1, yi), c = Hash(xi, yi + 1), d = Hash(xi + 1, yi + 1);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        // Distancia con signo a un rectángulo redondeado (negativa dentro)
        private static float RoundRect(float x, float y, float w, float h, float radius)
        {
            float qx = Mathf.Abs(x - w * 0.5f) - (w * 0.5f - radius);
            float qy = Mathf.Abs(y - h * 0.5f) - (h * 0.5f - radius);
            return new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        }

        // Tablones de madera horizontales con veta, junta oscura y variación de tono
        private static Color Wood(float x, float y, Color dark, Color light, float plank)
        {
            int row = Mathf.FloorToInt(y / plank);
            float local = y / plank - row;
            Color c = Color.Lerp(dark, light, 0.25f + 0.5f * Hash(row, 7));
            float grain = Noise(x * 0.025f + row * 9.1f, y * 0.35f) * 0.22f - 0.11f;
            grain += (Noise(x * 0.12f, y * 0.9f + row) - 0.5f) * 0.06f;
            c = new Color(c.r * (1f + grain), c.g * (1f + grain), c.b * (1f + grain), 1f);
            if (local < 0.07f || local > 0.93f) c = new Color(c.r * 0.62f, c.g * 0.62f, c.b * 0.62f, 1f);
            return c;
        }

        private static Texture2D MakePanel(int w, int h)
        {
            var dark = new Color(0.40f, 0.22f, 0.10f);
            var light = new Color(0.66f, 0.42f, 0.20f);
            const float radius = 52f, frame = 40f;
            return Render(w, h, (x, y) =>
            {
                float d = RoundRect(x, y, w, h, radius);
                if (d > 0f) return Color.clear;
                if (d > -frame)
                {
                    Color c = Wood(x, y, dark, light, 38f);
                    if (d > -7f) c = Color.Lerp(c, y > h * 0.5f ? Color.white : Color.black, y > h * 0.5f ? 0.22f : 0.30f); // bisel exterior
                    else if (d < -frame + 6f) c = Color.Lerp(c, Color.black, 0.35f);                                        // sombra interior
                    return c;
                }
                // Papel crema con una sombra suave junto al marco
                float shadow = Mathf.Clamp01((d + frame + 26f) / 26f);
                float n = Noise(x * 0.08f, y * 0.08f) * 0.05f;
                var paper = new Color(0.98f - n, 0.93f - n, 0.78f - n);
                return Color.Lerp(new Color(0.72f, 0.58f, 0.38f), paper, Mathf.SmoothStep(0.25f, 1f, 1f - (1f - shadow) * 0.9f));
            });
        }

        private static Texture2D MakeRibbon(int w, int h)
        {
            var dark = new Color(0.62f, 0.17f, 0.09f);
            var light = new Color(0.86f, 0.33f, 0.17f);
            return Render(w, h, (x, y) =>
            {
                float d = RoundRect(x, y, w, h, 40f);
                if (d > 0f) return Color.clear;
                Color c = Wood(x, y, dark, light, 54f);
                if (d > -8f) c = Color.Lerp(c, new Color(0.30f, 0.07f, 0.03f), 0.85f);       // borde oscuro
                else if (d > -16f && y > h * 0.5f) c = Color.Lerp(c, Color.white, 0.18f);   // brillo superior
                foreach (float nx in new[] { 34f, w - 34f })                                  // clavos
                {
                    float nd = Vector2.Distance(new Vector2(x, y), new Vector2(nx, h * 0.5f));
                    if (nd < 9f) c = Color.Lerp(new Color(0.78f, 0.78f, 0.80f), new Color(0.35f, 0.35f, 0.38f), Mathf.Clamp01((y - (h * 0.5f - 9f)) / 18f));
                }
                return c;
            });
        }

        private static Texture2D MakeButton(int w, int h)
        {
            return Render(w, h, (x, y) =>
            {
                float d = RoundRect(x, y, w, h, h * 0.5f);
                if (d > 0f) return Color.clear;
                if (d > -7f) return new Color(0.07f, 0.34f, 0.16f);                             // borde
                float k = y / h;
                Color c = Color.Lerp(new Color(0.14f, 0.58f, 0.27f), new Color(0.38f, 0.82f, 0.42f), k);
                if (d < -12f && k > 0.55f) c = Color.Lerp(c, Color.white, 0.22f * Mathf.InverseLerp(0.55f, 0.9f, k)); // brillo
                return c;
            });
        }

        private static Texture2D MakeStar(int size)
        {
            var outer = new Vector2[10];
            var inner = new Vector2[10];
            float cx = size * 0.5f, cy = size * 0.52f, rOut = size * 0.47f, rIn = size * 0.2f;
            for (int i = 0; i < 10; i++)
            {
                float angle = Mathf.PI / 2f + i * Mathf.PI / 5f;
                float radius = i % 2 == 0 ? rOut : rIn;
                outer[i] = new Vector2(cx + Mathf.Cos(angle) * radius, cy + Mathf.Sin(angle) * radius);
                inner[i] = new Vector2(cx + Mathf.Cos(angle) * radius * 0.8f, cy + Mathf.Sin(angle) * radius * 0.8f);
            }
            return Render(size, size, (x, y) =>
            {
                var p = new Vector2(x, y);
                if (!Inside(p, outer)) return Color.clear;
                if (!Inside(p, inner)) return new Color(0.62f, 0.34f, 0.04f);
                return Color.Lerp(new Color(1f, 0.62f, 0.10f), new Color(1f, 0.90f, 0.35f), y / size);
            });
        }

        private static bool Inside(Vector2 p, Vector2[] poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                    p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            return inside;
        }
    }
}
#endif
