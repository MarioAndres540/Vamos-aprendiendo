#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VamosAprendiendo.UI;

namespace VamosAprendiendo.EditorTools
{
    public static class MobileSetupTool
    {
        private const string BASE_SCENE_PATH = "Assets/_Project/Scenes/BaseScene.unity";
        private const string CHALLENGES_DIR = "Assets/_Project/Scenes/Challenges";
        private const string BUNDLE_ID = "com.darkmeridional.vamosaprendiendo";

        [MenuItem("Tools/Mobile/Apply Mobile Player Settings", false, 1)]
        public static void ApplyMobilePlayerSettings()
        {
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, BUNDLE_ID);
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, BUNDLE_ID);

            // Horizontal: el mundo 3D y los retos se diseñan para tablet/móvil en landscape.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            AddChallengeScenesToBuild();
            AssetDatabase.SaveAssets();
            Debug.Log($"[MobileSetup] Identificador '{BUNDLE_ID}', orientación landscape, IL2CPP + ARM64 y escenas de retos añadidas a Build Settings.");
        }

        private static void AddChallengeScenesToBuild()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!Directory.Exists(CHALLENGES_DIR)) return;

            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { CHALLENGES_DIR }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!scenes.Exists(s => s.path == path))
                {
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                }
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        [MenuItem("Tools/Mobile/Add Touch Controls to BaseScene", false, 2)]
        public static void AddTouchControlsToBaseScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.path.Contains("BaseScene"))
            {
                scene = EditorSceneManager.OpenScene(BASE_SCENE_PATH, OpenSceneMode.Single);
            }

            foreach (var sceneRoot in scene.GetRootGameObjects())
            {
                if (sceneRoot.name == "MobileControlsCanvas") Object.DestroyImmediate(sceneRoot);
            }

            Sprite circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            var canvasObj = new GameObject("MobileControlsCanvas");
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Mobile Controls");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50; // por debajo del menú de pausa (100)
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f; // alto fijo: los controles miden lo mismo en 16:9 y 20:9
            canvasObj.AddComponent<GraphicRaycaster>();

            var root = NewRect("Controls_Root", canvasObj.transform);
            Stretch(root);

            // Zona para girar la cámara (mitad derecha). Va primero para quedar por debajo del botón de salto.
            var look = NewRect("Look_Area", root);
            look.anchorMin = new Vector2(0.5f, 0f);
            look.anchorMax = new Vector2(1f, 1f);
            look.offsetMin = look.offsetMax = Vector2.zero;
            var lookImg = look.gameObject.AddComponent<Image>();
            lookImg.color = new Color(1f, 1f, 1f, 0f); // invisible, solo recibe toques
            look.gameObject.AddComponent<TouchLookArea>();

            // Zona del joystick flotante (mitad izquierda, sin la franja superior donde está el botón de pausa).
            var joyArea = NewRect("Joystick_Area", root);
            joyArea.anchorMin = new Vector2(0f, 0f);
            joyArea.anchorMax = new Vector2(0.5f, 0.7f);
            joyArea.pivot = new Vector2(0.5f, 0.5f);
            joyArea.offsetMin = joyArea.offsetMax = Vector2.zero;
            var joyAreaImg = joyArea.gameObject.AddComponent<Image>();
            joyAreaImg.color = new Color(1f, 1f, 1f, 0f);

            // Mando: se ancla al centro de la zona para que su posición local coincida con la del toque.
            var joyBg = NewRect("Joystick_Background", joyArea);
            joyBg.anchorMin = joyBg.anchorMax = joyBg.pivot = new Vector2(0.5f, 0.5f);
            joyBg.sizeDelta = new Vector2(260f, 260f);
            var joyBgImg = joyBg.gameObject.AddComponent<Image>();
            joyBgImg.sprite = circle;
            joyBgImg.color = new Color(1f, 1f, 1f, 0.3f);
            joyBgImg.raycastTarget = false;

            var handle = NewRect("Joystick_Handle", joyBg);
            handle.sizeDelta = new Vector2(110f, 110f);
            var handleImg = handle.gameObject.AddComponent<Image>();
            handleImg.sprite = circle;
            handleImg.color = new Color(1f, 1f, 1f, 0.8f);
            handleImg.raycastTarget = false;

            var joystick = joyArea.gameObject.AddComponent<VirtualJoystick>();
            var joySo = new SerializedObject(joystick);
            joySo.FindProperty("touchArea").objectReferenceValue = joyArea;
            joySo.FindProperty("background").objectReferenceValue = joyBg;
            joySo.FindProperty("handle").objectReferenceValue = handle;
            joySo.ApplyModifiedProperties();

            // Botón de salto (abajo-derecha)
            var jump = NewRect("Btn_Jump", root);
            jump.anchorMin = jump.anchorMax = jump.pivot = new Vector2(1f, 0f);
            jump.anchoredPosition = new Vector2(-140f, 140f);
            jump.sizeDelta = new Vector2(200f, 200f);
            var jumpImg = jump.gameObject.AddComponent<Image>();
            jumpImg.sprite = circle;
            jumpImg.color = new Color(0.31f, 0.35f, 0.9f, 0.7f);
            var jumpBtn = jump.gameObject.AddComponent<Button>();
            jumpBtn.targetGraphic = jumpImg;

            var labelRect = NewRect("Label", jump);
            Stretch(labelRect);
            var label = labelRect.gameObject.AddComponent<TMPro.TextMeshProUGUI>();
            label.text = "SALTAR";
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.fontSize = 34;
            label.fontStyle = TMPro.FontStyles.Bold;
            label.color = Color.white;
            label.raycastTarget = false;

            var ui = canvasObj.AddComponent<MobileControlsUI>();
            var uiSo = new SerializedObject(ui);
            uiSo.FindProperty("controlsRoot").objectReferenceValue = root.gameObject;
            uiSo.FindProperty("joystick").objectReferenceValue = joystick;
            uiSo.FindProperty("jumpButton").objectReferenceValue = jumpBtn;
            uiSo.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[MobileSetup] MobileControlsCanvas añadido a BaseScene.");
        }

        [MenuItem("Tools/Mobile/Add Pause Button to BaseScene", false, 3)]
        public static void AddPauseButtonToBaseScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.path.Contains("BaseScene"))
            {
                scene = EditorSceneManager.OpenScene(BASE_SCENE_PATH, OpenSceneMode.Single);
            }

            var pauseSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/images/btn_pause.png");
            var playSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/images/btn_play.png");
            if (pauseSprite == null || playSprite == null)
            {
                Debug.LogError("[MobileSetup] Faltan btn_pause.png / btn_play.png. Ejecuta Tools/Art/Generate Pause Button.");
                return;
            }

            foreach (var sceneRoot in scene.GetRootGameObjects())
            {
                if (sceneRoot.name == "PauseButtonCanvas") Object.DestroyImmediate(sceneRoot);
            }

            var canvasObj = new GameObject("PauseButtonCanvas");
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Pause Button");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150; // sobre el menú de pausa (100) para poder reanudar con el mismo botón
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;
            canvasObj.AddComponent<GraphicRaycaster>();

            var buttonRt = NewRect("Btn_Pause", canvasObj.transform);
            buttonRt.anchorMin = buttonRt.anchorMax = buttonRt.pivot = new Vector2(0f, 1f);
            buttonRt.anchoredPosition = new Vector2(40f, -40f);
            buttonRt.sizeDelta = new Vector2(130f, 130f);

            var img = buttonRt.gameObject.AddComponent<Image>();
            img.sprite = pauseSprite;
            img.preserveAspect = true;
            var btn = buttonRt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;

            var ui = canvasObj.AddComponent<PauseButtonUI>();
            var so = new SerializedObject(ui);
            so.FindProperty("button").objectReferenceValue = btn;
            so.FindProperty("icon").objectReferenceValue = img;
            so.FindProperty("pauseSprite").objectReferenceValue = pauseSprite;
            so.FindProperty("playSprite").objectReferenceValue = playSprite;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[MobileSetup] PauseButtonCanvas añadido a BaseScene.");
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
#endif
