#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using VamosAprendiendo.Gameplay;

namespace VamosAprendiendo.EditorTools
{
    /// <summary>
    /// Añade un resplandor cálido (halo + luz que pulsa) a cada Island_Portal_Core de BaseScene, como un disco
    /// retroiluminado. Textura y material se generan en _Project/Art; los resplandores van en un grupo propio.
    /// </summary>
    public static class PortalGlowSetupTool
    {
        private const string BASE_SCENE_PATH = "Assets/_Project/Scenes/BaseScene.unity";
        private const string TEXTURE_PATH = "Assets/_Project/Art/Textures/FX/T_PortalHalo.png";
        private const string MATERIAL_PATH = "Assets/_Project/Art/Materials/FX/M_PortalHalo.mat";
        private const string GROUP_NAME = "--- [FX] ---";
        private const float HaloScale = 1.9f; // el halo mide 1,9 veces el diámetro del disco
        private const float DiscFraction = 1f / HaloScale; // fracción del radio del quad que ocupa el disco

        private static readonly string[] CoreNames =
        {
            "Island_Portal_Core", "Island_Portal_Core (1)", "Island_Portal_Core (2)", "Island_Portal_Core (3)", "Island_Portal_Core (4)"
        };

        [MenuItem("Tools/Scenes/Add Portal Glow (BaseScene)", false, 29)]
        public static void AddGlow()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.path.Contains("BaseScene"))
            {
                scene = EditorSceneManager.OpenScene(BASE_SCENE_PATH, OpenSceneMode.Single);
            }

            var texture = CreateHaloTexture();
            var material = CreateHaloMaterial(texture);
            var quadMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

            var old = GameObject.Find(GROUP_NAME);
            if (old != null) Object.DestroyImmediate(old);
            var group = new GameObject(GROUP_NAME);
            Undo.RegisterCreatedObjectUndo(group, "Create FX");
            var glows = new GameObject("PortalGlows");
            glows.transform.SetParent(group.transform, false);

            int created = 0;
            foreach (var name in CoreNames)
            {
                var core = GameObject.Find(name);
                if (core == null) { Debug.LogWarning($"[PortalGlow] No se encontró '{name}'."); continue; }
                var r = core.GetComponent<Renderer>();
                if (r == null) continue;

                CreateGlow(glows.transform, name, r.bounds, material, quadMesh);
                created++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[PortalGlow] {created} resplandores creados.");
        }

        private static readonly string[] RockNames =
        {
            "Island_Rock_Base", "Island_Rock_Base (1)", "Island_Rock_Base (2)", "Island_Rock_Base (3)", "Island_Rock_Base (4)"
        };

        // Centro (en planta) de la roca de isla más cercana al portal.
        private static Vector3 NearestRockCenter(Vector3 point)
        {
            Vector3 best = point;
            float bestDistance = float.MaxValue;
            foreach (var n in RockNames)
            {
                var go = GameObject.Find(n);
                var r = go != null ? go.GetComponent<Renderer>() : null;
                if (r == null) continue;
                Vector3 c = r.bounds.center;
                float d = Vector2.Distance(new Vector2(c.x, c.z), new Vector2(point.x, point.z));
                if (d < bestDistance) { bestDistance = d; best = c; }
            }
            return best;
        }
        private static void CreateGlow(Transform parent, string coreName, Bounds b, Material material, Mesh quadMesh)
        {
            // El disco es plano: su eje más delgado es la normal; los otros dos dan el diámetro.
            Vector3 size = b.size;
            int thin = size.x <= size.y && size.x <= size.z ? 0 : (size.y <= size.z ? 1 : 2);
            Vector3 normal = thin == 0 ? Vector3.right : (thin == 1 ? Vector3.up : Vector3.forward);
            float diameter = Mathf.Max(thin == 0 ? 0f : size.x, thin == 1 ? 0f : size.y, thin == 2 ? 0f : size.z);

            // El disco está encajado en la pared de una isla: el brillo va en su cara exterior, la que mira
            // lejos del centro de la roca de esa isla (la más cercana en planta).
            float halfThickness = size[thin] * 0.5f;
            Vector3 dir = normal;
            Vector3 toCore = b.center - NearestRockCenter(b.center);
            if (Vector3.Dot(toCore, normal) < 0f) dir = -normal;
            var root = new GameObject("Glow_" + coreName.Replace("Island_Portal_Core", "Core").Replace(" ", ""));
            root.transform.SetParent(parent, false);
            root.transform.position = b.center;

            // Halo: quad con degradado radial, en el plano del disco, visible por ambos lados.
            var haloGo = new GameObject("Halo");
            haloGo.transform.SetParent(root.transform, false);
            haloGo.transform.position = b.center + dir * (halfThickness + 0.08f);
            haloGo.transform.rotation = Quaternion.LookRotation(normal, thin == 1 ? Vector3.forward : Vector3.up);
            float quad = diameter * HaloScale;
            haloGo.transform.localScale = new Vector3(quad, quad, 1f);
            haloGo.AddComponent<MeshFilter>().sharedMesh = quadMesh;
            var mr = haloGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;

            // Luz puntual cálida sin sombras (barata en móvil) que ilumina la isla alrededor.
            var lightGo = new GameObject("Light");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.position = b.center + dir * (halfThickness + 2.5f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = diameter * 1.7f;
            light.shadows = LightShadows.None;
            light.color = new Color(1f, 0.72f, 0.32f);
            light.intensity = 4f;

            var glow = root.AddComponent<PortalGlow>();
            glow.Configure(mr, light);
            EditorUtility.SetDirty(glow);
        }

        // Degradado radial: opaco bajo el disco y desvanecimiento suave hacia afuera (el borde del quad es transparente).
        private static Texture2D CreateHaloTexture()
        {
            const int size = 256;
            Directory.CreateDirectory(Path.GetDirectoryName(TEXTURE_PATH));
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f;
                    float dy = (y + 0.5f) / size * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy); // 0 centro, 1 borde del quad

                    // Nada sobre el disco; el brillo nace en su borde y se desvanece hacia afuera (cola suave).
                    float t = Mathf.Clamp01((r - DiscFraction) / (1f - DiscFraction));
                    float edge = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((r - DiscFraction * 0.86f) / (DiscFraction * 0.14f)));
                    float a = r < DiscFraction * 0.86f ? 0f : edge * Mathf.Pow(1f - t, 2.2f);
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(TEXTURE_PATH, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(TEXTURE_PATH, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(TEXTURE_PATH);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 256;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH);
        }

        private static Material CreateHaloMaterial(Texture2D texture)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(MATERIAL_PATH));
            var material = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, MATERIAL_PATH);
            }

            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", new Color(1f, 0.72f, 0.32f, 1f));
            // Transparente y aditivo: suma luz sin oscurecer lo que hay detrás.
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 2f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.One);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", 0f); // visible por ambos lados
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)RenderQueue.Transparent;

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }
    }
}
#endif
