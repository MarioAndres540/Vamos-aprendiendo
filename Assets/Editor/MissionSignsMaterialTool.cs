#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VamosAprendiendo.EditorTools
{
    /// <summary>
    /// Crea los materiales de plástico brillante (como bloques de armar) de los signos de misión "!" y "?" y se los asigna
    /// a sus modelos. Las texturas salen de los propios modelos: el .glb de la "!" trae color, normales y rugosidad, y
    /// el .fbx de la "?" trae color y normales embebidos (se extrajeron a Art/Textures/MisionSigns).
    /// Se puede ejecutar varias veces: actualiza los materiales sin duplicarlos.
    /// </summary>
    public static class MissionSignsMaterialTool
    {
        private const string TEX_DIR = "Assets/_Project/Art/Textures/MisionSigns/";
        private const string MAT_DIR = "Assets/_Project/Art/Materials/MisionSigns/";
        private const string EXCLAMATION_MODEL = "Assets/_Project/Prefabs/UI/send mission.fbx";
        private const string QUESTION_MODEL = "Assets/_Project/Prefabs/UI/finish_mision.fbx";

        // Plástico ABS: sin metal y bastante liso (la rugosidad del .glb de la "!" es ~0,3 → brillo 0,7)
        private const float SMOOTHNESS = 0.72f;
        // Los relieves de las piezas vienen muy marcados en el mapa de normales: se suavizan un poco
        private const float BUMP_SCALE = 0.8f;

        [MenuItem("Tools/Art/Create Mission Signs Materials (! and ?)", false, 30)]
        public static void Run()
        {
            ConfigureTexture(TEX_DIR + "Exclamacion_BaseColor.jpg", false);
            ConfigureTexture(TEX_DIR + "Exclamacion_Normal.jpg", true);
            ConfigureTexture(TEX_DIR + "Interrogacion_BaseColor.png", false);
            ConfigureTexture(TEX_DIR + "Interrogacion_Normal.png", true);

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Art/Materials/MisionSigns"))
                AssetDatabase.CreateFolder("Assets/_Project/Art/Materials", "MisionSigns");

            var exclamation = CreateMaterial("M_Signo_Exclamacion", "Exclamacion_BaseColor.jpg", "Exclamacion_Normal.jpg");
            var question = CreateMaterial("M_Signo_Interrogacion", "Interrogacion_BaseColor.png", "Interrogacion_Normal.png");

            AssignToModel(EXCLAMATION_MODEL, exclamation);
            AssignToModel(QUESTION_MODEL, question);
            AssetDatabase.SaveAssets();
            Debug.Log("[MissionSigns] Materiales creados y asignados a los modelos de la ! y la ?.");
        }

        private static void ConfigureTexture(string path, bool isNormal)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { Debug.LogError("[MissionSigns] No se encontró " + path); return; }

            importer.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (!isNormal) importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.anisoLevel = 4;
            // Los originales miden hasta 2560 px: 2048 basta para un objeto de este tamaño y ahorra memoria en el celular
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.crunchedCompression = false;
            importer.SaveAndReimport();
        }

        private static Material CreateMaterial(string name, string baseColor, string normal)
        {
            string path = MAT_DIR + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_DIR + baseColor));
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_DIR + normal));
            material.SetFloat("_BumpScale", BUMP_SCALE);
            material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", SMOOTHNESS);
            EditorUtility.SetDirty(material);
            return material;
        }

        // Cambia el material del modelo (todas sus instancias en escenas y prefabs lo usan) sin tocar ninguna escena
        private static void AssignToModel(string modelPath, Material material)
        {
            var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer == null) { Debug.LogError("[MissionSigns] No se encontró " + modelPath); return; }

            var embedded = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Material>().FirstOrDefault(m => m != material);
            string sourceName = embedded != null ? embedded.name : null;
            if (sourceName == null)
            {
                // Ya estaba reasignado: se reutiliza el nombre que tiene el remapeo existente
                var existing = importer.GetExternalObjectMap().FirstOrDefault(kv => kv.Key.type == typeof(Material));
                sourceName = existing.Key.name;
            }
            if (string.IsNullOrEmpty(sourceName)) { Debug.LogError("[MissionSigns] El modelo no tiene material que reemplazar: " + modelPath); return; }

            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), sourceName), material);
            importer.SaveAndReimport();
        }
    }
}
#endif
