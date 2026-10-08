#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VamosAprendiendo.Gameplay;

namespace VamosAprendiendo.EditorTools
{
    /// <summary>
    /// Fauna de BaseScene: aves (Zacxophone/Birds, versión URP) volando en círculos sobre las plataformas inferiores
    /// y mariposas (Butterfly (Animated)) revoloteando sobre las plataformas.
    /// Salidas ordenadas: materiales en _Project/Art/Materials/Fauna, prefabs en _Project/Prefabs/Fauna.
    /// </summary>
    public static class FaunaSetupTool
    {
        private const string BASE_SCENE_PATH = "Assets/_Project/Scenes/BaseScene.unity";

        private const string BIRDS_DIR = "Assets/Zacxophone/Birds";
        private const string BIRD_URP_PREFABS = BIRDS_DIR + "/URP/Prefabs"; // la carpeta URP: Built-In y HDRP usan otros shaders
        private const string BUTTERFLY_DIR = "Assets/Butterfly (Animated)";

        private const string MATERIALS_DIR = "Assets/_Project/Art/Materials/Fauna";
        private const string PREFABS_DIR = "Assets/_Project/Prefabs/Fauna";

        private const float BirdScale = 8f;       // las aves miden ~0,17 m: se agrandan para verse desde la cámara
        private const float ButterflyScale = 3f;  // mariposa de ~0,28 m -> ~0,85 m

        [MenuItem("Tools/Scenes/Add Birds and Butterflies (BaseScene)", false, 25)]
        public static void Setup()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.path.Contains("BaseScene"))
            {
                scene = EditorSceneManager.OpenScene(BASE_SCENE_PATH, OpenSceneMode.Single);
            }

            EnsureFolders();
            SetBirdAnimationsToLoop();
            var butterflyMaterials = CreateButterflyMaterials();

            var birdSingle = CreateBirdPrefab("Bird_Single", "01BirdPrefab");
            var birdFlock3 = CreateBirdPrefab("Bird_Flock3", "03BirdsPrefab");
            var birdFlock5 = CreateBirdPrefab("Bird_Flock5", "05BirdsPrefab");
            var butterfly = CreateButterflyPrefab(butterflyMaterials);
            if (birdSingle == null || birdFlock3 == null || birdFlock5 == null || butterfly == null)
            {
                Debug.LogError("[FaunaSetup] No se pudieron crear los prefabs de fauna.");
                return;
            }

            // Grupo raíz de la fauna (con el estilo de cabeceras de la escena), siempre en la raíz y sin escala.
            var old = GameObject.Find("--- [FAUNA] ---");
            if (old != null) Object.DestroyImmediate(old);
            var faunaRoot = new GameObject("--- [FAUNA] ---");
            Undo.RegisterCreatedObjectUndo(faunaRoot, "Create Fauna");
            var birdsGroup = new GameObject("Birds");
            birdsGroup.transform.SetParent(faunaRoot.transform, false);
            var butterfliesGroup = new GameObject("Butterflies");
            butterfliesGroup.transform.SetParent(faunaRoot.transform, false);

            Random.InitState(20261008);
            var platforms = FindPlatforms(out var lowerPlatforms);

            // --- Aves: sobre las plataformas inferiores ---
            int flockIndex = 0;
            foreach (var p in lowerPlatforms)
            {
                float radius = 0.5f * Mathf.Max(p.size.x, p.size.z) + 4f;
                // El vuelo usa una altura relativa al centro de la plataforma (p.center.y), no absoluta.
                float height = p.top + 8f - p.center.y;

                // Una bandada de 3 que da vueltas en un sentido...
                var flock = PlaceBird(birdFlock3, birdsGroup.transform, $"Flock3_{p.name}");
                flock.Configure(p.center, radius, height, Random.Range(14f, 20f), flockIndex % 2 == 0, Random.Range(0f, 360f));

                // ...y un ave suelta en un círculo menor, en sentido contrario y a otra altura.
                var lone = PlaceBird(birdSingle, birdsGroup.transform, $"Bird_{p.name}");
                lone.Configure(p.center, radius * 0.65f, height + 3.5f, Random.Range(22f, 30f), flockIndex % 2 != 0, Random.Range(0f, 360f));
                flockIndex++;
            }

            // Bandada grande alrededor de la zona de inicio del jugador.
            var player = Object.FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                var start = PlaceBird(birdFlock5, birdsGroup.transform, "Flock5_Inicio");
                start.Configure(player.transform.position, 18f, 9f, 15f, true, 0f);
            }

            // --- Mariposas: sobre todas las plataformas ---
            foreach (var p in platforms)
            {
                int count = p.size.x * p.size.z > 1500f ? 3 : 2;
                float wander = Mathf.Min(0.35f * Mathf.Min(p.size.x, p.size.z), 9f);
                for (int i = 0; i < count; i++)
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(butterfly, butterfliesGroup.transform);
                    go.name = $"Butterfly_{p.name}_{i + 1}";
                    var offset = Random.insideUnitCircle * wander * 0.6f;
                    var wanderer = go.GetComponent<ButterflyWander>();
                    wanderer.Configure(new Vector3(p.center.x + offset.x, p.top, p.center.z + offset.y), wander);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[FaunaSetup] Aves: {birdsGroup.transform.childCount} grupos. Mariposas: {butterfliesGroup.transform.childCount}.");
        }

        // ---------- carpetas y ajustes de importación ----------

        private static void EnsureFolders()
        {
            foreach (var dir in new[] { MATERIALS_DIR, PREFABS_DIR })
            {
                Directory.CreateDirectory(dir);
            }
            AssetDatabase.Refresh();
        }

        // Los clips de aleteo de las aves vienen sin bucle: tras 0,67 s se quedarían congelados.
        private static void SetBirdAnimationsToLoop()
        {
            foreach (var name in new[] { "01Bird", "03Birds", "05Birds", "10Birds", "15Birds" })
            {
                string path = $"{BIRDS_DIR}/Meshes/{name}.fbx";
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) continue;

                var clips = importer.clipAnimations;
                if (clips == null || clips.Length == 0) clips = importer.defaultClipAnimations;

                bool changed = false;
                foreach (var c in clips)
                {
                    if (!c.loopTime) { c.loopTime = true; changed = true; }
                }
                if (changed)
                {
                    importer.clipAnimations = clips;
                    importer.SaveAndReimport();
                }
            }
        }

        // ---------- materiales (Standard -> URP Lit) ----------

        private static (Material body, Material hair) CreateButterflyMaterials()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");

            var body = LoadOrCreateMaterial($"{MATERIALS_DIR}/M_Butterfly_Body.mat", lit);
            body.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{BUTTERFLY_DIR}/FBX/butterfly.fbm/butterfly_Albedo.tga"));
            body.SetColor("_BaseColor", new Color(0.7f, 0.7f, 0.7f, 1f));
            body.SetFloat("_Smoothness", 0.25f);
            body.SetFloat("_Metallic", 0f);

            var hair = LoadOrCreateMaterial($"{MATERIALS_DIR}/M_Butterfly_Hair.mat", lit);
            hair.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{BUTTERFLY_DIR}/FBX/butterfly.fbm/hair_unity.tga"));
            hair.SetColor("_BaseColor", new Color(0.7f, 0.7f, 0.7f, 1f));
            hair.SetFloat("_Smoothness", 0.1f);
            // Transparente (el material original usa modo Fade).
            hair.SetFloat("_Surface", 1f);
            hair.SetFloat("_Blend", 0f);
            hair.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            hair.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            hair.SetFloat("_ZWrite", 0f);
            hair.SetOverrideTag("RenderType", "Transparent");
            hair.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            hair.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            EditorUtility.SetDirty(body);
            EditorUtility.SetDirty(hair);
            AssetDatabase.SaveAssets();
            return (body, hair);
        }

        private static Material LoadOrCreateMaterial(string path, Shader shader)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        // ---------- prefabs ----------

        private static GameObject CreateBirdPrefab(string prefabName, string sourceName)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>($"{BIRD_URP_PREFABS}/{sourceName}.prefab");
            if (source == null) return null;

            var root = new GameObject(prefabName);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
            model.name = "Model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = source.transform.localScale * BirdScale;

            root.AddComponent<CircleFlight>();
            var prox = root.AddComponent<ProximityActivator>();
            var so = new SerializedObject(prox);
            so.FindProperty("model").objectReferenceValue = model;
            so.FindProperty("hideDistance").floatValue = 110f;
            so.ApplyModifiedProperties();

            var saved = PrefabUtility.SaveAsPrefabAsset(root, $"{PREFABS_DIR}/{prefabName}.prefab");
            Object.DestroyImmediate(root);
            return saved;
        }

        private static GameObject CreateButterflyPrefab((Material body, Material hair) mats)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>($"{BUTTERFLY_DIR}/Prefab/Butterfly.prefab");
            if (source == null) return null;

            var root = new GameObject("Butterfly");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
            model.name = "Model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = source.transform.localScale * ButterflyScale; // el FBX trae su propia escala (0,01)

            // Cambia los materiales Standard del paquete por sus copias URP.
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                var shared = r.sharedMaterials;
                for (int i = 0; i < shared.Length; i++)
                {
                    bool isHair = shared[i] != null && shared[i].name.ToLowerInvariant().Contains("hair");
                    shared[i] = isHair ? mats.hair : mats.body;
                }
                r.sharedMaterials = shared;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            }

            var wander = root.AddComponent<ButterflyWander>();
            var prox = root.AddComponent<ProximityActivator>();
            var so = new SerializedObject(prox);
            so.FindProperty("model").objectReferenceValue = model;
            so.FindProperty("hideDistance").floatValue = 60f; // 23 renderers por mariposa: solo cerca del jugador
            so.ApplyModifiedProperties();
            var wso = new SerializedObject(wander);
            wso.FindProperty("flapAnimation").objectReferenceValue = model.GetComponent<Animation>();
            wso.ApplyModifiedProperties();

            var saved = PrefabUtility.SaveAsPrefabAsset(root, $"{PREFABS_DIR}/Butterfly.prefab");
            Object.DestroyImmediate(root);
            return saved;
        }

        private static CircleFlight PlaceBird(GameObject prefab, Transform parent, string name)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            return go.GetComponent<CircleFlight>();
        }

        // ---------- plataformas de la escena ----------

        private struct Platform
        {
            public string name;
            public Vector3 center; // centro en XZ y Y de la placa
            public Vector3 size;
            public float top;      // altura de la superficie transitable (con la capa de hierba)
        }

        private static List<Platform> FindPlatforms(out List<Platform> lower)
        {
            var all = new List<Platform>();
            lower = new List<Platform>();

            var names = new List<string> { "Plataforma_uno", "Plataforma_uno (1)", "Plataforma_uno (2)", "Plataforma_uno (3)", "Plataforma_uno (4)", "Plataforma_dos", "Plataforma_tres" };
            foreach (var n in names)
            {
                var go = GameObject.Find(n);
                if (go == null) { Debug.LogWarning($"[FaunaSetup] No se encontró '{n}'."); continue; }
                var r = go.GetComponentInChildren<Renderer>();
                if (r == null) continue;

                var b = r.bounds;
                var p = new Platform
                {
                    name = n.Replace(" ", "").Replace("(", "").Replace(")", ""),
                    center = b.center,
                    size = b.size,
                    top = b.max.y + 1.2f
                };
                all.Add(p);
                if (n.StartsWith("Plataforma_uno")) lower.Add(p);
            }
            return all;
        }
    }
}
#endif
