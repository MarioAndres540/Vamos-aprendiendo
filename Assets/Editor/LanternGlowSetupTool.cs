#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Project.Gameplay.Environment;

namespace Project.EditorTools
{
    public static class LanternGlowSetupTool
    {
        private const string MaterialPath = "Assets/_Project/Art/Materials/M_Glowing_japanis.mat";
        private const string EmissionTexturePath = "Assets/_Project/Art/Textures/Glowing_Japanese_Lantern_emission.png";
        private const string NormalTexturePath = "Assets/_Project/Art/Textures/Glowing_Japanese_Lantern_normal.png";
        private const string BaseTexturePath = "Assets/_Project/Art/Textures/Glowing_Japanese_Lantern_baseColor.png";

        private static readonly Color GlowCyanColor = new Color(0.28f, 0.92f, 0.96f, 1f);

        [MenuItem("Tools/Environment/Setup Japanese Lantern Glow & Lights", false, 10)]
        public static void SetupJapaneseLanterns()
        {
            Debug.Log("<color=#00e6e6><b>[LanternSetup] Configurando iluminación para Glowing Japanese Lantern...</b></color>");

            // 1. Refrescar y configurar Material URP
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat != null)
            {
                Texture2D baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(BaseTexturePath);
                Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalTexturePath);
                Texture2D emissionTex = AssetDatabase.LoadAssetAtPath<Texture2D>(EmissionTexturePath);

                if (baseTex != null) mat.SetTexture("_BaseMap", baseTex);
                if (normalTex != null) mat.SetTexture("_BumpMap", normalTex);
                if (emissionTex != null) mat.SetTexture("_EmissionMap", emissionTex);

                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                mat.SetColor("_EmissionColor", GlowCyanColor * 2.5f);
                mat.SetFloat("_Smoothness", 0.15f);

                EditorUtility.SetDirty(mat);
            }

            // 2. Configurar linternas en BaseScene
            SetupLanternsInBaseScene();

            AssetDatabase.SaveAssets();
            Debug.Log("<color=#00ff88><b>[LanternSetup] ¡Glowing Japanese Lanterns configuradas con éxito!</b></color>");
        }

        private static void SetupLanternsInBaseScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            string baseScenePath = "Assets/_Project/Scenes/BaseScene.unity";

            if (!activeScene.isLoaded || !activeScene.path.Contains("BaseScene"))
            {
                if (File.Exists(baseScenePath))
                {
                    activeScene = EditorSceneManager.OpenScene(baseScenePath, OpenSceneMode.Single);
                }
            }

            if (!activeScene.isLoaded)
            {
                Debug.LogError("[LanternSetup] No se pudo abrir BaseScene.");
                return;
            }

            GameObject[] rootObjects = activeScene.GetRootGameObjects();
            int lanternCount = 0;

            foreach (var root in rootObjects)
            {
                ProcessTransformRecursive(root.transform, ref lanternCount);
            }

            if (lanternCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
                Debug.Log($"[LanternSetup] Se configuraron {lanternCount} lámparas japonesas en la escena '{activeScene.name}'.");
            }
            else
            {
                Debug.LogWarning("[LanternSetup] No se encontraron objetos 'Glowing Japanese Lantern' en la escena activa.");
            }
        }

        private static void ProcessTransformRecursive(Transform parent, ref int count)
        {
            if (parent.name.StartsWith("Glowing Japanese Lantern"))
            {
                SetupSingleLantern(parent.gameObject);
                count++;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                ProcessTransformRecursive(parent.GetChild(i), ref count);
            }
        }

        private static void SetupSingleLantern(GameObject lanternGo)
        {
            Undo.RegisterCompleteObjectUndo(lanternGo, "Setup Lantern Glow");

            // 1. Agregar o actualizar LanternGlow component
            LanternGlow glow = lanternGo.GetComponent<LanternGlow>();
            if (glow == null)
            {
                glow = Undo.AddComponent<LanternGlow>(lanternGo);
            }

            // 2. Buscar o crear hijo con Point Light
            Transform lightTransform = lanternGo.transform.Find("Lantern_PointLight");
            GameObject lightGo;

            if (lightTransform == null)
            {
                lightGo = new GameObject("Lantern_PointLight");
                Undo.RegisterCreatedObjectUndo(lightGo, "Create Lantern Light");
                lightGo.transform.SetParent(lanternGo.transform, false);
            }
            else
            {
                lightGo = lightTransform.gameObject;
            }

            // Posicionar la luz en la cámara de la vela / ventana interior
            MeshRenderer mr = lanternGo.GetComponentInChildren<MeshRenderer>();
            Vector3 localLightPos = new Vector3(0f, 0.95f, 0f);

            if (mr != null)
            {
                Bounds localBounds = mr.localBounds;
                localLightPos = new Vector3(localBounds.center.x, localBounds.center.y * 1.15f, localBounds.center.z);
            }

            lightGo.transform.localPosition = localLightPos;

            // Configurar componente Light
            Light ptLight = lightGo.GetComponent<Light>();
            if (ptLight == null)
            {
                ptLight = Undo.AddComponent<Light>(lightGo);
            }

            ptLight.type = LightType.Point;
            ptLight.color = GlowCyanColor;
            ptLight.intensity = 2.8f;
            ptLight.range = 5.5f;
            ptLight.shadows = LightShadows.Soft;

            EditorUtility.SetDirty(lanternGo);
            EditorUtility.SetDirty(lightGo);
        }
    }
}
#endif
