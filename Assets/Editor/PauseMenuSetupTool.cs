#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using VamosAprendiendo.UI;

namespace VamosAprendiendo.EditorTools
{
    public static class PauseMenuSetupTool
    {
        private const string BASE_SCENE_PATH = "Assets/_Project/Scenes/BaseScene.unity";
        private const string PAUSE_IMAGE_PATH = "Assets/_Project/Art/images/menu_pause.png";
        private const string PREFAB_PATH = "Assets/_Project/Prefabs/UI/PauseMenuCanvas.prefab";

        [MenuItem("Tools/UI/Rebuild Pause Menu (Only Art Image)", false, 1)]
        public static void SetupPauseMenu()
        {
            Debug.Log("<color=#00e6e6><b>[PauseMenuSetup] Reconstruyendo menú de pausa puro con la imagen menu_pause...</b></color>");

            // 1. Cargar sprite
            Sprite pauseSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PAUSE_IMAGE_PATH);
            if (pauseSprite == null)
            {
                pauseSprite = Resources.Load<Sprite>("menu_pause");
            }

            // 2. Abrir BaseScene
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.isLoaded || !activeScene.path.Contains("BaseScene"))
            {
                if (File.Exists(BASE_SCENE_PATH))
                {
                    activeScene = EditorSceneManager.OpenScene(BASE_SCENE_PATH, OpenSceneMode.Single);
                }
            }

            EnsureEventSystem();

            // 3. Limpiar TODOS los objetos viejos del menú de pausa
            string[] obsoleteNames = new string[] {
                "PauseMenuCanvas", "PauseMenu_Root", "Center_Modal", "Dim_Overlay",
                "Title_Text", "Button_Resume", "Button_Restart", "Button_MainMenu",
                "Button_Quit", "MenuPause_Image", "MenuPause_Dialog", "Button_Reanudar", "Button_Salir"
            };

            foreach (var root in activeScene.GetRootGameObjects())
            {
                CleanObsoleteObjectsRecursive(root.transform, obsoleteNames);
            }

            // 4. Crear Canvas nuevo y limpio
            GameObject canvasObj = new GameObject("PauseMenuCanvas");
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Pause Menu Canvas");

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            CanvasGroup cg = canvasObj.AddComponent<CanvasGroup>();
            PauseMenuUI pauseUI = canvasObj.AddComponent<PauseMenuUI>();

            // 5. Root de pantalla completa
            GameObject rootObj = CreateUIElement("PauseMenu_Root", canvasObj.transform);
            SetFullStretch(rootObj.GetComponent<RectTransform>());

            // 6. Fondo oscuro (Dim Overlay)
            GameObject dimObj = CreateUIElement("Dim_Overlay", rootObj.transform);
            SetFullStretch(dimObj.GetComponent<RectTransform>());
            Image dimImg = dimObj.AddComponent<Image>();
            dimImg.color = new Color(0.02f, 0.04f, 0.08f, 0.70f);
            dimImg.raycastTarget = true;

            // 7. Diálogo de la Imagen (Grande y Claro: 720x720)
            float dialogW = 720f;
            float dialogH = 720f;

            GameObject dialogObj = CreateUIElement("MenuPause_Dialog", rootObj.transform);
            RectTransform dialogRect = dialogObj.GetComponent<RectTransform>();
            dialogRect.sizeDelta = new Vector2(dialogW, dialogH);
            dialogRect.anchoredPosition = Vector2.zero;

            Image dialogImg = dialogObj.AddComponent<Image>();
            if (pauseSprite != null)
            {
                dialogImg.sprite = pauseSprite;
                dialogImg.preserveAspect = true;
            }
            dialogImg.raycastTarget = false;

            // 8. Hitbox Interactiva sobre 'Reanudar' (Botón Verde)
            GameObject resumeObj = CreateButtonHitbox("Button_Reanudar", dialogObj.transform, new Vector2(0f, -dialogH * 0.13f), new Vector2(dialogW * 0.52f, dialogH * 0.15f));
            Button resumeBtn = resumeObj.GetComponent<Button>();

            // 9. Hitbox Interactiva sobre 'Salir' (Botón Rojo)
            GameObject exitObj = CreateButtonHitbox("Button_Salir", dialogObj.transform, new Vector2(0f, -dialogH * 0.31f), new Vector2(dialogW * 0.52f, dialogH * 0.15f));
            Button exitBtn = exitObj.GetComponent<Button>();

            // 10. Conectar campos serializados en PauseMenuUI
            SerializedObject serializedUI = new SerializedObject(pauseUI);
            serializedUI.FindProperty("pauseMenuRoot").objectReferenceValue = rootObj;
            serializedUI.FindProperty("dimBackgroundOverlay").objectReferenceValue = dimImg;
            serializedUI.FindProperty("menuPanel").objectReferenceValue = dialogRect;
            serializedUI.FindProperty("menuPauseImage").objectReferenceValue = dialogImg;
            serializedUI.FindProperty("resumeButton").objectReferenceValue = resumeBtn;
            serializedUI.FindProperty("mainMenuButton").objectReferenceValue = exitBtn;
            serializedUI.ApplyModifiedProperties();

            // Guardar Prefab
            if (!Directory.Exists("Assets/_Project/Prefabs/UI"))
            {
                Directory.CreateDirectory("Assets/_Project/Prefabs/UI");
            }
            PrefabUtility.SaveAsPrefabAsset(canvasObj, PREFAB_PATH);

            // Guardar Escena
            if (activeScene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
            }

            Debug.Log("<color=#00ff88><b>[PauseMenuSetup] ¡Menú de pausa reconstruido con la imagen grande (720x720) y sin cajas viejas!</b></color>");
        }

        private static void CleanObsoleteObjectsRecursive(Transform current, string[] names)
        {
            if (current == null) return;

            for (int i = current.childCount - 1; i >= 0; i--)
            {
                CleanObsoleteObjectsRecursive(current.GetChild(i), names);
            }

            foreach (string n in names)
            {
                if (current.name == n)
                {
                    Undo.DestroyObjectImmediate(current.gameObject);
                    break;
                }
            }
        }

        private static GameObject CreateUIElement(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void SetFullStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static GameObject CreateButtonHitbox(string name, Transform parent, Vector2 localPos, Vector2 size)
        {
            GameObject btnObj = CreateUIElement(name, parent);
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = localPos;

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.18f);
            colors.pressedColor = new Color(0f, 0f, 0f, 0.22f);
            colors.selectedColor = new Color(1f, 1f, 1f, 0.10f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;

            return btnObj;
        }

        private static void EnsureEventSystem()
        {
            UnityEngine.EventSystems.EventSystem existing = UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (existing == null)
            {
                GameObject eventSysObj = new GameObject("EventSystem");
                Undo.RegisterCreatedObjectUndo(eventSysObj, "Create EventSystem");
                eventSysObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                var inputModule = eventSysObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                var actionsAsset = Resources.Load<UnityEngine.InputSystem.InputActionAsset>("InputSystem_Actions");
                if (actionsAsset != null)
                {
                    inputModule.actionsAsset = actionsAsset;
                }
                inputModule.AssignDefaultActions();
            }
        }
    }
}
#endif
