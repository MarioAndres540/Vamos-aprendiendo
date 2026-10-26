#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VamosAprendiendo.EditorTools
{
    /// <summary>
    /// Iguala la densidad de textura de las islas, rocas y portales a una medida común (metros por repetición).
    /// Mide cuántos metros reales cubre hoy una repetición de la textura de cada objeto y reescala sus UV (en una malla
    /// propia, nunca en la del paquete) hasta llegar al objetivo. Se puede ejecutar de nuevo con otro objetivo.
    /// </summary>
    public static class TextureDensityTool
    {
        private const string BASE_SCENE_PATH = "Assets/_Project/Scenes/BaseScene.unity";
        private const string MESHES_DIR = "Assets/_Project/Art/Meshes/Islands";
        private const float TargetMeters = 3f; // metros reales por repetición de la textura

        [MenuItem("Tools/Scenes/Apply Texture Density (3 m)", false, 33)]
        public static void Apply() => ApplyDensity(TargetMeters);

        public static void ApplyDensity(float targetMeters)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.path.Contains("BaseScene"))
            {
                scene = EditorSceneManager.OpenScene(BASE_SCENE_PATH, OpenSceneMode.Single);
            }

            Directory.CreateDirectory(MESHES_DIR);
            var done = new HashSet<Mesh>();

            foreach (var go in CollectObjects())
            {
                var mf = go.GetComponent<MeshFilter>();
                var renderer = go.GetComponent<Renderer>();
                if (mf == null || renderer == null || mf.sharedMesh == null || mf.sharedMesh.uv.Length == 0) continue;

                Mesh mesh = mf.sharedMesh;
                float tiling = Mathf.Max(0.0001f, renderer.sharedMaterial.mainTextureScale.x);
                float currentMeters = MetersPerUvUnit(mesh, go.transform.lossyScale) / tiling;
                if (float.IsNaN(currentMeters) || currentMeters <= 0f) continue;

                float factor = currentMeters / targetMeters; // >1 = textura más densa
                if (Mathf.Abs(factor - 1f) < 0.05f) continue;

                // Si la malla es del paquete la copiamos; las copias propias de _Project se reescalan en su sitio.
                string path = AssetDatabase.GetAssetPath(mesh);
                if (!path.StartsWith("Assets/_Project/"))
                {
                    var copy = Object.Instantiate(mesh);
                    copy.name = go.name.Replace(" ", "").Replace("(", "_").Replace(")", "") + "_UV";
                    string copyPath = $"{MESHES_DIR}/{copy.name}.asset";
                    AssetDatabase.DeleteAsset(copyPath);
                    AssetDatabase.CreateAsset(copy, copyPath);
                    mesh = AssetDatabase.LoadAssetAtPath<Mesh>(copyPath);
                    Undo.RecordObject(mf, "Densidad de textura");
                    mf.sharedMesh = mesh;
                    var mc = go.GetComponent<MeshCollider>();
                    if (mc != null) { Undo.RecordObject(mc, "Densidad de textura"); mc.sharedMesh = mesh; }
                }

                if (done.Add(mesh))
                {
                    var uvs = new List<Vector2>();
                    mesh.GetUVs(0, uvs);
                    for (int i = 0; i < uvs.Count; i++) uvs[i] *= factor;
                    mesh.SetUVs(0, uvs);
                    mesh.RecalculateTangents();
                    EditorUtility.SetDirty(mesh);
                }

                Debug.Log($"[TextureDensity] {go.name}: {currentMeters:0.0} m → {targetMeters:0.0} m por repetición (UV ×{factor:0.00})");
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // Objetos a los que se les puso material nuevo con UV propias.
        private static List<GameObject> CollectObjects()
        {
            var list = new List<GameObject>();
            string[] prefixes = { "Mini_Island_Right_Front", "Mini_Island_Right_Back", "Island_Rock_Base", "Island_Portal_Core" };
            foreach (var prefix in prefixes)
            {
                AddIfFound(list, prefix);
                for (int i = 1; i <= 5; i++) AddIfFound(list, $"{prefix} ({i})");
            }
            return list;
        }

        private static void AddIfFound(List<GameObject> list, string name)
        {
            var go = GameObject.Find(name);
            if (go != null && go.GetComponent<MeshFilter>() != null) list.Add(go);
        }

        // Metros reales que cubre una unidad de UV (raíz de área real / área UV, sobre todos los triángulos).
        private static float MetersPerUvUnit(Mesh mesh, Vector3 scale)
        {
            var v = mesh.vertices;
            var uv = mesh.uv;
            var t = mesh.triangles;
            double worldArea = 0, uvArea = 0;
            for (int i = 0; i < t.Length; i += 3)
            {
                Vector3 a = Vector3.Scale(v[t[i]], scale), b = Vector3.Scale(v[t[i + 1]], scale), c = Vector3.Scale(v[t[i + 2]], scale);
                worldArea += Vector3.Cross(b - a, c - a).magnitude * 0.5;
                Vector2 ua = uv[t[i]], ub = uv[t[i + 1]], uc = uv[t[i + 2]];
                uvArea += Mathf.Abs((ub.x - ua.x) * (uc.y - ua.y) - (uc.x - ua.x) * (ub.y - ua.y)) * 0.5;
            }
            return uvArea > 1e-9 ? Mathf.Sqrt((float)(worldArea / uvArea)) : float.NaN;
        }
    }
}
#endif
