using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using VamosAprendiendo.UI;

namespace VamosAprendiendo.EditorTools
{
    /// <summary>
    /// Crea la escena del menú principal (Jugar, Instructivo, Créditos, Cerrar sesión), la agrega a la compilación justo después
    /// del login y hace que el login lleve a ella. Se puede ejecutar de nuevo: reconstruye la escena entera.
    /// </summary>
    public static class MainMenuSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/MainMenuScene.unity";
        private const string LoginPath = "Assets/_Project/Scenes/LoginScene.unity";

        private static readonly Color Purple = new Color32(0x6B, 0x57, 0xDD, 255);
        private static readonly Color Navy = new Color32(0x1E, 0x1B, 0x5C, 255);
        private static readonly Color Green = new Color32(0x22, 0xC5, 0x5E, 255);
        private static readonly Color Coral = new Color32(0xE5, 0x6B, 0x6F, 255);

        private static Sprite _round;

        private const string HowToText =
            "<b><color=#6B57DD>Objetivo</color></b>\n" +
            "Camina por las islas, acércate al pulpo y elige una actividad. Traza las letras y los números siguiendo el camino.\n" +
            "Cuando termines, abre el cofre para recibir tu experiencia.\n\n" +
            "<b><color=#6B57DD>Controles</color></b>\n" +
            "•  <b>Joystick</b> (a la izquierda): mover al personaje\n" +
            "•  <b>Deslizar el dedo</b> por la derecha: girar la cámara\n" +
            "•  <b>Saltar</b>: botón de salto\n" +
            "•  <b>Sonido</b>: activar o silenciar la música y los efectos\n" +
            "•  <b>Pausa</b>: detener el juego y volver cuando quieras\n" +
            "•  <b>?</b>: repetir el tutorial";

        private const string CreditsText =
            "<b><color=#6B57DD>Darkmeridional studio games</color></b>\n" +
            "Proyecto Productivo · SENA\n" +
            "Desarrollo y diseño: Mario Andrés Jaramillo Arias\n\n" +
            "<b><color=#6B57DD>Arte, animación y efectos de terceros</color></b>\n" +
            "Kevin Iglesias (Humanoid Giant) · Mixamo (animaciones) · Hovl Studio (Magic Effects) · SineVFX · " +
            "TriForge Assets · Selcuk Gerceker (Toon Environments) · IgniteCoders (Simple Water Shader) · " +
            "Reversed Interactive (The Portal Collection) · Polytope Studio · IL3DN · Whaleride · Zacxophone · Pixit Games\n\n" +
            "Hecho con Unity 6";

        [MenuItem("Tools/Scenes/Build Main Menu Scene")]
        public static void Build()
        {
            _round = Resources.Load<Sprite>("ui_round");
            var background = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/images/fondo.png");
            var mascot = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/images/avatar.png");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0xF3, 0xF2, 0xFB, 255);

            var eventGo = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasGo = new GameObject("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            var root = (RectTransform)canvasGo.transform;

            // Fondo: cubre toda la pantalla sin deformarse
            var bg = NewRect("Background", root);
            Stretch(bg);
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.sprite = background;
            bgImg.color = background != null ? Color.white : new Color32(0xE8, 0xE5, 0xFA, 255);
            bgImg.raycastTarget = false;
            if (background != null)
            {
                var fitter = bg.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = background.rect.width / background.rect.height;
            }

            // Mascota a la izquierda
            if (mascot != null)
            {
                var m = NewRect("Mascot", root);
                Place(m, Vector2.one * 0.5f, new Vector2(-520f, -20f), new Vector2(660f, 528f));
                var mi = m.gameObject.AddComponent<Image>();
                mi.sprite = mascot; mi.preserveAspect = true; mi.raycastTarget = false;
            }

            // Título y saludo
            var title = Text("Title", root, "Vamos <color=#6B57DD>Aprendiendo</color>", 86, Navy, TextAlignmentOptions.Center);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            Place(title.rectTransform, Vector2.one * 0.5f, new Vector2(330f, 345f), new Vector2(1100f, 140f));
            var subtitle = Text("Subtitle", root, "Aprender es crecer cada día", 44, new Color32(0x6B, 0x70, 0x99, 255), TextAlignmentOptions.Center);
            subtitle.fontStyle = FontStyles.Normal;
            Place(subtitle.rectTransform, Vector2.one * 0.5f, new Vector2(330f, 262f), new Vector2(1000f, 70f));
            var greeting = Text("Greeting", root, "¡Hola!", 54, Navy, TextAlignmentOptions.Center);
            Place(greeting.rectTransform, Vector2.one * 0.5f, new Vector2(330f, 170f), new Vector2(1000f, 80f));

            // Botones
            var play = MakeButton(root, "Btn_Play", "Jugar", Green, new Vector2(330f, 40f), new Vector2(560f, 104f), 52);
            var howTo = MakeButton(root, "Btn_HowTo", "Instructivo", Purple, new Vector2(330f, -86f), new Vector2(560f, 104f), 48);
            var credits = MakeButton(root, "Btn_Credits", "Créditos", Purple, new Vector2(330f, -212f), new Vector2(560f, 104f), 48);
            var logout = MakeButton(root, "Btn_Logout", "Cerrar sesión", Coral, new Vector2(330f, -338f), new Vector2(380f, 84f), 36);

            // Paneles
            var howToPanel = BuildPanel(root, "HowToPanel", "Cómo se juega", HowToText, 1500f, 900f, out var howToClose, out var replay);
            var creditsPanel = BuildPanel(root, "CreditsPanel", "Créditos", CreditsText, 1500f, 860f, out var creditsClose, out _);

            var menu = canvasGo.AddComponent<MainMenuUI>();
            var so = new SerializedObject(menu);
            so.FindProperty("playButton").objectReferenceValue = play;
            so.FindProperty("howToButton").objectReferenceValue = howTo;
            so.FindProperty("creditsButton").objectReferenceValue = credits;
            so.FindProperty("logoutButton").objectReferenceValue = logout;
            so.FindProperty("howToPanel").objectReferenceValue = howToPanel;
            so.FindProperty("howToCloseButton").objectReferenceValue = howToClose;
            so.FindProperty("replayTutorialButton").objectReferenceValue = replay;
            so.FindProperty("creditsPanel").objectReferenceValue = creditsPanel;
            so.FindProperty("creditsCloseButton").objectReferenceValue = creditsClose;
            so.FindProperty("greetingLabel").objectReferenceValue = greeting;
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, ScenePath);

            AddToBuildSettings();
            RedirectLogin();
            Debug.Log("[MainMenu] MainMenuScene creada; el login ahora lleva a ella.");
        }

        // La escena del menú va justo después del login en la compilación
        private static void AddToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            scenes.RemoveAll(s => s.path == ScenePath);
            int login = scenes.FindIndex(s => s.path == LoginPath);
            scenes.Insert(login >= 0 ? login + 1 : 0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void RedirectLogin()
        {
            var login = EditorSceneManager.OpenScene(LoginPath, OpenSceneMode.Single);
            var ui = Object.FindFirstObjectByType<LoginUI>(FindObjectsInactive.Include);
            if (ui == null) { Debug.LogError("[MainMenu] LoginScene no tiene LoginUI."); return; }
            var so = new SerializedObject(ui);
            so.FindProperty("targetSceneName").stringValue = "MainMenuScene";
            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(login);
            EditorSceneManager.SaveScene(login);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        // ---------- ayudas ----------

        private static GameObject BuildPanel(RectTransform root, string name, string title, string body, float width, float height,
            out Button closeButton, out Button extraButton)
        {
            var holder = NewRect(name, root);
            Stretch(holder);
            var shade = NewRect("Shade", holder);
            Stretch(shade);
            var shadeImg = shade.gameObject.AddComponent<Image>();
            shadeImg.color = new Color(0.07f, 0.05f, 0.2f, 0.78f);

            var card = NewRect("Card", holder);
            Place(card, Vector2.one * 0.5f, Vector2.zero, new Vector2(width, height));
            var cardImg = card.gameObject.AddComponent<Image>();
            cardImg.sprite = _round; cardImg.type = Image.Type.Sliced; cardImg.color = Color.white;

            var header = NewRect("Header", card);
            header.anchorMin = new Vector2(0f, 1f); header.anchorMax = Vector2.one;
            header.offsetMin = new Vector2(0f, -120f); header.offsetMax = Vector2.zero;
            var headerImg = header.gameObject.AddComponent<Image>();
            headerImg.sprite = _round; headerImg.type = Image.Type.Sliced; headerImg.color = Purple;
            var titleText = Text("Title", header, title, 62, Color.white, TextAlignmentOptions.Center);
            Stretch(titleText.rectTransform);

            var bodyText = Text("Body", card, body, 36, Navy, TextAlignmentOptions.TopLeft);
            bodyText.fontStyle = FontStyles.Normal;
            bodyText.textWrappingMode = TextWrappingModes.Normal;
            bodyText.enableAutoSizing = true;
            bodyText.fontSizeMin = 22f; bodyText.fontSizeMax = 38f;
            bodyText.lineSpacing = 8f;
            var br = bodyText.rectTransform;
            br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one;
            br.offsetMin = new Vector2(70f, 150f); br.offsetMax = new Vector2(-70f, -150f);

            closeButton = MakeButton(card, "Btn_Close", "Cerrar", Purple, Vector2.zero, new Vector2(320f, 90f), 40);
            var closeRt = (RectTransform)closeButton.transform;
            closeRt.anchorMin = closeRt.anchorMax = new Vector2(0.5f, 0f);
            closeRt.anchoredPosition = new Vector2(name == "HowToPanel" ? 250f : 0f, 40f + 45f);

            extraButton = null;
            if (name == "HowToPanel")
            {
                extraButton = MakeButton(card, "Btn_ReplayTutorial", "Ver el tutorial", Green, Vector2.zero, new Vector2(460f, 90f), 38);
                var rt = (RectTransform)extraButton.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(-230f, 40f + 45f);
            }
            return holder.gameObject;
        }

        private static Button MakeButton(Transform parent, string name, string label, Color fill, Vector2 pos, Vector2 size, float fontSize)
        {
            var rt = NewRect(name, parent);
            Place(rt, Vector2.one * 0.5f, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = _round; img.type = Image.Type.Sliced; img.color = fill;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            btn.colors = colors;
            var t = Text("Label", rt, label, fontSize, Color.white, TextAlignmentOptions.Center);
            Stretch(t.rectTransform);
            return btn;
        }

        private static TextMeshProUGUI Text(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align)
        {
            var rt = NewRect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
            t.fontStyle = FontStyles.Bold;
            t.raycastTarget = false;
            return t;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
