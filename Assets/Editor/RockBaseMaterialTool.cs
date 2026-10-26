#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = System.Random;

namespace VamosAprendiendo.EditorTools
{
    /// <summary>
    /// Asigna a cada Island_Rock_Base un material de arena (YughuesFreeSandMaterials) elegido al azar, sin repetir.
    /// Si la malla de alguna roca no tuviera UV, se genera una copia con UV por proyección para que la textura se vea.
    /// Cada ejecución sortea de nuevo.
    /// </summary>
    public static class RockBaseMaterialTool
    {
        private const string BASE_SCENE_PATH = "Assets/_Project/Scenes/BaseScene.unity";
        private const string SAND_FOLDER = "Assets/_ThirdParty/Textures/YughuesFreeSandMaterials/Materials";
        private const string MESHES_DIR = "Assets/_Project/Art/Meshes/Islands";
        private const float TileMeters = 6f;

        private static readonly string[] RockNames =
        {
            "Island_Rock_Base", "Island_Rock_Base (1)", "Island_Rock_Base (2)", "Island_Rock_Base (3)", "Island_Rock_Base (4)"
        };

        [MenuItem("Tools/Scenes/Randomize Rock Base Materials", false, 32)]
        public static void Randomize()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.path.Contains("BaseScene"))
            {
                scene = EditorSceneManager.OpenScene(BASE_SCENE_PATH, OpenSceneMode.Single);
            }

            var pool = new List<Material>();
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { SAND_FOLDER }))
            {
                pool.Add(AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid)));
            }
            pool.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            var rng = new Random(Environment.TickCount);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            Directory.CreateDirectory(MESHES_DIR);
            for (int i = 0; i < RockNames.Length && i < pool.Count; i++)
            {
                var go = GameObject.Find(RockNames[i]);
                if (go == null) { Debug.LogWarning($"[RockBaseMaterial] No se encontró '{RockNames[i]}'."); continue; }

                var renderer = go.GetComponent<Renderer>();
                var mf = go.GetComponent<MeshFilter>();
                var collider = go.GetComponent<MeshCollider>();

                if (mf.sharedMesh.uv.Length == 0)
                {
                    var mesh = CreateUvMesh(go, i);
                    Undo.RecordObject(mf, "Material roca");
                    mf.sharedMesh = mesh;
                    if (collider != null) { Undo.RecordObject(collider, "Material roca"); collider.sharedMesh = mesh; }
                }

                Undo.RecordObject(renderer, "Material roca");
                var mats = renderer.sharedMaterials;
                for (int m = 0; m < mats.Length; m++) mats[m] = pool[i];
                renderer.sharedMaterials = mats;
                EditorUtility.SetDirty(renderer);

                Debug.Log($"[RockBaseMaterial] {RockNames[i]} → {pool[i].name}");
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static Mesh CreateUvMesh(GameObject go, int index)
        {
            var source = go.GetComponent<MeshFilter>().sharedMesh;
            Vector3 scale = go.transform.lossyScale;
            var sv = source.vertices;
            var sn = source.normals;
            var tris = source.triangles;
            bool hasNormals = sn != null && sn.Length == sv.Length;

            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var idx = new List<int>();
            for (int t = 0; t < tris.Length; t += 3)
            {
                Vector3 a = sv[tris[t]], b = sv[tris[t + 1]], c = sv[tris[t + 2]];
                Vector3 fn = Vector3.Cross(Vector3.Scale(b - a, scale), Vector3.Scale(c - a, scale)).normalized;
                float ax = Mathf.Abs(fn.x), ay = Mathf.Abs(fn.y), az = Mathf.Abs(fn.z);
                for (int k = 0; k < 3; k++)
                {
                    Vector3 v = sv[tris[t + k]];
                    Vector3 w = Vector3.Scale(v, scale);
                    Vector2 uv = (ay >= ax && ay >= az) ? new Vector2(w.x, w.z) : (ax >= az ? new Vector2(w.z, w.y) : new Vector2(w.x, w.y));
                    verts.Add(v);
                    norms.Add(hasNormals ? sn[tris[t + k]] : fn);
                    uvs.Add(uv / TileMeters);
                    idx.Add(verts.Count - 1);
                }
            }

            var mesh = new Mesh { name = $"Island_Rock_Base_{index}_UV" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(idx, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            string path = $"{MESHES_DIR}/Island_Rock_Base_{index}_UV.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            return AssetDatabase.LoadAssetAtPath<Mesh>(path);
        }
    }
}
#endif
