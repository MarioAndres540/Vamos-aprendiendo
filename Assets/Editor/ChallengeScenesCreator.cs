#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace VamosAprendiendo.EditorTools
{
    public static class ChallengeScenesCreator
    {
        private const string CHALLENGES_DIR = "Assets/_Project/Scenes/Challenges";

        private static readonly (string sceneName, bool is2D)[] ScenesToCreate = new[]
        {
            ("MathOperation_2D", true),
            ("NumberTrace_2D", true),
            ("PlatformJump_25D", false),
            ("Maze_3D", false),
            ("Memory_3D", false)
        };

        [MenuItem("Tools/Scenes/Create All Challenge Scenes", false, 20)]
        public static void CreateAllChallengeScenes()
        {
            if (!Directory.Exists(CHALLENGES_DIR))
            {
                Directory.CreateDirectory(CHALLENGES_DIR);
                AssetDatabase.Refresh();
            }

            Scene originalActiveScene = SceneManager.GetActiveScene();
            string originalScenePath = originalActiveScene.path;

            List<string> createdPaths = new List<string>();

            foreach (var (sceneName, is2D) in ScenesToCreate)
            {
                string scenePath = $"{CHALLENGES_DIR}/{sceneName}.unity";

                // If scene already exists, don't overwrite if it has content, but ensure it's in build settings
                if (File.Exists(scenePath))
                {
                    Debug.Log($"[ChallengeScenesCreator] La escena '{sceneName}' ya existe en '{scenePath}'.");
                    createdPaths.Add(scenePath);
                    continue;
                }

                // Create new scene
                Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                // Setup Hierarchy Containers
                GameObject sysHeader = new GameObject("--- [CAMERAS & SYSTEMS] ---");
                GameObject envHeader = new GameObject("--- [ENVIRONMENT] ---");
                GameObject gameplayHeader = new GameObject("--- [GAMEPLAY] ---");
                GameObject uiHeader = new GameObject("--- [UI] ---");

                // Main Camera
                GameObject camObj = new GameObject("Main Camera");
                camObj.transform.SetParent(sysHeader.transform, false);
                Camera cam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();

                if (is2D)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0.92f, 0.95f, 0.99f, 1f);
                    cam.orthographic = true;
                    cam.orthographicSize = 5f;
                    camObj.transform.position = new Vector3(0, 0, -10);
                }
                else
                {
                    cam.clearFlags = CameraClearFlags.Skybox;
                    cam.orthographic = false;
                    cam.fieldOfView = 60f;
                    camObj.transform.position = new Vector3(0, 3, -6);
                    camObj.transform.rotation = Quaternion.Euler(20, 0, 0);

                    // Directional Light
                    GameObject lightHeader = new GameObject("--- [LIGHTING & VOLUME] ---");
                    GameObject lightObj = new GameObject("Directional Light");
                    lightObj.transform.SetParent(lightHeader.transform, false);
                    Light light = lightObj.AddComponent<Light>();
                    light.type = LightType.Directional;
                    light.intensity = 1.2f;
                    light.color = new Color(1f, 0.97f, 0.92f);
                    lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
                }

                // EventSystem
                GameObject eventSysObj = new GameObject("EventSystem");
                eventSysObj.transform.SetParent(sysHeader.transform, false);
                eventSysObj.AddComponent<UnityEngine.EventSystems.EventSystem>();

                Type inputSystemModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputSystemModuleType != null)
                {
                    eventSysObj.AddComponent(inputSystemModuleType);
                }
                else
                {
                    eventSysObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }

                // Scene Title / Placeholder in UI or Root
                GameObject levelRoot = new GameObject("Level_Root");
                levelRoot.transform.SetParent(envHeader.transform, false);

                // Save Scene
                EditorSceneManager.SaveScene(newScene, scenePath);
                createdPaths.Add(scenePath);
                Debug.Log($"<color=#5ce65c>[ChallengeScenesCreator] Creada escena: {scenePath}</color>");
            }

            // Update Build Settings with all project scenes
            UpdateBuildSettings();

            // Reopen original scene if it was valid
            if (!string.IsNullOrEmpty(originalScenePath) && File.Exists(originalScenePath))
            {
                EditorSceneManager.OpenScene(originalScenePath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#5ce65c>[ChallengeScenesCreator] ¡Todas las escenas de desafíos fueron creadas e indexadas en Build Settings!</color>");
        }

        private static void UpdateBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();

            // 0. LoginScene
            string loginPath = "Assets/_Project/Scenes/LoginScene.unity";
            if (File.Exists(loginPath)) scenes.Add(new EditorBuildSettingsScene(loginPath, true));

            // 1. BaseScene
            string basePath = "Assets/_Project/Scenes/BaseScene.unity";
            if (File.Exists(basePath)) scenes.Add(new EditorBuildSettingsScene(basePath, true));

            // 2. Challenge scenes
            foreach (var (sceneName, _) in ScenesToCreate)
            {
                string path = $"{CHALLENGES_DIR}/{sceneName}.unity";
                if (File.Exists(path))
                {
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
