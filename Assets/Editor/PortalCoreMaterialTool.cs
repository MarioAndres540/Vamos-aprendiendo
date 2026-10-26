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
    /// Asigna a cada Island_Portal_Core un material elegido al azar entre los paquetes A_piece_of_nature, Texture_Me y Blink.
    /// Esos paquetes usan shaders del pipeline antiguo (Standard, Mobile/Unlit), que en URP se verían magenta, así que se
    /// crea una copia URP Lit del material elegido en _Project/Art/Materials/PortalCore. Las mallas no tienen UV: se genera
    /// una copia con UV por proyección para que la textura se vea. Cada ejecución sortea de nuevo.
    /// </summary>
    public static class PortalCoreMaterialTool
    {
        private const string BASE_SCENE_PATH = "Assets/_Project/Scenes/BaseScene.unity";
        private const string OUTPUT_MATERIALS = "Assets/_Project/Art/Materials/PortalCore";
        private const string OUTPUT_MESHES = "Assets/_Project/Art/Meshes/Islands";
        private const float TileMeters = 4f; // metros que cubre una repetición de la textura

        private static readonly string[] SourceFolders = { "Assets/_ThirdParty/Vegetation/A_piece_of_nature", "Assets/_ThirdParty/Textures/Texture_Me", "Assets/_ThirdParty/Textures/Blink" };
        private static readonly string[] CoreNames =
        {
            "Island_Portal_Core", "Island_Portal_Core (1)", "Island_Portal_Core (2)", "Island_Portal_Core (3)", "Island_Portal_Core (4)"
        };

        [MenuItem("Tools/Scenes/Randomize Portal Core Materials", false, 31)]
        public static void Randomize()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.path.Contains("BaseScene"))
            {
                scene = EditorSceneManager.OpenScene(BASE_SCENE_PATH, OpenSceneMode.Single);
            }

            var pool = CollectPool();
            if (pool.Count < CoreNames.Length)
            {
                Debug.LogError($"[PortalCoreMaterial] Solo hay {pool.Count} materiales con textura en los paquetes nuevos.");
                return;
            }

            // Sorteo sin repetir: cada portal recibe un material distinto.
            var rng = new Random(Environment.TickCount);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            Directory.CreateDirectory(OUTPUT_MATERIALS);
            Directory.CreateDirectory(OUTPUT_MESHES);

            for (int i = 0; i < CoreNames.Length; i++)
            {
                var core = GameObject.Find(CoreNames[i]);
                if (core == null) { Debug.LogWarning($"[PortalCoreMaterial] No se encontró '{CoreNames[i]}'."); continue; }

                var source = pool[i];
                var urpMaterial = CreateUrpCopy(source);
                var mesh = CreateUvMesh(core, i);

                var mf = core.GetComponent<MeshFilter>();
                var renderer = core.GetComponent<Renderer>();
                var collider = core.GetComponent<MeshCollider>();
                Undo.RecordObject(mf, "Material portal");
                Undo.RecordObject(renderer, "Material portal");
                mf.sharedMesh = mesh;
                if (collider != null) { Undo.RecordObject(collider, "Material portal"); collider.sharedMesh = mesh; }

                var mats = renderer.sharedMaterials;
                for (int m = 0; m < mats.Length; m++) mats[m] = urpMaterial;
                renderer.sharedMaterials = mats;
                EditorUtility.SetDirty(renderer);

                Debug.Log($"[PortalCoreMaterial] {CoreNames[i]} → {AssetDatabase.GetAssetPath(source).Replace("Assets/", "")}");
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // Materiales con textura principal de los tres paquetes (sin repetir la misma textura).
        private static List<Material> CollectPool()
        {
            var pool = new List<Material>();
            var seenTextures = new HashSet<int>();
            foreach (var guid in AssetDatabase.FindAssets("t:Material", SourceFolders))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                var tex = MainTexture(m);
                if (tex == null || !seenTextures.Add(tex.GetInstanceID())) continue;
                pool.Add(m);
            }
            return pool;
        }

        private static Texture MainTexture(Material m)
        {
            if (m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null) return m.GetTexture("_BaseMap");
            if (m.HasProperty("_MainTex")) return m.GetTexture("_MainTex");
            return null;
        }

        private static Material CreateUrpCopy(Material source)
        {
            string pack = Path.GetFileName(Path.GetDirectoryName(AssetDatabase.GetAssetPath(source)));
            string rootFolder = AssetDatabase.GetAssetPath(source).Split('/')[1];
            string safe = $"PC_{rootFolder}_{source.name}".Replace(' ', '_').Replace('\'', '_').Replace('(', '_').Replace(')', '_').Replace('#', '_');
            string path = $"{OUTPUT_MATERIALS}/{safe}.mat";

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            var tex = MainTexture(source);
            material.SetTexture("_BaseMap", tex);
            material.SetTextureScale("_BaseMap", source.mainTextureScale);
            material.SetTextureOffset("_BaseMap", source.mainTextureOffset);

            Color tint = source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
            tint.a = 1f;
            material.SetColor("_BaseColor", tint);

            if (source.HasProperty("_BumpMap") && source.GetTexture("_BumpMap") != null)
            {
                material.SetTexture("_BumpMap", source.GetTexture("_BumpMap"));
                material.EnableKeyword("_NORMALMAP");
            }

            material.SetFloat("_Smoothness", 0.2f);
            material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        // Copia de la malla con UV por proyección según la cara (la malla original no tiene UV).
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

            var mesh = new Mesh { name = $"Island_Portal_Core_{index}_UV" };
            mesh.indexFormat = verts.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(idx, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            string path = $"{OUTPUT_MESHES}/Island_Portal_Core_{index}_UV.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            return AssetDatabase.LoadAssetAtPath<Mesh>(path);
        }
    }
}
#endif
