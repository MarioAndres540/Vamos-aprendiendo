#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VamosAprendiendo.Gameplay;

namespace VamosAprendiendo.EditorTools
{
    /// <summary>
    /// Anima el cofre (se abre al tocarlo) y el portón de madera (se abre al acercarse el jugador) de BaseScene con
    /// HingeOpener; los modelos no traen animaciones, así que se giran sus piezas por la bisagra.
    /// </summary>
    public static class OpenablesSetupTool
    {
        private const string BASE_SCENE_PATH = "Assets/_Project/Scenes/BaseScene.unity";

        [MenuItem("Tools/Scenes/Setup Chest and Gate Animations (BaseScene)", false, 40)]
        public static void Setup()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.path.Contains("BaseScene")) scene = EditorSceneManager.OpenScene(BASE_SCENE_PATH, OpenSceneMode.Single);

            SetupChest(Find(scene, "Chest"));
            SetupGate(Find(scene, "PT_Modular_Gate_Wood_01"));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        [MenuItem("Tools/Scenes/Setup Octopus Tap -> Activity Select (BaseScene)", false, 41)]
        public static void SetupOctopus()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.path.Contains("BaseScene")) scene = EditorSceneManager.OpenScene(BASE_SCENE_PATH, OpenSceneMode.Single);

            var octopus = Find(scene, "Octopus_Rigged_Unity");
            if (octopus == null) return;

            // Collider que envuelve al pulpo (la malla animada no tiene) para poder tocarlo.
            var box = octopus.GetComponent<BoxCollider>();
            if (box == null) box = Undo.AddComponent<BoxCollider>(octopus.gameObject);
            Bounds b = new Bounds(octopus.position, Vector3.zero);
            foreach (var r in octopus.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
            box.center = octopus.InverseTransformPoint(b.center);
            Vector3 ls = octopus.lossyScale;
            box.size = new Vector3(b.size.x / Mathf.Abs(ls.x), b.size.y / Mathf.Abs(ls.y), b.size.z / Mathf.Abs(ls.z));

            var tap = octopus.GetComponent<TapToLoadScene>();
            if (tap == null) tap = Undo.AddComponent<TapToLoadScene>(octopus.gameObject);
            var so = new SerializedObject(tap);
            so.FindProperty("sceneName").stringValue = "ActivitySelect";
            so.FindProperty("message").stringValue = "Cargando tu progreso...";
            so.FindProperty("maxDistance").floatValue = 14f;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Openables] Pulpo listo: al tocarlo abre ActivitySelect.");
        }

        private static Transform Find(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name) return t;
            Debug.LogError("[Openables] No se encontró " + name);
            return null;
        }

        private static void SetupChest(Transform chest)
        {
            if (chest == null) return;
            var lid = chest.Find("Chest_cover");
            if (lid == null) { Debug.LogError("[Openables] El cofre no tiene Chest_cover."); return; }

            // Collider que cubre base y tapa cerrada, para poder tocarlo.
            var box = chest.GetComponent<BoxCollider>();
            if (box == null) box = Undo.AddComponent<BoxCollider>(chest.gameObject);
            Bounds b = new Bounds(chest.position, Vector3.zero);
            foreach (var r in chest.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
            box.center = chest.InverseTransformPoint(b.center);
            Vector3 ls = chest.lossyScale;
            box.size = new Vector3(b.size.x / Mathf.Abs(ls.x), b.size.y / Mathf.Abs(ls.y), b.size.z / Mathf.Abs(ls.z));

            // Giro de la tapa sobre X: negativo levanta el frente (la bisagra está atrás).
            var opener = Configure(chest.gameObject, 0.6f, new[] { (lid, new Vector3(-100f, 0f, 0f)) });
            var tap = chest.GetComponent<TapToOpen>();
            if (tap == null) tap = Undo.AddComponent<TapToOpen>(chest.gameObject);
            var so = new SerializedObject(tap);
            so.FindProperty("opener").objectReferenceValue = opener;
            so.ApplyModifiedProperties();
            Debug.Log("[Openables] Cofre listo: se abre al tocarlo.");
        }

        private static void SetupGate(Transform gate)
        {
            if (gate == null) return;
            var left = gate.Find("PT_Modular_Gate_Wood_01_left");
            var right = gate.Find("PT_Modular_Gate_Wood_01_right");
            if (left == null || right == null) { Debug.LogError("[Openables] El portón no tiene sus dos hojas."); return; }

            // Cada hoja tiene la bisagra en su extremo exterior; giran en sentidos opuestos para abrir juntas hacia el mismo lado.
            var opener = Configure(gate.gameObject, 0.8f, new[] { (left, new Vector3(0f, 90f, 0f)), (right, new Vector3(0f, -90f, 0f)) });
            var prox = gate.GetComponent<ProximityOpener>();
            if (prox == null) prox = Undo.AddComponent<ProximityOpener>(gate.gameObject);
            var so = new SerializedObject(prox);
            so.FindProperty("opener").objectReferenceValue = opener;
            so.ApplyModifiedProperties();
            Debug.Log("[Openables] Portón listo: se abre al acercarse.");
        }

        private static HingeOpener Configure(GameObject go, float duration, (Transform part, Vector3 euler)[] parts)
        {
            var opener = go.GetComponent<HingeOpener>();
            if (opener == null) opener = Undo.AddComponent<HingeOpener>(go);
            var so = new SerializedObject(opener);
            so.FindProperty("duration").floatValue = duration;
            var arr = so.FindProperty("hinges");
            arr.arraySize = parts.Length;
            for (int i = 0; i < parts.Length; i++)
            {
                var el = arr.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("part").objectReferenceValue = parts[i].part;
                el.FindPropertyRelative("openEuler").vector3Value = parts[i].euler;
            }
            so.ApplyModifiedProperties();
            return opener;
        }
    }
}
#endif
