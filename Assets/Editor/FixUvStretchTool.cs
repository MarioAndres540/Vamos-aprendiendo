#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VamosAprendiendo.EditorTools
{
    /// <summary>
    /// Rehace las UV de islas, rocas y portales para eliminar el estiramiento (rayas) en caras inclinadas o giradas.
    /// Cada triángulo se proyecta sobre SU propio plano, con ejes alineados al mundo (horizontal a lo largo de la cara y
    /// vertical hacia arriba; en caras casi horizontales, eje X del mundo), en metros reales. Así la textura mantiene
    /// la misma densidad en todas las caras y las caras coplanares encajan sin costuras.
    /// </summary>
    public static class FixUvStretchTool
    {
        private const string BASE_SCENE_PATH = "Assets/_Project/Scenes/BaseScene.unity";
        private const float TargetMeters = 3f;

        [MenuItem("Tools/Scenes/Fix Texture Stretching (3 m)", false, 34)]
        public static void Fix()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.path.Contains("BaseScene"))
            {
                scene = EditorSceneManager.OpenScene(BASE_SCENE_PATH, OpenSceneMode.Single);
            }

            var processed = new HashSet<Mesh>();
            var copies = new Dictionary<Mesh, Mesh>();
            foreach (var go in Collect())
            {
                var mf = go.GetComponent<MeshFilter>();
                var mesh = mf.sharedMesh;
                string path = AssetDatabase.GetAssetPath(mesh);
                if (!path.StartsWith("Assets/_Project/Art/Meshes/"))
                {
                    // Malla de un modelo (.blend/.fbx): se trabaja sobre una copia propia, nunca sobre el modelo.
                    if (!copies.TryGetValue(mesh, out var copy))
                    {
                        copy = Object.Instantiate(mesh);
                        copy.name = go.name.Replace(" ", "").Replace("(", "_").Replace(")", "") + "_UV";
                        string copyPath = $"Assets/_Project/Art/Meshes/Islands/{copy.name}.asset";
                        AssetDatabase.DeleteAsset(copyPath);
                        AssetDatabase.CreateAsset(copy, copyPath);
                        copy = AssetDatabase.LoadAssetAtPath<Mesh>(copyPath);
                        copies[mesh] = copy;
                    }
                    Undo.RecordObject(mf, "UV sin estiramiento");
                    mf.sharedMesh = copy;
                    var mcol = go.GetComponent<MeshCollider>();
                    if (mcol != null) { Undo.RecordObject(mcol, "UV sin estiramiento"); mcol.sharedMesh = copy; }
                    mesh = copy;
                }
                if (!processed.Add(mesh)) continue;

                Rebuild(mesh, go.transform, go.name.StartsWith("Island_Portal_Core"));
                Debug.Log($"[FixUv] {go.name}: UV rehechas ({mesh.vertexCount} vértices).");
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }


        /// <summary>
        /// Bordes de pasto, tapas de pasto y Plataforma_uno: comparten una misma malla con distinta escala en cada copia
        /// (y los bordes no traen UV), así que cada objeto recibe su propia malla con UV en metros reales.
        /// </summary>
        [MenuItem("Tools/Scenes/Fix Grass Rims, Caps and Platforms UV (3 m)", false, 35)]
        public static void FixPerInstance()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.path.Contains("BaseScene"))
            {
                scene = EditorSceneManager.OpenScene(BASE_SCENE_PATH, OpenSceneMode.Single);
            }

            var sand = AssetDatabase.FindAssets("M_YFSM_Sand01 t:Material");
            Material fallback = null;
            foreach (var g in sand)
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g));
                if (m != null && m.name == "M_YFSM_Sand01") { fallback = m; break; }
            }

            System.IO.Directory.CreateDirectory("Assets/_Project/Art/Meshes/Islands");
            int count = 0;
            foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string n = mf.name;
                bool target = n.StartsWith("Island_Grass_Rim") || n.StartsWith("Grass_Cap_Mini_Island") || n.StartsWith("Plataforma_uno");
                if (!target || mf.sharedMesh == null) continue;

                string safe = n.Replace(" ", "").Replace("(", "_").Replace(")", "");
                string path = $"Assets/_Project/Art/Meshes/Islands/{safe}_UV.asset";
                var source = mf.sharedMesh;
                var copy = Object.Instantiate(source);
                copy.name = safe + "_UV";
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(copy, path);
                copy = AssetDatabase.LoadAssetAtPath<Mesh>(path);

                Undo.RecordObject(mf, "UV sin estiramiento");
                mf.sharedMesh = copy;
                var col = mf.GetComponent<MeshCollider>();
                if (col != null) { Undo.RecordObject(col, "UV sin estiramiento"); col.sharedMesh = copy; }

                Rebuild(copy, mf.transform, false);

                var r = mf.GetComponent<MeshRenderer>();
                if (r != null)
                {
                    var mats = r.sharedMaterials;
                    bool changed = false;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        // Sin material propio (Lit por defecto): mismo material de arena que sus vecinas.
                        if ((mats[i] == null || mats[i].name == "Lit") && fallback != null) { mats[i] = fallback; changed = true; }
                        if (mats[i] != null && mats[i].HasProperty("_BaseMap") && mats[i].GetTextureScale("_BaseMap") != Vector2.one && AssetDatabase.GetAssetPath(mats[i]).StartsWith("Assets/_Project"))
                        {
                            // La densidad ahora la dan las UV (3 m por repetición): el tiling del material vuelve a 1.
                            mats[i].SetTextureScale("_BaseMap", Vector2.one);
                            EditorUtility.SetDirty(mats[i]);
                        }
                    }
                    if (changed) { Undo.RecordObject(r, "Material"); r.sharedMaterials = mats; }
                }
                count++;
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[FixUv] Bordes de pasto, tapas y Plataforma_uno: {count} objetos con UV propias.");
        }

        private static IEnumerable<GameObject> Collect()
        {
            string[] prefixes = { "Mini_Island_Right_Front", "Mini_Island_Right_Back", "Island_Rock_Base", "Island_Portal_Core" };
            // Escalones de la plataforma inicial: su UV viene del .blend y estira la cara frontal.
            for (int s = 0; s <= 4; s++)
            {
                var step = GameObject.Find($"Stair_Step_{s}");
                if (step != null && step.GetComponent<MeshFilter>() != null) yield return step;
            }
            foreach (var extra in new[] { "Dais_Inner", "Rock_Dais_1", "Rock_Back_Standing", "Landmark_Boulder_Stair" })
            {
                var e = GameObject.Find(extra);
                if (e != null && e.GetComponent<MeshFilter>() != null) yield return e;
            }
            foreach (var mfFound in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mfFound.name == "Stair_Step_4 (1)") yield return mfFound.gameObject;
            }
            foreach (var p in prefixes)
            {
                for (int i = 0; i <= 5; i++)
                {
                    var go = GameObject.Find(i == 0 ? p : $"{p} ({i})");
                    if (go != null && go.GetComponent<MeshFilter>() != null) yield return go;
                }
            }
        }

        private static void Rebuild(Mesh mesh, Transform t, bool curved)
        {
            var sv = mesh.vertices;
            var sn = mesh.normals;
            var tris = mesh.triangles;
            bool hasNormals = sn != null && sn.Length == sv.Length;

            var verts = new List<Vector3>(tris.Length);
            var norms = new List<Vector3>(tris.Length);
            var uvs = new List<Vector2>(tris.Length);
            var idx = new List<int>(tris.Length);

            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 la = sv[tris[i]], lb = sv[tris[i + 1]], lc = sv[tris[i + 2]];
                Vector3 a = t.TransformPoint(la), b = t.TransformPoint(lb), c = t.TransformPoint(lc);

                Vector3 n = Vector3.Cross(b - a, c - a).normalized;
                if (n.sqrMagnitude < 0.5f) n = Vector3.up;

                // Ejes del plano de la cara, alineados al mundo.
                Vector3 u = Mathf.Abs(n.y) > 0.9f ? Vector3.right : Vector3.Cross(Vector3.up, n).normalized;
                Vector3 v = Vector3.Cross(n, u).normalized;
                if (curved)
                {
                    // Superficie curva (portal): proyección por eje dominante del mundo, continua entre triángulos vecinos.
                    float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);
                    if (ay >= ax && ay >= az) { u = Vector3.right; v = Vector3.forward; }
                    else if (ax >= az) { u = Vector3.forward; v = Vector3.up; }
                    else { u = Vector3.right; v = Vector3.up; }
                }

                for (int k = 0; k < 3; k++)
                {
                    Vector3 lp = sv[tris[i + k]];
                    Vector3 wp = t.TransformPoint(lp);
                    verts.Add(lp);
                    norms.Add(hasNormals ? sn[tris[i + k]] : t.InverseTransformDirection(n));
                    uvs.Add(new Vector2(Vector3.Dot(wp, u), Vector3.Dot(wp, v)) / TargetMeters);
                    idx.Add(verts.Count - 1);
                }
            }

            mesh.Clear();
            mesh.indexFormat = verts.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(idx, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            EditorUtility.SetDirty(mesh);
        }
    }
}
#endif
