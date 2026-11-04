#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VamosAprendiendo.EditorTools
{
    /// <summary>
    /// Ordena la jerarquía de BaseScene: los objetos sueltos en la raíz (árboles, pasto, rocas, animales, canvas...)
    /// se agrupan bajo los contenedores "--- [...] ---". Conserva la posición en el mundo de cada objeto.
    /// </summary>
    public static class HierarchyOrganizerTool
    {
        private const string BASE_SCENE_PATH = "Assets/_Project/Scenes/BaseScene.unity";

        private static readonly Regex Suffix = new Regex(@" \(\d+\)$");

        [MenuItem("Tools/Scenes/Organize Hierarchy (BaseScene)", false, 39)]
        public static void Organize()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.path.Contains("BaseScene")) scene = EditorSceneManager.OpenScene(BASE_SCENE_PATH, OpenSceneMode.Single);

            Transform env = Root("--- [ENVIRONMENT] ---");
            Transform fauna = Root("--- [FAUNA] ---");
            Transform ui = Root("--- [UI] ---");
            Transform nature = Child(env, "Nature");
            Transform props = Child(env, "Props_Decoration");
            Transform level = Child(env, "Level_Geometry");

            var trees = Child(nature, "Trees");
            var cut = Child(nature, "Stumps_Logs");
            var plants = Child(nature, "Shrubs_Flowers");
            var rocks = Child(nature, "Rocks");
            var grass = Child(nature, "Grass");
            var fences = Child(props, "Fences");
            var farm = Child(fauna, "Farm_Animals");

            int moved = 0;
            var leftovers = new List<string>();

            // 1) Raíz: todo lo que no sea un contenedor "--- ... ---".
            foreach (var go in scene.GetRootGameObjects())
            {
                string n = go.name;
                if (n.StartsWith("---")) continue;
                string b = Suffix.Replace(n, "");

                Transform target = null;
                if (b.EndsWith("Canvas")) target = ui;
                else if (b == "Water_Sea") target = env;
                else if (b == "PT_Wooden_Bridge_02") target = level;
                else if (b == "Chest" || b.StartsWith("Giant")) target = props;
                else if (b.StartsWith("PT_Modular_")) target = fences;
                else if (b == "ButterflySpawnArea") target = fauna;
                else if (Regex.IsMatch(b, "^(Chiken|Pig_pink1|LLama|Cat|Cow|Elephent)$")) target = farm;
                else if (b.EndsWith("_cut") || b.EndsWith("_stump") || b.EndsWith("_logs")) target = cut;
                else if (b.StartsWith("PT_Grass") || b.StartsWith("Grass_")) target = grass;
                else if (b.Contains("Rock")) target = rocks;
                else if (Regex.IsMatch(b, "Shrub|Bush|Flower|Poppy|Mushroom|Cattail|Waterlily")) target = plants;
                else if (Regex.IsMatch(b, "(?i)tree|^Fir_")) target = trees;

                if (target == null) { leftovers.Add(n); continue; }
                Move(go.transform, target);
                moved++;
            }

            // 2) Los árboles armados a mano que ya estaban en Nature van con el resto de árboles.
            var loose = new List<Transform>();
            foreach (Transform t in nature)
                if (t != trees && t != cut && t != plants && t != rocks && t != grass) loose.Add(t);
            foreach (var t in loose) { Move(t, trees); moved++; }

            // 3) Orden de los contenedores de primer nivel.
            string[] order = { "--- [CAMERAS & SYSTEMS] ---", "--- [LIGHTING & VOLUME] ---", "--- [GAMEPLAY] ---",
                               "--- [ENVIRONMENT] ---", "--- [FAUNA] ---", "--- [FX] ---", "--- [UI] ---" };
            for (int i = 0; i < order.Length; i++)
            {
                var g = GameObject.Find(order[i]);
                if (g != null) g.transform.SetSiblingIndex(i);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Hierarchy] Objetos movidos: " + moved + ". Sin clasificar: " +
                      (leftovers.Count == 0 ? "ninguno" : string.Join(", ", leftovers)));
        }

        private static Transform Root(string name)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, "Hierarchy group");
            }
            if (go.transform.position != Vector3.zero || go.transform.localScale != Vector3.one)
                Debug.LogWarning("[Hierarchy] El contenedor " + name + " no está en la identidad; se conserva la posición mundial al mover.");
            return go.transform;
        }

        private static Transform Child(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t != null) return t;
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Hierarchy group");
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static void Move(Transform t, Transform parent)
        {
            Undo.SetTransformParent(t, parent, "Organize Hierarchy");
        }
    }
}
#endif
