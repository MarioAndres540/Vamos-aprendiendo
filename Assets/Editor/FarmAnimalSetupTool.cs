#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VamosAprendiendo.Gameplay;

namespace VamosAprendiendo.EditorTools
{
    /// <summary>
    /// Los clips del Cartoon Animal Pack animan la posición de la raíz del modelo y, al jugar, mandaban a todos los
    /// animales al origen del mundo. Cada animal se mete en un contenedor (que conserva nombre y posición en la isla)
    /// y se le añade FarmAnimal para que descanse y salte de vez en cuando.
    /// </summary>
    public static class FarmAnimalSetupTool
    {
        private const string BASE_SCENE_PATH = "Assets/_Project/Scenes/BaseScene.unity";
        private const string PACK = "Assets/_ThirdParty/Animals/Cartoon Animal Pack 01/Prefabs/";

        [MenuItem("Tools/Scenes/Setup Farm Animals (BaseScene)", false, 37)]
        public static void Setup()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.path.Contains("BaseScene")) scene = EditorSceneManager.OpenScene(BASE_SCENE_PATH, OpenSceneMode.Single);

            var animals = new List<GameObject>();
            foreach (var an in Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var src = PrefabUtility.GetCorrespondingObjectFromSource(an.gameObject);
                if (src == null || !AssetDatabase.GetAssetPath(src).StartsWith(PACK)) continue;
                if (an.GetComponentInParent<FarmAnimal>() != null) continue; // ya preparado
                animals.Add(an.gameObject);
            }

            int count = 0;
            foreach (var model in animals)
            {
                var animator = model.GetComponent<Animator>();
                Transform oldParent = model.transform.parent;
                int sibling = model.transform.GetSiblingIndex();
                string animalName = model.name;

                Vector3 origin = model.transform.position;
                Quaternion rot = model.transform.rotation;
                Vector3 scale = model.transform.lossyScale;
                Vector3 clipOffset = Vector3.zero;
                bool hasIdle = false;
                var ctrl = animator.runtimeAnimatorController;
                if (ctrl != null && ctrl.animationClips.Length > 0)
                {
                    AnimationClip first = ctrl.animationClips[0];
                    foreach (var clip in ctrl.animationClips)
                    {
                        if (clip.name.StartsWith("Idle")) { hasIdle = true; first = clip; }
                    }

                    // Dónde coloca el clip al modelo en su primer cuadro: se compensa moviendo el contenedor.
                    var savedPos = model.transform.localPosition;
                    var savedRot = model.transform.localRotation;
                    first.SampleAnimation(model, 0f);
                    clipOffset = model.transform.localPosition;
                    model.transform.localPosition = savedPos;
                    model.transform.localRotation = savedRot;
                }

                var container = new GameObject(animalName);
                Undo.RegisterCreatedObjectUndo(container, "Animal");
                container.transform.SetParent(oldParent, false);
                container.transform.SetSiblingIndex(sibling);
                container.transform.rotation = rot;
                container.transform.localScale = Vector3.one;
                Vector3 planar = new Vector3(clipOffset.x, 0f, clipOffset.z);
                container.transform.position = origin - rot * Vector3.Scale(planar, scale);

                Undo.SetTransformParent(model.transform, container.transform, "Animal");
                model.name = "Model";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;

                var fa = container.AddComponent<FarmAnimal>();
                fa.Configure(animator, hasIdle);
                count++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[FarmAnimals] {count} animales preparados.");
        }
    }
}
#endif
