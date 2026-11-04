#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VamosAprendiendo.EditorTools
{
    /// <summary>
    /// Reparte pasto (PT_Grass_02) por las zonas libres de arena de Plataforma_uno (1): manchones irregulares,
    /// esquivando linternas, rocas, camino de piedras, cerca, arbustos y el pasto que ya existe.
    /// </summary>
    public static class GrassScatterTool
    {
        private const string BASE_SCENE_PATH = "Assets/_Project/Scenes/BaseScene.unity";
        private const string PREFAB = "Assets/_ThirdParty/Vegetation/Polytope Studio/Lowpoly_Environments/Prefabs/Plants/PT_Grass_02.prefab";
        private const string PLATFORM = "Plataforma_uno (1)";
        private const string CONTAINER = "Grass_Plataforma_uno_1";
        private const float EdgeInset = 2.0f;     // lejos del borde de pasto / precipicio
        private const float MinSpacing = 0.75f;   // entre matas
        private const float ObstaclePad = 0.55f;   // alrededor de objetos
        private const int MaxCount = 360;

        [MenuItem("Tools/Scenes/Scatter Grass On Plataforma_uno (1)", false, 36)]
        public static void Scatter()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.path.Contains("BaseScene")) scene = EditorSceneManager.OpenScene(BASE_SCENE_PATH, OpenSceneMode.Single);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB);
            var platform = GameObject.Find(PLATFORM);
            if (prefab == null || platform == null) { Debug.LogError("[GrassScatter] No se encontró el prefab PT_Grass_02 o " + PLATFORM); return; }

            var old = GameObject.Find(CONTAINER);
            if (old != null) Object.DestroyImmediate(old);

            var plat = platform.GetComponent<MeshRenderer>();
            Bounds pb = plat.bounds;
            float top = pb.max.y;
            Physics.SyncTransforms();

            // Obstáculos: cualquier cosa que se apoye sobre la plataforma (sin la plataforma, el borde ni el suelo grande).
            var obstacles = new List<Bounds>();
            var existingGrass = new List<Vector2>();
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (r.gameObject == platform || r.name.StartsWith("Island_Grass_Rim") || r.name.StartsWith("Island_Rock_Base")) continue;
                Bounds b = r.bounds;
                if (b.size.x > 12f || b.size.z > 12f) continue;
                if (b.min.y > top + 1.5f || b.max.y < top - 0.3f) continue;
                if (b.max.x < pb.min.x || b.min.x > pb.max.x || b.max.z < pb.min.z || b.min.z > pb.max.z) continue;
                if (r.name.StartsWith("PT_Grass_02")) { existingGrass.Add(new Vector2(b.center.x, b.center.z)); continue; }
                obstacles.Add(b);
            }

            var parent = new GameObject(CONTAINER);
            Undo.RegisterCreatedObjectUndo(parent, "Grass");
            var placed = new List<Vector2>(existingGrass);
            var rng = new System.Random(20261026);
            float seed = (float)rng.NextDouble() * 100f;
            int count = 0, tries = 0;

            while (count < MaxCount && tries < 30000)
            {
                tries++;
                float x = Mathf.Lerp(pb.min.x + EdgeInset, pb.max.x - EdgeInset, (float)rng.NextDouble());
                float z = Mathf.Lerp(pb.min.z + EdgeInset, pb.max.z - EdgeInset, (float)rng.NextDouble());

                // Manchones: la probabilidad sigue un ruido suave, así no queda uniforme.
                float noise = Mathf.PerlinNoise(seed + x * 0.18f, seed + z * 0.18f);
                if (noise < 0.35f && rng.NextDouble() > 0.25) continue;

                var p = new Vector2(x, z);
                bool blocked = false;
                foreach (var o in placed) if ((o - p).sqrMagnitude < MinSpacing * MinSpacing) { blocked = true; break; }
                if (blocked) continue;
                foreach (var o in obstacles)
                {
                    if (x > o.min.x - ObstaclePad && x < o.max.x + ObstaclePad && z > o.min.z - ObstaclePad && z < o.max.z + ObstaclePad) { blocked = true; break; }
                }
                if (blocked) continue;

                // Solo sobre la superficie superior de la plataforma (no sobre el borde ni el vacío).
                if (!Physics.Raycast(new Vector3(x, top + 3f, z), Vector3.down, out var hit, 6f)) continue;
                if (hit.collider.gameObject != platform || hit.normal.y < 0.9f) continue;

                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
                go.name = $"PT_Grass_02 ({count})";
                go.transform.position = hit.point;
                go.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                go.transform.localScale = Vector3.one * Mathf.Lerp(1.3f, 2.0f, (float)rng.NextDouble());
                placed.Add(p);
                count++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[GrassScatter] {count} matas de PT_Grass_02 en {PLATFORM} (obstáculos evitados: {obstacles.Count}, pasto previo: {existingGrass.Count}).");
        }
    }
}
#endif
