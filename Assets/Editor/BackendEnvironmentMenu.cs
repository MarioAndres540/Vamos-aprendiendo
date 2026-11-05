using UnityEditor;
using UnityEngine;

namespace VamosAprendiendo.EditorTools
{
    /// <summary>
    /// Elige contra qué backend corre el juego dentro del Editor, para validarlo en los dos antes de generar el APK:
    /// · Local: la dirección de la escena (http://localhost:3000/api/v1). Arranca el backend con <c>npm run start:dev</c> en la carpeta backend.
    /// · Producción: la de Resources/api_base_url.txt (el servidor publicado), la misma que usarán las compilaciones.
    /// La elección se recuerda y se ve en la insignia de la esquina inferior izquierda al jugar. No afecta a las compilaciones.
    /// </summary>
    public static class BackendEnvironmentMenu
    {
        private const string LocalMenu = "Tools/Backend/Editor usa backend LOCAL (localhost)";
        private const string ProductionMenu = "Tools/Backend/Editor usa backend de PRODUCCIÓN";
        private const string UrlFile = "Assets/Resources/api_base_url.txt";

        private static int Current => EditorPrefs.GetInt(ApiClient.EditorEnvironmentKey, 0);

        [MenuItem(LocalMenu, false, 1)]
        private static void UseLocal()
        {
            EditorPrefs.SetInt(ApiClient.EditorEnvironmentKey, 0);
            Debug.Log("[Backend] El Editor usará el backend LOCAL (localhost:3000). Arráncalo con 'npm run start:dev' en la carpeta backend.");
        }

        [MenuItem(LocalMenu, true)]
        private static bool UseLocalValidate()
        {
            Menu.SetChecked(LocalMenu, Current == 0);
            return true;
        }

        [MenuItem(ProductionMenu, false, 2)]
        private static void UseProduction()
        {
            var file = AssetDatabase.LoadAssetAtPath<TextAsset>(UrlFile);
            if (file == null)
            {
                Debug.LogError("[Backend] Falta Assets/Resources/api_base_url.txt con la dirección de producción (Tools/Build/Set Backend URL from Clipboard).");
                return;
            }
            EditorPrefs.SetInt(ApiClient.EditorEnvironmentKey, 1);
            Debug.Log($"[Backend] El Editor usará el backend de PRODUCCIÓN: {file.text.Trim()}");
        }

        [MenuItem(ProductionMenu, true)]
        private static bool UseProductionValidate()
        {
            Menu.SetChecked(ProductionMenu, Current == 1);
            return true;
        }
    }
}
