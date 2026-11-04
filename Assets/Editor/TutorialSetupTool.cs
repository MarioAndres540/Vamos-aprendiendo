using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using VamosAprendiendo.UI;

namespace VamosAprendiendo.EditorTools
{
    /// <summary>Agrega el tutorial de controles a BaseScene y conecta los botones que explica.</summary>
    public static class TutorialSetupTool
    {
        private const string BaseScenePath = "Assets/_Project/Scenes/BaseScene.unity";

        [MenuItem("Tools/UI/Add Tutorial to BaseScene")]
        public static void AddTutorial()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.path.Contains("BaseScene")) scene = EditorSceneManager.OpenScene(BaseScenePath, OpenSceneMode.Single);

            foreach (var old in Object.FindObjectsByType<TutorialManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(old.gameObject);

            var go = new GameObject("TutorialManager");
            Undo.RegisterCreatedObjectUndo(go, "Create Tutorial");
            var group = GameObject.Find("--- [UI] ---");
            if (group != null) go.transform.SetParent(group.transform, false);
            var tutorial = go.AddComponent<TutorialManager>();

            var so = new SerializedObject(tutorial);
            so.FindProperty("joystick").objectReferenceValue = Object.FindFirstObjectByType<VirtualJoystick>(FindObjectsInactive.Include);
            so.FindProperty("joystickBackground").objectReferenceValue = Find<RectTransform>("Joystick_Background");
            so.FindProperty("cameraHint").objectReferenceValue = Find<RectTransform>("Cam_Hint");
            so.FindProperty("jumpButton").objectReferenceValue = Find<Button>("Btn_Jump");
            so.FindProperty("audioButton").objectReferenceValue = Find<Button>("Btn_Audio");
            so.FindProperty("pauseButton").objectReferenceValue = Find<Button>("Btn_Pause");
            so.FindProperty("roundSprite").objectReferenceValue = Resources.Load<Sprite>("ui_round");
            so.FindProperty("circleSprite").objectReferenceValue = Resources.Load<Sprite>("ui_circle");
            so.FindProperty("arrowSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/images/ui_trace/arrow.png");
            so.FindProperty("mascotSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/images/avatar.png");
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Tutorial] TutorialManager añadido a BaseScene.");
            AddHelpButton();
        }

        /// <summary>Botón "?" de ayuda arriba a la derecha, debajo del de pausa (130 px + 24 px de separación entre botones).</summary>
        [MenuItem("Tools/UI/Add Help Button to BaseScene")]
        public static void AddHelpButton()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.path.Contains("BaseScene")) scene = EditorSceneManager.OpenScene(BaseScenePath, OpenSceneMode.Single);

            foreach (var old in Object.FindObjectsByType<HelpButtonUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(old.gameObject);

            var circle = Resources.Load<Sprite>("ui_circle");
            var canvasObj = new GameObject("HelpButtonCanvas");
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Help Button");
            var group = GameObject.Find("--- [UI] ---");
            if (group != null) canvasObj.transform.SetParent(group.transform, false);
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;   // como el audio y la pausa
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // Mismo estilo que los botones de audio y pausa: círculo violeta con aro claro
            var button = NewRect("Btn_Help", canvasObj.transform);
            button.anchorMin = button.anchorMax = button.pivot = new Vector2(1f, 1f);
            button.anchoredPosition = new Vector2(-40f, -348f);
            button.sizeDelta = new Vector2(130f, 130f);
            var ring = button.gameObject.AddComponent<Image>();
            ring.sprite = circle; ring.color = new Color32(0xE6, 0xE1, 0xFF, 255);
            var fill = NewRect("Fill", button);
            fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one;
            fill.offsetMin = new Vector2(8f, 8f); fill.offsetMax = new Vector2(-8f, -8f);
            var fillImg = fill.gameObject.AddComponent<Image>();
            fillImg.sprite = circle; fillImg.color = new Color32(0x5B, 0x4F, 0xD6, 255); fillImg.raycastTarget = false;
            var label = NewRect("Label", button);
            label.anchorMin = Vector2.zero; label.anchorMax = Vector2.one;
            label.offsetMin = label.offsetMax = Vector2.zero;
            var text = label.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = "?"; text.fontSize = 84; text.fontStyle = FontStyles.Bold;
            text.color = Color.white; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
            var btn = button.gameObject.AddComponent<Button>();
            btn.targetGraphic = ring;
            btn.transition = Selectable.Transition.None;

            var ui = canvasObj.AddComponent<HelpButtonUI>();
            var so = new SerializedObject(ui);
            so.FindProperty("button").objectReferenceValue = btn;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Tutorial] HelpButtonCanvas añadido a BaseScene.");
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        [MenuItem("Tools/UI/Reset Tutorial (this device, current account)")]
        public static void ResetTutorial()
        {
            TutorialManager.ResetDone();
            Debug.Log("[Tutorial] El tutorial volverá a mostrarse para la cuenta actual.");
        }

        private static T Find<T>(string name) where T : Component
        {
            foreach (var c in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (c.name == name) return c;
            Debug.LogWarning($"[Tutorial] No se encontró {name}; ese paso se omitirá.");
            return null;
        }
    }
}
