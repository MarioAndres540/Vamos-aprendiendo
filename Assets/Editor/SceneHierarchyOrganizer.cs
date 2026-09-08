#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Project.EditorTools
{
    public static class SceneHierarchyOrganizer
    {
        [MenuItem("Tools/Hierarchy/Organize Current Scene Hierarchy", false, 1)]
        public static void OrganizeActiveScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.isLoaded)
            {
                Debug.LogError("[HierarchyOrganizer] No hay ninguna escena activa cargada.");
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Organize Scene Hierarchy");
            int undoGroup = Undo.GetCurrentGroup();

            // 1. Get or create root category folders (Headers)
            GameObject sysHeader = GetOrCreateFolder("--- [CAMERAS & SYSTEMS] ---", null);
            GameObject lightHeader = GetOrCreateFolder("--- [LIGHTING & VOLUME] ---", null);
            GameObject gameplayHeader = GetOrCreateFolder("--- [GAMEPLAY] ---", null);
            GameObject envHeader = GetOrCreateFolder("--- [ENVIRONMENT] ---", null);

            // 2. Sub-folders inside ENVIRONMENT
            GameObject geoFolder = GetOrCreateFolder("Level_Geometry", envHeader.transform);
            GameObject natureFolder = GetOrCreateFolder("Nature", envHeader.transform);
            GameObject propsFolder = GetOrCreateFolder("Props_Decoration", envHeader.transform);

            // 3. Sub-props folders
            GameObject eagleFolder = GetOrCreateFolder("Eagle_Statues", propsFolder.transform);
            GameObject lightingPropsFolder = GetOrCreateFolder("Lighting_Props", propsFolder.transform);
            GameObject portalsFolder = GetOrCreateFolder("Portals", propsFolder.transform);

            // Fetch root objects
            GameObject[] rootObjects = activeScene.GetRootGameObjects();

            foreach (GameObject go in rootObjects)
            {
                if (go == null) continue;
                string goName = go.name.Trim();

                // Skip category header roots
                if (goName.StartsWith("--- [") && goName.EndsWith("] ---"))
                    continue;

                // Move matching objects
                Transform targetParent = DetermineTargetParent(
                    goName,
                    sysHeader.transform,
                    lightHeader.transform,
                    gameplayHeader.transform,
                    geoFolder.transform,
                    natureFolder.transform,
                    eagleFolder.transform,
                    lightingPropsFolder.transform,
                    portalsFolder.transform,
                    propsFolder.transform
                );

                if (targetParent != null && go.transform.parent != targetParent)
                {
                    Undo.SetTransformParent(go.transform, targetParent, "Parent " + goName);
                }
            }

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log($"<color=#5ce65c>[HierarchyOrganizer] Jerarquía organizada exitosamente en la escena '{activeScene.name}'.</color>");
        }

        private static Transform DetermineTargetParent(
            string name,
            Transform sys,
            Transform light,
            Transform gameplay,
            Transform geo,
            Transform nature,
            Transform eagle,
            Transform lightProps,
            Transform portals,
            Transform generalProps
        )
        {
            string lower = name.ToLowerInvariant();

            // Cameras & Systems
            if (lower.Contains("cinemachine") || lower.Contains("camera") || lower.Contains("eventsystem") || lower.Contains("gamemanager") || lower.Contains("ui") || lower.Contains("canvas"))
            {
                return sys;
            }

            // Lighting & Volume
            if (lower.Contains("directional light") || lower.Contains("global volume") || lower.Contains("point light") || lower.Contains("spot light") || lower.Contains("volume") || lower.Contains("reflection probe"))
            {
                return light;
            }

            // Gameplay
            if (lower.Contains("player") || lower.Contains("enemy") || lower.Contains("character") || lower.Contains("spawn") || lower.Contains("checkpoint"))
            {
                return gameplay;
            }

            // Statues
            if (lower.Contains("eagle") || lower.Contains("statue"))
            {
                return eagle;
            }

            // Lighting Props / Lanterns
            if (lower.Contains("lantern") || lower.Contains("lamppost") || lower.Contains("lamp") || lower.Contains("torch") || lower.Contains("light_prop"))
            {
                return lightProps;
            }

            // Portals
            if (lower.Contains("portal") || lower.Contains("rune portal"))
            {
                return portals;
            }

            // Geometry / Platforms / Temples / Bridges
            if (lower.Contains("plataforma") || lower.Contains("platform") || lower.Contains("puente") || lower.Contains("bridge") || lower.Contains("temple") || lower.Contains("floating temple") || lower.Contains("stair") || lower.Contains("suelo") || lower.Contains("ground") || lower.Contains("wall") || lower.Contains("floor"))
            {
                return geo;
            }

            // Nature / Foliage / Trees
            if (lower.Contains("arbol") || lower.Contains("tree") || lower.Contains("diorama") || lower.Contains("bush") || lower.Contains("grass") || lower.Contains("rock") || lower.Contains("flower") || lower.Contains("nature"))
            {
                return nature;
            }

            // Default Props
            return generalProps;
        }

        private static GameObject GetOrCreateFolder(string name, Transform parent)
        {
            GameObject folderObj = null;

            if (parent == null)
            {
                Scene activeScene = SceneManager.GetActiveScene();
                foreach (GameObject root in activeScene.GetRootGameObjects())
                {
                    if (root.name == name)
                    {
                        folderObj = root;
                        break;
                    }
                }
            }
            else
            {
                Transform child = parent.Find(name);
                if (child != null)
                {
                    folderObj = child.gameObject;
                }
            }

            if (folderObj == null)
            {
                folderObj = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(folderObj, "Create " + name);

                if (parent != null)
                {
                    folderObj.transform.SetParent(parent, false);
                }

                folderObj.transform.localPosition = Vector3.zero;
                folderObj.transform.localRotation = Quaternion.identity;
                folderObj.transform.localScale = Vector3.one;
            }

            return folderObj;
        }
    }
}
#endif
