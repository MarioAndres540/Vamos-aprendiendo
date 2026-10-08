#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VamosAprendiendo.Gameplay;

namespace VamosAprendiendo.EditorTools
{
    public static class PlayerSetupTool
    {
        private const string BASE_SCENE_PATH = "Assets/_Project/Scenes/BaseScene.unity";

        [MenuItem("Tools/Gameplay/Add Fall Respawn to Player", false, 30)]
        public static void AddFallRespawn()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.path.Contains("BaseScene"))
            {
                scene = EditorSceneManager.OpenScene(BASE_SCENE_PATH, OpenSceneMode.Single);
            }

            var player = Object.FindFirstObjectByType<PlayerController>();
            if (player == null)
            {
                Debug.LogError("[PlayerSetup] No se encontró el jugador en BaseScene.");
                return;
            }

            if (player.GetComponent<FallRespawn>() == null)
            {
                Undo.AddComponent<FallRespawn>(player.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[PlayerSetup] FallRespawn añadido al jugador.");
        }
    }
}
#endif
