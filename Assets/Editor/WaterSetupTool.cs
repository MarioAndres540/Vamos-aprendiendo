#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using VamosAprendiendo.Gameplay;

namespace VamosAprendiendo.EditorTools
{
    /// <summary>
    /// Coloca agua (Simple Water Shader URP) como mar bajo las islas flotantes de BaseScene, con bruma lejana
    /// y niebla bajo el agua. Usa copias del shader y del material en _Project (el paquete no se modifica):
    /// la copia del shader es de doble cara para que el mar también se vea desde abajo al caer.
    /// </summary>
    public static class WaterSetupTool
    {
        private const string BASE_SCENE_PATH = "Assets/_Project/Scenes/BaseScene.unity";
        private const string WATER_DIR = "Assets/IgniteCoders/Simple Water Shader";
        private const string SHADERS_DIR = "Assets/_Project/Art/Shaders/Water";
        private const string MATERIALS_DIR = "Assets/_Project/Art/Materials/Water";
        private const string ROOT_NAME = "Water_Sea";

        // Medidas de las islas (m): x -78..32, z -110..20, y desde -3.8. El agua queda por debajo de las puntas.
        private static readonly Vector2 IslandCenterXZ = new Vector2(-23f, -45f);
        private const float WaterY = -10f;
        private const int GridSize = 13;          // 13x13 bloques de 50 m = 650 m de mar (el borde queda dentro de la bruma)
        private const float TileOverlap = 1.003f; // los bloques se solapan 0,15 m: sin grietas blancas y casi sin franja visible en las uniones

        [MenuItem("Tools/Scenes/Add Water Below Islands (BaseScene)", false, 24)]
        public static void AddWater()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.path.Contains("BaseScene"))
            {
                scene = EditorSceneManager.OpenScene(BASE_SCENE_PATH, OpenSceneMode.Single);
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{WATER_DIR}/Prefabs/WaterBlock_50m.prefab");
            if (prefab == null)
            {
                Debug.LogError("[WaterSetup] Falta el paquete Simple Water Shader (prefab).");
                return;
            }

            var material = CreateSeaMaterial();
            if (material == null) return;

            var old = GameObject.Find(ROOT_NAME);
            if (old != null) Object.DestroyImmediate(old);

            var root = new GameObject(ROOT_NAME);
            Undo.RegisterCreatedObjectUndo(root, "Create Sea");
            // En la raíz de la escena: el grupo "--- [ENVIRONMENT] ---" está escalado (x1.5) y agrandaría los bloques.
            root.transform.position = Vector3.zero;
            root.transform.localScale = Vector3.one;

            // Tamaño real del bloque, medido del propio prefab.
            float block = 50f;
            var probe = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var probeRenderer = probe.GetComponentInChildren<MeshRenderer>();
            if (probeRenderer != null) block = Mathf.Max(1f, probeRenderer.bounds.size.x);
            Object.DestroyImmediate(probe);

            int half = GridSize / 2;
            for (int gx = -half; gx <= half; gx++)
            {
                for (int gz = -half; gz <= half; gz++)
                {
                    var tile = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
                    tile.name = $"WaterBlock_{gx}_{gz}";
                    tile.transform.position = new Vector3(IslandCenterXZ.x + gx * block, WaterY, IslandCenterXZ.y + gz * block);
                    tile.transform.rotation = Quaternion.identity;
                    tile.transform.localScale = new Vector3(TileOverlap, 1f, TileOverlap);
                    tile.isStatic = false;

                    var mr = tile.GetComponentInChildren<MeshRenderer>();
                    if (mr != null)
                    {
                        mr.sharedMaterial = material;
                        mr.shadowCastingMode = ShadowCastingMode.Off;
                        mr.receiveShadows = false; // ahorra coste en móvil
                        mr.lightProbeUsage = LightProbeUsage.Off;
                        mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
                    }
                }
            }

            // Bruma lejana y niebla bajo el agua. La niebla lineal queda guardada en la escena (necesario para los builds).
            var atmosphere = new GameObject("SeaAtmosphere");
            atmosphere.transform.SetParent(root.transform, false);
            var so = new SerializedObject(atmosphere.AddComponent<SeaAtmosphere>());
            so.FindProperty("waterLevel").floatValue = WaterY;
            so.ApplyModifiedProperties();

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.82f, 0.92f, 0.95f);
            RenderSettings.fogStartDistance = 90f;
            RenderSettings.fogEndDistance = 260f;

            // El shader usa Scene Depth (color por profundidad y espuma): la cámara debe generar la textura de profundidad.
            var cam = Camera.main;
            if (cam != null)
            {
                var data = cam.GetUniversalAdditionalCameraData();
                data.requiresDepthOption = CameraOverrideOption.On;
                if (cam.farClipPlane < 900f) cam.farClipPlane = 900f;
                EditorUtility.SetDirty(cam);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[WaterSetup] Mar creado: {GridSize}x{GridSize} bloques de {block} m a y={WaterY}, con bruma y doble cara.");
        }

        // Copia el shader graph del paquete con "Two Sided" activado y un material (copia de Water_mat_03) que lo usa.
        private static Material CreateSeaMaterial()
        {
            Directory.CreateDirectory(SHADERS_DIR);
            Directory.CreateDirectory(MATERIALS_DIR);
            AssetDatabase.Refresh();

            string graphSource = $"{WATER_DIR}/Resources/WaterShader.shadergraph";
            string graphCopy = $"{SHADERS_DIR}/WaterSeaTwoSided.shadergraph";
            string materialSource = $"{WATER_DIR}/Resources/Water_mat_03.mat"; // turquesa translúcido, el más parecido a la referencia
            string materialCopy = $"{MATERIALS_DIR}/Water_Sea.mat";

            if (!File.Exists(graphCopy))
            {
                if (!AssetDatabase.CopyAsset(graphSource, graphCopy))
                {
                    Debug.LogError("[WaterSetup] No se pudo copiar el shader graph del agua.");
                    return null;
                }
            }

            string json = File.ReadAllText(graphCopy);
            if (json.Contains("\"m_TwoSided\": false"))
            {
                File.WriteAllText(graphCopy, json.Replace("\"m_TwoSided\": false", "\"m_TwoSided\": true"));
            }
            AssetDatabase.ImportAsset(graphCopy, ImportAssetOptions.ForceUpdate);

            var shader = AssetDatabase.LoadAssetAtPath<Shader>(graphCopy);
            if (shader == null)
            {
                Debug.LogError("[WaterSetup] El shader copiado no se pudo cargar (¿error de compilación del graph?).");
                return null;
            }

            if (!File.Exists(materialCopy) && !AssetDatabase.CopyAsset(materialSource, materialCopy))
            {
                Debug.LogError("[WaterSetup] No se pudo copiar el material del agua.");
                return null;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(materialCopy);
            material.shader = shader;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }
    }
}
#endif
