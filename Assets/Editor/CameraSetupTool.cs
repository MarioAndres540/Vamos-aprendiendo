#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using VamosAprendiendo.Gameplay;

namespace VamosAprendiendo.EditorTools
{
    public static class CameraSetupTool
    {
        private const string BASE_SCENE_PATH = "Assets/_Project/Scenes/BaseScene.unity";

        [MenuItem("Tools/Camera/Auto-Fix and Bind Camera to Player", false, 40)]
        public static void FixAndBindCamera()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            FixCameraInLoadedScene(activeScene);
        }

        [MenuItem("Tools/Camera/Auto-Fix Camera in BaseScene (All)", false, 41)]
        public static void FixCameraInBaseSceneDirectly()
        {
            if (System.IO.File.Exists(BASE_SCENE_PATH))
            {
                Scene openedScene = EditorSceneManager.OpenScene(BASE_SCENE_PATH, OpenSceneMode.Single);
                FixCameraInLoadedScene(openedScene);
                EditorSceneManager.SaveScene(openedScene);
                Debug.Log("<color=#5ce65c>[CameraSetupTool] BaseScene abierta, configurada y guardada con seguimiento de cámara.</color>");
            }
        }

        public static void FixCameraInLoadedScene(Scene scene)
        {
            if (!scene.isLoaded) return;

            // 1. Find Player
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null) playerObj = GameObject.Find("Player");
            if (playerObj == null)
            {
                var pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                if (pc != null) playerObj = pc.gameObject;
            }

            if (playerObj == null)
            {
                Debug.LogWarning($"[CameraSetupTool] No se encontró el objeto 'Player' en la escena '{scene.name}'.");
                return;
            }

            // Ensure Player has tag "Player"
            if (!playerObj.CompareTag("Player"))
            {
                try
                {
                    playerObj.tag = "Player";
                }
                catch { }
            }

            // 2. Find Main Camera
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = GameObject.Find("Main Camera");
                if (camObj != null) mainCam = camObj.GetComponent<Camera>();
            }

            if (mainCam == null)
            {
                Debug.LogWarning($"[CameraSetupTool] No se encontró la Main Camera en la escena '{scene.name}'.");
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Fix Camera Follow");
            int undoGroup = Undo.GetCurrentGroup();

            // Set perspective mode
            mainCam.orthographic = false;
            mainCam.fieldOfView = 60f;

            // 3. Ensure SmoothThirdPersonCamera is attached and configured
            SmoothThirdPersonCamera smoothCam = mainCam.GetComponent<SmoothThirdPersonCamera>();
            if (smoothCam == null)
            {
                smoothCam = Undo.AddComponent<SmoothThirdPersonCamera>(mainCam.gameObject);
            }
            smoothCam.SetTarget(playerObj.transform);
            smoothCam.SnapToTarget();

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"<color=#5ce65c>[CameraSetupTool] ¡Cámara vinculada exitosamente al Player en la escena '{scene.name}' con perspectiva y seguimiento suave!</color>");
        }
    }
}
#endif
