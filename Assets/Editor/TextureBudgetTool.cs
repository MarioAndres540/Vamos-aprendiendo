using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VamosAprendiendo.EditorTools
{
    /// <summary>
    /// Presupuesto de texturas para la entrega (máximo 1024 x 1024). Baja el tamaño máximo de importación a 1024 en las texturas de
    /// modelos 3D y entorno de _Project y _ThirdParty. No toca lo que se vería mal más pequeño: sprites de interfaz, cielos
    /// (Skybox), iconos y cursores. Es solo un límite de importación: los archivos originales no se modifican y se puede revertir
    /// con "Restore Texture Limits" (vuelve a 2048, el valor que tenían).
    /// </summary>
    public static class TextureBudgetTool
    {
        private const int Limit = 1024;
        private const int Original = 2048;

        [MenuItem("Tools/Optimization/Textures - Report (dry run)")]
        public static void Report() => Run(apply: false, restore: false);

        [MenuItem("Tools/Optimization/Textures - Cap to 1024")]
        public static void Cap() => Run(apply: true, restore: false);

        [MenuItem("Tools/Optimization/Textures - Restore Texture Limits (2048)")]
        public static void Restore() => Run(apply: true, restore: true);

        private static bool Skip(TextureImporter importer, string path)
        {
            if (importer.textureType == TextureImporterType.Sprite || importer.textureType == TextureImporterType.Cursor
                || importer.textureType == TextureImporterType.Lightmap || importer.textureType == TextureImporterType.GUI) return true;
            if (importer.textureShape == TextureImporterShape.TextureCube) return true;
            string p = path.ToLowerInvariant();
            return p.Contains("skybox") || p.Contains("/resources/") || p.Contains("/art/images/") || p.Contains("/ui/") || p.Contains("icon");
        }

        private static void Run(bool apply, bool restore)
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Project", "Assets/_ThirdParty" });
            var changed = new List<string>();
            int skipped = 0, alreadySmall = 0;

            if (apply) AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) continue;
                    if (Skip(importer, path)) { skipped++; continue; }

                    int target = restore ? Original : Limit;
                    int current = importer.maxTextureSize;
                    if (restore ? current >= target : current <= target) { alreadySmall++; continue; }

                    // Una textura cuyo archivo ya mide 1024 o menos no cambia con el tope: no se reimporta por gusto
                    if (!restore)
                    {
                        importer.GetSourceTextureWidthAndHeight(out int w, out int h);
                        if (Mathf.Max(w, h) <= Limit && current >= Limit) { alreadySmall++; continue; }
                    }

                    changed.Add(path);
                    if (!apply) continue;
                    importer.maxTextureSize = target;
                    importer.SaveAndReimport();
                }
            }
            finally
            {
                if (apply) AssetDatabase.StopAssetEditing();
            }

            Debug.Log($"[Textures] {(apply ? (restore ? "Restauradas" : "Limitadas a 1024") : "Se limitarían a 1024")}: {changed.Count} · " +
                      $"sin cambio: {alreadySmall} · excluidas (interfaz, cielos, iconos): {skipped} · total revisadas: {guids.Length}");
        }
    }
}
