#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.EditorTools
{
    public static class BridgeSetupValidator
    {
        [MenuItem("Tools/Antigravity/Validar y Configurar Puentes y Plataformas", false, 2)]
        public static void ValidateFromMenu()
        {
            string report = ValidateAndFixBridgesAndPlatforms();
            Debug.Log($"<color=#5ce65c>[BridgeSetupValidator]</color> Resultado:\n{report}");
            EditorUtility.DisplayDialog("Validación de Puentes y Plataformas", report, "Entendido");
        }

        public static string ValidateAndFixBridges()
        {
            return ValidateAndFixBridgesAndPlatforms();
        }

        public static string ValidateAndFixBridgesAndPlatforms()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.isLoaded)
            {
                return "{\"error\":\"No hay ninguna escena activa cargada.\"}";
            }

            // Buscar todos los GameObjects en la escena activa (incluyendo inactivos)
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            List<GameObject> targetObjects = new List<GameObject>();

            foreach (var go in allObjects)
            {
                if (go.scene != activeScene) continue;
                if (EditorUtility.IsPersistent(go)) continue; // Ignorar assets de proyecto

                string lower = go.name.ToLowerInvariant();
                
                // Reconocer puentes, plataformas, suelo y geometría transitable
                bool isTarget = lower.Contains("bridge") ||
                                lower.Contains("puente") ||
                                lower.Contains("plataforma") ||
                                lower.Contains("platform") ||
                                lower.Contains("stair") ||
                                lower.Contains("escalera") ||
                                lower.Contains("level_geometry");

                if (isTarget)
                {
                    // Evitar duplicados si ya un ancestro está incluido
                    if (!targetObjects.Contains(go))
                    {
                        targetObjects.Add(go);
                    }
                }
            }

            if (targetObjects.Count == 0)
            {
                return $"No se encontraron objetos de puentes o plataformas en la escena activa '{activeScene.name}'.";
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Setup Bridge and Platform Colliders");
            int undoGroup = Undo.GetCurrentGroup();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"=== Reporte de Físicas y Colliders: '{activeScene.name}' ===");

            // Datos del jugador
            PlayerController player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            CharacterController cc = player != null ? player.GetComponent<CharacterController>() : UnityEngine.Object.FindFirstObjectByType<CharacterController>();
            float stepOffset = cc != null ? cc.stepOffset : 0.3f;
            float slopeLimit = cc != null ? cc.slopeLimit : 45.0f;

            int fixedCount = 0;
            HashSet<GameObject> processedMeshNodes = new HashSet<GameObject>();

            foreach (var rootGo in targetObjects)
            {
                if (rootGo == null) continue;

                // 1. Limpiar colliders vacíos/huérfanos en objetos sin MeshFilter
                MeshCollider[] rootMCs = rootGo.GetComponents<MeshCollider>();
                MeshFilter rootMf = rootGo.GetComponent<MeshFilter>();
                if (rootMf == null || rootMf.sharedMesh == null)
                {
                    foreach (var mc in rootMCs)
                    {
                        if (mc.sharedMesh == null)
                        {
                            Undo.DestroyObjectImmediate(mc);
                            sb.AppendLine($"  [LIMPIEZA] Se eliminó MeshCollider huérfano sin malla en '{rootGo.name}'.");
                            fixedCount++;
                        }
                    }
                }

                // 2. Procesar todos los MeshFilters del objeto y sus hijos
                MeshFilter[] meshFilters = rootGo.GetComponentsInChildren<MeshFilter>(true);
                if (meshFilters.Length == 0) continue;

                sb.AppendLine($"\nObjeto: '{rootGo.name}' ({meshFilters.Length} mallas detectadas)");

                foreach (var mf in meshFilters)
                {
                    if (mf == null || mf.sharedMesh == null) continue;
                    GameObject node = mf.gameObject;
                    if (processedMeshNodes.Contains(node)) continue;
                    processedMeshNodes.Add(node);

                    Collider col = node.GetComponent<Collider>();

                    if (col == null)
                    {
                        MeshCollider newMc = Undo.AddComponent<MeshCollider>(node);
                        newMc.sharedMesh = mf.sharedMesh;
                        newMc.convex = false;
                        newMc.isTrigger = false;
                        fixedCount++;
                        sb.AppendLine($"  -> + MeshCollider agregado a '{node.name}' (malla: {mf.sharedMesh.name}, no-trigger).");
                    }
                    else
                    {
                        if (col.isTrigger)
                        {
                            Undo.RecordObject(col, "Fix isTrigger");
                            col.isTrigger = false;
                            sb.AppendLine($"  -> Se desactivó isTrigger en '{node.name}' para soportar peso físico.");
                            fixedCount++;
                        }

                        if (col is MeshCollider mc)
                        {
                            if (mc.sharedMesh == null)
                            {
                                Undo.RecordObject(mc, "Assign Mesh");
                                mc.sharedMesh = mf.sharedMesh;
                                sb.AppendLine($"  -> Se asignó la malla faltante '{mf.sharedMesh.name}' en '{node.name}'.");
                                fixedCount++;
                            }
                        }
                    }

                    // Asegurar Layer Default para contacto con CharacterController
                    if (node.layer != 0)
                    {
                        Undo.RecordObject(node, "Set Default Layer");
                        node.layer = 0;
                        sb.AppendLine($"  -> Se corrigió layer de '{node.name}' a Default (0).");
                        fixedCount++;
                    }
                }
            }

            sb.AppendLine($"\nPlayer Controller Info:");
            sb.AppendLine($"  - CharacterController: {(cc != null ? "ACTIVO" : "NO DETECTADO")}");
            sb.AppendLine($"  - Step Offset (altura máxima escalón): {stepOffset} m");
            sb.AppendLine($"  - Slope Limit: {slopeLimit}°");

            if (fixedCount > 0)
            {
                Undo.CollapseUndoOperations(undoGroup);
                EditorSceneManager.MarkSceneDirty(activeScene);
                sb.AppendLine($"\n¡ÉXITO TOTAL! Se aplicaron {fixedCount} correcciones de colliders físicos.");
            }
            else
            {
                sb.AppendLine("\nTodas las mallas de puentes y plataformas ya cuentan con colliders físicos válidos.");
            }

            return sb.ToString();
        }
    }
}
#endif
