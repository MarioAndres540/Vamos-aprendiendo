#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VamosAprendiendo.Gameplay;

namespace VamosAprendiendo.EditorTools
{
    public static class PortalSetupTool
    {
        private const string BASE_SCENE_PATH = "Assets/_Project/Scenes/BaseScene.unity";
        private const string PORTALS_PATH = "--- [ENVIRONMENT] ---/Props_Decoration/Portals";

        [MenuItem("Tools/Scenes/Connect Portals (Yellow=Letters, Red=Numbers)", false, 22)]
        public static void ConnectPortals()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.path.Contains("BaseScene"))
            {
                scene = EditorSceneManager.OpenScene(BASE_SCENE_PATH, OpenSceneMode.Single);
            }

            Transform portals = FindByPath(PORTALS_PATH);
            if (portals == null)
            {
                Debug.LogError("[PortalSetup] No se encontró el grupo de portales en BaseScene.");
                return;
            }

            bool yellow = ConnectPortal(portals, "Portal yellow", "Portal_Letters_Trigger", "LetterTrace_2D");
            bool red = ConnectPortal(portals, "Portal red", "Portal_Numbers_Trigger", "NumberTrace_2D");

            var player = Object.FindFirstObjectByType<PlayerController>();
            if (player != null && player.GetComponent<PlayerSpawnRestorer>() == null)
            {
                Undo.AddComponent<PlayerSpawnRestorer>(player.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[PortalSetup] Amarillo→LetterTrace_2D: {yellow}. Rojo→NumberTrace_2D: {red}.");
        }

        private static bool ConnectPortal(Transform portals, string portalName, string triggerName, string sceneName)
        {
            Transform visual = portals.Find(portalName);
            if (visual == null)
            {
                Debug.LogError($"[PortalSetup] No se encontró '{portalName}'.");
                return false;
            }

            Transform old = portals.Find(triggerName);
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var go = new GameObject(triggerName);
            Undo.RegisterCreatedObjectUndo(go, "Create Portal Trigger");
            go.transform.SetParent(portals, false);
            go.transform.position = visual.position;

            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(3.5f, 5f, 3.5f); // generoso, sin depender de la orientación del portal

            var trigger = go.AddComponent<PortalTrigger>();
            var so = new SerializedObject(trigger);
            so.FindProperty("targetSceneName").stringValue = sceneName;
            so.ApplyModifiedProperties();
            return true;
        }

        private static Transform FindByPath(string path)
        {
            string[] parts = path.Split('/');
            Transform current = null;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name == parts[0]) { current = root.transform; break; }
            }
            for (int i = 1; current != null && i < parts.Length; i++)
            {
                current = current.Find(parts[i]);
            }
            return current;
        }
    }
}
#endif
