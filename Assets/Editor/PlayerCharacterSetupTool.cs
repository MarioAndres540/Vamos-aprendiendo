#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VamosAprendiendo.Gameplay;

namespace VamosAprendiendo.EditorTools
{
    /// <summary>
    /// Sustituye la cápsula del jugador por el prefab Encapuchado (como hijo de Player, que conserva el
    /// CharacterController, el PlayerInput y los scripts) y le añade PlayerAnimatorDriver.
    /// </summary>
    public static class PlayerCharacterSetupTool
    {
        private const string BASE_SCENE_PATH = "Assets/_Project/Scenes/BaseScene.unity";
        private const string PREFAB_PATH = "Assets/_Project/Art/Models/Characters/Encapuchado/Encapuchado.prefab";
        private const string CHILD_NAME = "Encapuchado";
        private const string VISUAL_NAME = "Visual";
        private const float VisualScale = 1.25f; // 1,6 m del modelo -> 2 m del CharacterController

        [MenuItem("Tools/Gameplay/Replace Player With Encapuchado", false, 35)]
        public static void Replace()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.path.Contains("BaseScene"))
            {
                scene = EditorSceneManager.OpenScene(BASE_SCENE_PATH, OpenSceneMode.Single);
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            var player = Object.FindFirstObjectByType<PlayerController>();
            if (prefab == null || player == null)
            {
                Debug.LogError("[PlayerCharacter] Falta el prefab Encapuchado o el Player en BaseScene.");
                return;
            }

            var go = player.gameObject;
            var cc = go.GetComponent<CharacterController>();

            // La cápsula visible ya no se usa (el CharacterController sigue siendo el colisionador).
            var meshRenderer = go.GetComponent<MeshRenderer>();
            var meshFilter = go.GetComponent<MeshFilter>();
            if (meshRenderer != null) Undo.DestroyObjectImmediate(meshRenderer);
            if (meshFilter != null) Undo.DestroyObjectImmediate(meshFilter);

            foreach (var oldName in new[] { VISUAL_NAME, CHILD_NAME })
            {
                var old = go.transform.Find(oldName);
                if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
            }

            // Contenedor "Visual": lleva la posición (pies en la base de la cápsula), la escala y el giro de 180°.
            // Los clips de animación sobrescriben la posición, rotación y escala de la raíz del modelo, así que esos
            // ajustes NO pueden ir en el propio modelo: se perderían en cuanto el Animator empiece a reproducir.
            var visual = new GameObject(VISUAL_NAME);
            Undo.RegisterCreatedObjectUndo(visual, "Encapuchado");
            visual.transform.SetParent(go.transform, false);
            float feetY = cc.center.y - cc.height * 0.5f;
            visual.transform.localPosition = new Vector3(0f, feetY, 0f);
            visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // las animaciones miran hacia -Z
            visual.transform.localScale = Vector3.one * VisualScale;

            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, visual.transform);
            model.name = CHILD_NAME;
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = prefab.transform.rotation;
            model.transform.localScale = Vector3.one;

            var animator = model.GetComponent<Animator>();
            if (animator != null) animator.applyRootMotion = false;
            var driver = go.GetComponent<PlayerAnimatorDriver>();
            if (driver == null) driver = Undo.AddComponent<PlayerAnimatorDriver>(go);
            driver.SetAnimator(animator);
            EditorUtility.SetDirty(driver);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[PlayerCharacter] Player ahora usa Encapuchado (escala {VisualScale}, pies en y={feetY}).");
        }
    }
}
#endif
