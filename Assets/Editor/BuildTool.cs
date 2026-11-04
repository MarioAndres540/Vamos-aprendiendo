using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace VamosAprendiendo.EditorTools
{
    /// <summary>
    /// Compilaciones para la entrega (Windows y Android) apuntando al backend publicado.
    /// 1) Copia la dirección pública del backend al portapapeles y ejecuta "Set Backend URL from Clipboard".
    /// 2) Ejecuta "Build Windows" y/o "Build Android APK". Los resultados quedan en Builds/.
    /// Las escenas son las de File > Build Profiles (el menú principal, el mundo y los retos de trazo).
    /// </summary>
    public static class BuildTool
    {
        private const string UrlFile = "Assets/Resources/api_base_url.txt";

        [MenuItem("Tools/Build/Set Backend URL from Clipboard")]
        public static void SetBackendUrl()
        {
            string url = GUIUtility.systemCopyBuffer?.Trim().TrimEnd('/');
            if (string.IsNullOrEmpty(url) || !(url.StartsWith("http://") || url.StartsWith("https://")))
            {
                Debug.LogError("[Build] El portapapeles no tiene una dirección http(s). Ejemplo: https://api.midominio.com/api/v1");
                return;
            }
            if (url.StartsWith("http://") && !url.Contains("localhost"))
                Debug.LogWarning("[Build] La dirección es http (sin cifrar): Android bloquea ese tráfico por defecto. Usa https en producción.");
            Directory.CreateDirectory("Assets/Resources");
            File.WriteAllText(UrlFile, url + "\n");
            AssetDatabase.ImportAsset(UrlFile);
            Debug.Log($"[Build] Backend de las compilaciones: {url}");
        }

        [MenuItem("Tools/Build/Clear Backend URL (use scene value)")]
        public static void ClearBackendUrl()
        {
            AssetDatabase.DeleteAsset(UrlFile);
            Debug.Log("[Build] Sin dirección configurada: las compilaciones usan la de la escena (localhost).");
        }

        [MenuItem("Tools/Build/Build Windows")]
        public static void BuildWindows() =>
            Build(BuildTarget.StandaloneWindows64, "Builds/Windows/VamosAprendiendo.exe");

        [MenuItem("Tools/Build/Build Android APK")]
        public static void BuildAndroid() =>
            Build(BuildTarget.Android, "Builds/VamosAprendiendo.apk");

        private static void Build(BuildTarget target, string output)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (!File.Exists(UrlFile))
                Debug.LogWarning("[Build] No hay dirección de backend configurada: la compilación apuntará a localhost.");

            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var options = new BuildPlayerOptions { scenes = scenes, locationPathName = output, target = target, options = BuildOptions.None };
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
                Debug.Log($"[Build] {target}: listo en {output} ({summary.totalSize / (1024 * 1024)} MB, {summary.totalTime.TotalMinutes:F1} min)");
            else
                Debug.LogError($"[Build] {target}: {summary.result} ({summary.totalErrors} errores)");
        }
    }
}
