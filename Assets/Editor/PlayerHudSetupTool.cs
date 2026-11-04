using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VamosAprendiendo.UI;

namespace VamosAprendiendo.EditorTools
{
    /// <summary>Crea en BaseScene el HUD de la esquina superior izquierda: nombre del jugador y barra de experiencia.</summary>
    public static class PlayerHudSetupTool
    {
        private const string BaseScenePath = "Assets/_Project/Scenes/BaseScene.unity";
        private const string CanvasName = "PlayerHudCanvas";

        [MenuItem("Tools/UI/Add Player HUD to BaseScene")]
        public static void AddPlayerHud()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.path.Contains("BaseScene")) scene = EditorSceneManager.OpenScene(BaseScenePath, OpenSceneMode.Single);

            foreach (var old in Object.FindObjectsByType<PlayerHudUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(old.gameObject);

            var round = Resources.Load<Sprite>("ui_round");
            if (round == null) { Debug.LogError("[PlayerHud] Falta Resources/ui_round."); return; }

            var canvasObj = new GameObject(CanvasName);
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Player HUD");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90; // bajo el menú de pausa (100)
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;

            // Panel de madera oscura en la esquina; no recibe toques para no tapar el arrastre de cámara
            var panel = NewRect("Hud_Panel", canvasObj.transform);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0f, 1f);
            panel.anchoredPosition = new Vector2(40f, -40f);
            panel.sizeDelta = new Vector2(520f, 150f);
            var panelImg = panel.gameObject.AddComponent<Image>();
            panelImg.sprite = round;
            panelImg.type = Image.Type.Sliced;
            panelImg.color = new Color(0.16f, 0.1f, 0.06f, 0.82f);
            panelImg.raycastTarget = false;

            var nameLabel = NewText("Name", panel, 52f, FontStyles.Bold, new Color(1f, 0.93f, 0.75f), TextAlignmentOptions.MidlineLeft);
            Place(nameLabel.rectTransform, new Vector2(30f, -14f), new Vector2(-30f, -74f));
            nameLabel.text = "Jugador";

            var barBack = NewRect("Xp_Back", panel);
            Place(barBack, new Vector2(30f, 20f), new Vector2(-30f, 70f), fromBottom: true);
            var backImg = barBack.gameObject.AddComponent<Image>();
            backImg.sprite = round;
            backImg.type = Image.Type.Sliced;
            backImg.color = new Color(0f, 0f, 0f, 0.55f);
            backImg.raycastTarget = false;

            var fillRt = NewRect("Xp_Fill", barBack);
            Place(fillRt, new Vector2(4f, 4f), new Vector2(-4f, -4f), stretch: true);
            var fillImg = fillRt.gameObject.AddComponent<Image>();
            fillImg.sprite = Resources.Load<Sprite>("ui_circle") ?? round;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = 0;
            fillImg.fillAmount = 0.3f;
            fillImg.color = new Color(1f, 0.78f, 0.2f);
            fillImg.raycastTarget = false;

            var xpLabel = NewText("Xp_Label", barBack, 34f, FontStyles.Bold, Color.white, TextAlignmentOptions.Midline);
            Place(xpLabel.rectTransform, Vector2.zero, Vector2.zero, stretch: true);
            xpLabel.text = "0 XP";
            xpLabel.outlineWidth = 0.25f;
            xpLabel.outlineColor = new Color32(60, 30, 0, 255);

            var hud = canvasObj.AddComponent<PlayerHudUI>();
            var so = new SerializedObject(hud);
            so.FindProperty("nameLabel").objectReferenceValue = nameLabel;
            so.FindProperty("fill").objectReferenceValue = fillImg;
            so.FindProperty("xpLabel").objectReferenceValue = xpLabel;
            so.ApplyModifiedProperties();

            var uiGroup = GameObject.Find("--- [UI] ---");
            if (uiGroup != null) canvasObj.transform.SetParent(uiGroup.transform, false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[PlayerHud] PlayerHudCanvas añadido a BaseScene.");
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static TextMeshProUGUI NewText(string name, Transform parent, float size, FontStyles style, Color color, TextAlignmentOptions align)
        {
            var rt = NewRect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.alignment = align;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.raycastTarget = false;
            return t;
        }

        // Rectángulo que ocupa el ancho del padre con márgenes; offsetMin/offsetMax en los extremos indicados
        private static void Place(RectTransform rt, Vector2 min, Vector2 max, bool fromBottom = false, bool stretch = false)
        {
            if (stretch) rt.anchorMin = Vector2.zero; else rt.anchorMin = new Vector2(0f, fromBottom ? 0f : 1f);
            rt.anchorMax = stretch ? Vector2.one : new Vector2(1f, fromBottom ? 0f : 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            if (stretch) { rt.offsetMin = min; rt.offsetMax = max; return; }
            // Margen izquierdo/derecho en X; en Y se interpreta como (inferior, superior) respecto al ancla
            if (fromBottom) { rt.offsetMin = new Vector2(min.x, min.y); rt.offsetMax = new Vector2(max.x, max.y); }
            else { rt.offsetMin = new Vector2(min.x, max.y); rt.offsetMax = new Vector2(max.x, min.y); }
        }
    }
}
