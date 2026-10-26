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
        private const string WATER_DIR = "Assets/_ThirdParty/Environment/IgniteCoders/Simple Water Shader";
        private const string SHADERS_DIR = "Assets/_Project/Art/Shaders/Water";
        private const string MATERIALS_DIR = "Assets/_Project/Art/Materials/Water";
        private const string ROOT_NAME = "Water_Sea";

        // Medidas de las islas (m): x -78..32, z -110..20, y desde -3.8. El agua queda por debajo de las puntas.
        private static readonly Vector2 IslandCenterXZ = new Vector2(-23f, -45f);
        private const float WaterY = -10f;
        private const float SeaHalfSize = 2500f;  // 5 km de mar en una sola malla: el borde queda casi en la línea del horizonte y dentro de la bruma
        private const float DenseHalfSize = 90f;  // zona central (islas y vista del jugador) con malla fina
        private const float DenseStep = 1.25f;    // separación de vértices de la zona fina, similar a la del bloque original (~1,1 m)
        private const float UvMetersPerTile = 50f; // el bloque original repetía su textura cada 50 m: se conserva el mismo tamaño de ondas
        private const string MESHES_DIR = "Assets/_Project/Art/Meshes/Water";

        [MenuItem("Tools/Scenes/Add Water Below Islands (BaseScene)", false, 24)]
        public static void AddWater()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.path.Contains("BaseScene"))
            {
                scene = EditorSceneManager.OpenScene(BASE_SCENE_PATH, OpenSceneMode.Single);
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

            // Una sola malla grande en lugar de muchos bloques: sin uniones, grietas ni franjas.
            var seaMesh = CreateSeaMesh();
            var plane = new GameObject("Sea_Plane");
            plane.transform.SetParent(root.transform, false);
            plane.transform.position = new Vector3(IslandCenterXZ.x, WaterY, IslandCenterXZ.y);
            plane.isStatic = false;
            plane.AddComponent<MeshFilter>().sharedMesh = seaMesh;
            var mr = plane.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false; // ahorra coste en móvil
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            // Bruma lejana y niebla bajo el agua. La niebla lineal queda guardada en la escena (necesario para los builds).
            var atmosphere = new GameObject("SeaAtmosphere");
            atmosphere.transform.SetParent(root.transform, false);
            var so = new SerializedObject(atmosphere.AddComponent<SeaAtmosphere>());
            so.FindProperty("waterLevel").floatValue = WaterY;
            so.ApplyModifiedProperties();

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.82f, 0.92f, 0.95f);
            RenderSettings.fogStartDistance = 60f;
            RenderSettings.fogEndDistance = 190f;

            // El shader usa Scene Depth (color por profundidad y espuma): la cámara debe generar la textura de profundidad.
            var cam = Camera.main;
            if (cam != null)
            {
                var data = cam.GetUniversalAdditionalCameraData();
                data.requiresDepthOption = CameraOverrideOption.On;
                if (cam.farClipPlane < 3500f) cam.farClipPlane = 3500f;
                EditorUtility.SetDirty(cam);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[WaterSetup] Mar creado: una malla de {SeaHalfSize * 2} m ({seaMesh.vertexCount} vértices) a y={WaterY}, con bruma y doble cara.");
        }

        // Malla plana centrada en el origen: fina en el centro y cada vez más espaciada hacia los bordes.
        // Las UV siguen las coordenadas del mundo (posición / 50 m), igual que el bloque original, para que las ondas conserven su tamaño.
        private static Mesh CreateSeaMesh()
        {
            Directory.CreateDirectory(MESHES_DIR);

            float[] axis = BuildAxis();
            int n = axis.Length;
            var vertices = new Vector3[n * n];
            var uvs = new Vector2[n * n];
            var normals = new Vector3[n * n];
            var tangents = new Vector4[n * n];

            for (int z = 0; z < n; z++)
            {
                for (int x = 0; x < n; x++)
                {
                    int i = z * n + x;
                    vertices[i] = new Vector3(axis[x], 0f, axis[z]);
                    uvs[i] = new Vector2(axis[x] / UvMetersPerTile + 0.5f, axis[z] / UvMetersPerTile + 0.5f);
                    normals[i] = Vector3.up;
                    tangents[i] = new Vector4(1f, 0f, 0f, -1f);
                }
            }

            var triangles = new int[(n - 1) * (n - 1) * 6];
            int t = 0;
            for (int z = 0; z < n - 1; z++)
            {
                for (int x = 0; x < n - 1; x++)
                {
                    int a = z * n + x, b = a + 1, c = a + n, d = c + 1;
                    triangles[t++] = a; triangles[t++] = c; triangles[t++] = b;
                    triangles[t++] = b; triangles[t++] = c; triangles[t++] = d;
                }
            }

            var mesh = new Mesh { name = "SeaPlane", indexFormat = IndexFormat.UInt32 };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.tangents = tangents;
            mesh.triangles = triangles;
            // Margen vertical por si el shader desplaza los vértices (olas): evita que se recorte la malla.
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(SeaHalfSize * 2f, 20f, SeaHalfSize * 2f));

            string path = $"{MESHES_DIR}/SeaPlane.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<Mesh>(path);
        }

        private static float[] BuildAxis()
        {
            var positive = new System.Collections.Generic.List<float> { 0f };
            float x = 0f;
            while (x < DenseHalfSize)
            {
                x += DenseStep;
                positive.Add(x);
            }

            float step = DenseStep;
            while (x < SeaHalfSize)
            {
                step *= 1.15f;
                x = Mathf.Min(x + step, SeaHalfSize);
                positive.Add(x);
            }

            var axis = new float[positive.Count * 2 - 1];
            for (int i = 0; i < positive.Count; i++)
            {
                axis[positive.Count - 1 + i] = positive[i];
                axis[positive.Count - 1 - i] = -positive[i];
            }
            return axis;
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
