using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VamosAprendiendo.Gameplay;

namespace VamosAprendiendo.EditorTools
{
    /// <summary>
    /// Arma en BaseScene el paso del nivel 1 al nivel 2: el portal azul (cerrado hasta completar el nivel), la plataforma verde
    /// (bloqueada hasta cruzar el portal) y el regreso del personaje al frente del pulpo con la celebración. Se puede repetir.
    /// También trae el menú Tools/Level para probar sin tener que completar las 64 actividades.
    /// </summary>
    public static class LevelSystemSetupTool
    {
        private const string BaseScenePath = "Assets/_Project/Scenes/BaseScene.unity";
        private const string FxDir = "Assets/_ThirdParty/VFX/Hovl Studio/Magic effects pack/Prefabs/";

        [MenuItem("Tools/Level/Setup Level System (BaseScene)")]
        public static void Setup()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.path.Contains("BaseScene")) scene = EditorSceneManager.OpenScene(BaseScenePath, OpenSceneMode.Single);

            var portalFx = GameObject.Find("Portal blue");
            var portalsGroup = GameObject.Find("Portals");
            var lift = FindLift("Spiral_Lift_1");
            var octopus = GameObject.Find("Octopus_Rigged_Unity");
            if (portalFx == null || portalsGroup == null || lift == null || octopus == null)
            {
                Debug.LogError("[Level] Faltan objetos en BaseScene (Portal blue, Portals, Spiral_Lift_1 u Octopus_Rigged_Unity).");
                return;
            }

            // --- Portal del nivel: el trigger va aparte (hermano) para que siga activo cuando el portal está apagado ---
            DestroyNamed("Portal_Level_Trigger");
            var triggerGo = new GameObject("Portal_Level_Trigger");
            Undo.RegisterCreatedObjectUndo(triggerGo, "Level portal trigger");
            triggerGo.transform.SetParent(portalsGroup.transform, true);
            triggerGo.transform.position = portalFx.transform.position + new Vector3(0f, -0.1f, -0.1f);
            var box = triggerGo.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(3.6f, 4.2f, 1.8f);
            var portal = triggerGo.AddComponent<LevelPortal>();

            // Lo que se apaga mientras el portal está cerrado: el aro mágico y los brillos que lo acompañan
            var openOnly = new List<GameObject> { portalFx };
            foreach (var glow in Object.FindObjectsByType<PortalGlow>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (Vector3.Distance(glow.transform.position, portalFx.transform.position) < 8f) openOnly.Add(glow.gameObject);
            var portalSo = new SerializedObject(portal);
            var list = portalSo.FindProperty("openOnly");
            list.arraySize = openOnly.Count;
            for (int i = 0; i < openOnly.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = openOnly[i];
            portalSo.FindProperty("openedBurstPrefab").objectReferenceValue = LoadFx("Hits and explosions/Holy hit.prefab");
            portalSo.ApplyModifiedProperties();

            // --- Frente al pulpo ---
            DestroyNamed("Octopus_Front");
            var front = new GameObject("Octopus_Front");
            Undo.RegisterCreatedObjectUndo(front, "Octopus front");
            front.transform.SetParent(octopus.transform.parent, true);
            front.transform.position = FindFrontPoint(octopus.transform);

            // --- Secuencia de nivel ---
            DestroyNamed("LevelSystem");
            var systemGo = new GameObject("LevelSystem");
            Undo.RegisterCreatedObjectUndo(systemGo, "Level system");
            var group = GameObject.Find("--- [GAMEPLAY] ---");
            if (group != null) systemGo.transform.SetParent(group.transform, false);
            var sequence = systemGo.AddComponent<LevelUpSequence>();
            var so = new SerializedObject(sequence);
            so.FindProperty("lift").objectReferenceValue = lift;
            so.FindProperty("octopusFront").objectReferenceValue = front.transform;
            so.FindProperty("lookAt").objectReferenceValue = octopus.transform;
            var fx = so.FindProperty("celebrationPrefabs");
            string[] celebration = { "Magic circles/Magic circle 2.prefab", "Sparks/Sparks explode yellow.prefab", "Hits and explosions/Holy hit.prefab" };
            fx.arraySize = celebration.Length;
            for (int i = 0; i < celebration.Length; i++) fx.GetArrayElementAtIndex(i).objectReferenceValue = LoadFx(celebration[i]);
            so.FindProperty("platformAuraPrefab").objectReferenceValue = LoadFx("Magic circles/Healing circle.prefab");
            so.FindProperty("platformBurstPrefab").objectReferenceValue = LoadFx("Sparks/Sparks explode yellow.prefab");
            so.ApplyModifiedProperties();

            var material = BuildPlatformMaterial();
            if (material != null)
                foreach (var renderer in lift.GetComponentsInChildren<Renderer>(true))
                {
                    Undo.RecordObject(renderer, "Level platform material");
                    var shared = new Material[renderer.sharedMaterials.Length];
                    for (int i = 0; i < shared.Length; i++) shared[i] = material;
                    renderer.sharedMaterials = shared;
                }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Level] Listo. Portal cerrado hasta completar el nivel 1; plataforma bloqueada hasta cruzarlo; regreso al frente del pulpo en {front.transform.position:F1}.");
        }

        private const string PlatformMaterialPath = "Assets/_Project/Art/Materials/M_LevelPlatform.mat";
        private const string GrassMudDir = "Assets/_ThirdParty/Textures/Blink/Art/Textures/StylizedForestTextures/Grass_Mud/";

        // Material de la plataforma: URP/Lit con las texturas de césped y tierra (color, relieve y oclusión) y un brillo verde suave,
        // en lugar del material plano sin luz que tenía.
        private static Material BuildPlatformMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) { Debug.LogWarning("[Level] No se encontró el shader URP/Lit."); return null; }

            var material = AssetDatabase.LoadAssetAtPath<Material>(PlatformMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, PlatformMaterialPath);
            }
            material.shader = shader;

            var baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(GrassMudDir + "Grass_Mud_Basecolor.png");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(GrassMudDir + "Grass_Mud_Normal.png");
            var occlusion = AssetDatabase.LoadAssetAtPath<Texture2D>(GrassMudDir + "Grass_Mud_Ambientocclusion.png");
            material.SetTexture("_BaseMap", baseMap);
            material.SetColor("_BaseColor", new Color(0.82f, 1f, 0.78f));
            if (normal != null) { material.SetTexture("_BumpMap", normal); material.SetFloat("_BumpScale", 1f); material.EnableKeyword("_NORMALMAP"); }
            if (occlusion != null) { material.SetTexture("_OcclusionMap", occlusion); material.EnableKeyword("_OCCLUSIONMAP"); }
            material.SetFloat("_Smoothness", 0.35f);
            material.SetFloat("_Metallic", 0f);
            material.SetTextureScale("_BaseMap", new Vector2(2f, 2f));
            material.SetTextureScale("_BumpMap", new Vector2(2f, 2f));
            material.SetTextureScale("_OcclusionMap", new Vector2(2f, 2f));
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            material.SetColor("_EmissionColor", new Color(0.15f, 0.9f, 0.35f) * 0.6f);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        // Punto firme frente al pulpo: se prueba alrededor de él (primero hacia donde mira) y se queda con el primero con suelo cerca
        private static Vector3 FindFrontPoint(Transform octopus)
        {
            Vector3 forward = octopus.forward; forward.y = 0f; forward.Normalize();
            Vector3[] directions = { forward, Quaternion.Euler(0f, 45f, 0f) * forward, Quaternion.Euler(0f, -45f, 0f) * forward,
                                     Quaternion.Euler(0f, 90f, 0f) * forward, Quaternion.Euler(0f, -90f, 0f) * forward, -forward };
            foreach (float distance in new[] { 5f, 4f, 6f, 3f })
                foreach (var dir in directions)
                {
                    Vector3 candidate = octopus.position + dir * distance;
                    if (Physics.Raycast(candidate + Vector3.up * 8f, Vector3.down, out var hit, 20f)
                        && Mathf.Abs(hit.point.y - octopus.position.y) < 4f && hit.normal.y > 0.7f)
                        return hit.point + Vector3.up * 0.15f;
                }
            Debug.LogWarning("[Level] No se encontró suelo firme frente al pulpo: se usa su posición; ajusta 'Octopus_Front' a mano.");
            return octopus.position;
        }

        private static SpiralLift FindLift(string name)
        {
            foreach (var lift in Object.FindObjectsByType<SpiralLift>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (lift.name == name) return lift;
            return null;
        }

        private static void DestroyNamed(string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null) Object.DestroyImmediate(existing);
        }

        private static GameObject LoadFx(string relativePath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FxDir + relativePath);
            if (prefab == null) Debug.LogWarning($"[Level] No se encontró el efecto {relativePath}");
            return prefab;
        }

        // ---------- pruebas ----------

        private const string DebugMenu = "Tools/Level/Tratar el nivel 1 como completo (prueba)";

        [MenuItem(DebugMenu, false, 20)]
        private static void ToggleDebugUnlock()
        {
            PlayerLevel.DebugUnlock = !PlayerLevel.DebugUnlock;
            Debug.Log(PlayerLevel.DebugUnlock
                ? "[Level] Prueba: el portal se abre como si el nivel 1 estuviera completo (el progreso real no cambia)."
                : "[Level] Prueba desactivada: el portal vuelve a depender del progreso real.");
        }

        [MenuItem(DebugMenu, true)]
        private static bool ToggleDebugUnlockValidate()
        {
            Menu.SetChecked(DebugMenu, PlayerLevel.DebugUnlock);
            return true;
        }

        [MenuItem("Tools/Level/Reiniciar el nivel (volver al nivel 1)", false, 21)]
        private static void ResetLevel()
        {
            PlayerLevel.Reset();
            Debug.Log("[Level] Nivel reiniciado: nivel 1 y portal sin cruzar (para la cuenta actual de este dispositivo).");
        }
    }
}
