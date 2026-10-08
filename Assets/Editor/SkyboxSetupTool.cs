#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using VamosAprendiendo.Gameplay;

namespace VamosAprendiendo.EditorTools
{
    /// <summary>Configura el cielo animado de BaseScene con Fantasy Skybox FREE (panorámico FS003).</summary>
    public static class SkyboxSetupTool
    {
        private const string BASE_SCENE_PATH = "Assets/_Project/Scenes/BaseScene.unity";
        private const string SKY_DIR = "Assets/Fantasy Skybox FREE/Panoramics/FS003";

        [MenuItem("Tools/Scenes/Setup Animated Skybox (BaseScene)", false, 23)]
        public static void Setup()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.path.Contains("BaseScene"))
            {
                scene = EditorSceneManager.OpenScene(BASE_SCENE_PATH, OpenSceneMode.Single);
            }

            var day = Load("FS003_Day");
            var sunrise = Load("FS003_Sunrise");
            var sunset = Load("FS003_Sunset");
            var night = Load("FS003_Night");
            if (day == null)
            {
                Debug.LogError("[SkyboxSetup] No se encontró FS003_Day en Fantasy Skybox FREE.");
                return;
            }

            // Cielo base y luz ambiente tomada del cielo.
            RenderSettings.skybox = day;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            DynamicGI.UpdateEnvironment();

            var cam = Camera.main;
            if (cam != null) cam.clearFlags = CameraClearFlags.Skybox;

            Light sun = RenderSettings.sun;
            if (sun == null)
            {
                foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                {
                    if (l.type == LightType.Directional) { sun = l; break; }
                }
            }

            var existing = Object.FindFirstObjectByType<SkyboxAnimator>();
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var go = new GameObject("SkyboxAnimator");
            Undo.RegisterCreatedObjectUndo(go, "Create Skybox Animator");
            var lighting = GameObject.Find("--- [LIGHTING & VOLUME] ---");
            if (lighting != null) go.transform.SetParent(lighting.transform, false);

            var animator = go.AddComponent<SkyboxAnimator>();
            var so = new SerializedObject(animator);
            so.FindProperty("skybox").objectReferenceValue = day;
            so.FindProperty("sun").objectReferenceValue = sun;
            so.FindProperty("rotationSpeed").floatValue = 1.5f;
            so.FindProperty("enableDayCycle").boolValue = false;

            Color dayColor = sun != null ? sun.color : new Color(1f, 0.97f, 0.92f);
            float dayIntensity = sun != null ? sun.intensity : 1.2f;
            var phases = so.FindProperty("phases");
            phases.arraySize = 4;
            SetPhase(phases.GetArrayElementAtIndex(0), "Día", day, dayColor, dayIntensity, 90f);
            SetPhase(phases.GetArrayElementAtIndex(1), "Atardecer", sunset, new Color(1f, 0.62f, 0.42f), dayIntensity * 0.75f, 45f);
            SetPhase(phases.GetArrayElementAtIndex(2), "Noche", night, new Color(0.55f, 0.65f, 1f), dayIntensity * 0.3f, 45f);
            SetPhase(phases.GetArrayElementAtIndex(3), "Amanecer", sunrise, new Color(1f, 0.82f, 0.62f), dayIntensity * 0.8f, 45f);
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SkyboxSetup] Cielo animado configurado en BaseScene (FS003_Day, ciclo de día desactivado).");
        }

        private static Material Load(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Material>($"{SKY_DIR}/{name}.mat");
        }

        private static void SetPhase(SerializedProperty p, string name, Material sky, Color sunColor, float intensity, float hold)
        {
            p.FindPropertyRelative("name").stringValue = name;
            p.FindPropertyRelative("skybox").objectReferenceValue = sky;
            p.FindPropertyRelative("sunColor").colorValue = sunColor;
            p.FindPropertyRelative("sunIntensity").floatValue = intensity;
            p.FindPropertyRelative("holdSeconds").floatValue = hold;
        }
    }
}
#endif
