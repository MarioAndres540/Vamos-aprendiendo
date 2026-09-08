#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace VamosAprendiendo.EditorTools
{
    public class SceneEnvironmentScalerWindow : EditorWindow
    {
        private float scaleFactor = 1.5f;

        [MenuItem("Tools/Environment/Scale Environment Tool", false, 30)]
        public static void ShowWindow()
        {
            GetWindow<SceneEnvironmentScalerWindow>("Environment Scaler");
        }

        [MenuItem("Tools/Environment/Scale Environment x1.5", false, 31)]
        public static void QuickScale15()
        {
            ApplyScaleToEnvironment(1.5f);
        }

        [MenuItem("Tools/Environment/Reset Environment Scale (x1.0)", false, 32)]
        public static void ResetScale()
        {
            ApplyScaleToEnvironment(1.0f);
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Escalar Entorno de la Escena Activa", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Escala todos los objetos de la escena (plataformas, vegetación, templos, props) sin afectar al Player, cámaras ni luces.", MessageType.Info);

            GUILayout.Space(10);
            scaleFactor = EditorGUILayout.Slider("Factor de Escala", scaleFactor, 0.5f, 3.0f);

            GUILayout.Space(15);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Aplicar Escala 1.5x", GUILayout.Height(32)))
            {
                ApplyScaleToEnvironment(1.5f);
            }

            if (GUILayout.Button("Aplicar Escala Seleccionada", GUILayout.Height(32)))
            {
                ApplyScaleToEnvironment(scaleFactor);
            }

            if (GUILayout.Button("Resetear (1.0x)", GUILayout.Height(32)))
            {
                ApplyScaleToEnvironment(1.0f);
            }

            EditorGUILayout.EndHorizontal();
        }

        public static void ApplyScaleToEnvironment(float factor)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.isLoaded) return;

            GameObject envRoot = null;
            foreach (GameObject root in activeScene.GetRootGameObjects())
            {
                if (root.name == "--- [ENVIRONMENT] ---" || root.name.ToLower().Contains("environment"))
                {
                    envRoot = root;
                    break;
                }
            }

            if (envRoot != null)
            {
                Undo.RecordObject(envRoot.transform, "Scale Environment");
                envRoot.transform.localScale = new Vector3(factor, factor, factor);
                EditorSceneManager.MarkSceneDirty(activeScene);
                Debug.Log($"<color=#5ce65c>[EnvironmentScaler] Entorno escalado a {factor}x exitosamente.</color>");
            }
            else
            {
                Debug.LogWarning("[EnvironmentScaler] No se encontró el contenedor '--- [ENVIRONMENT] ---' en la escena activa.");
            }
        }
    }
}
#endif
