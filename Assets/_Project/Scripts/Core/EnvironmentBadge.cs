using UnityEngine;

namespace VamosAprendiendo.Core
{
    /// <summary>
    /// Insignia de pruebas: muestra en una esquina contra qué backend está hablando el juego (LOCAL o PRODUCCIÓN y su dirección).
    /// Solo aparece en el Editor y en compilaciones de desarrollo; en la compilación final no existe.
    /// </summary>
    public class EnvironmentBadge : MonoBehaviour
    {
        private static EnvironmentBadge _instance;
        private GUIStyle _style;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (_instance != null || !(Application.isEditor || Debug.isDebugBuild)) return;
            var go = new GameObject("EnvironmentBadge");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<EnvironmentBadge>();
        }

        private void OnGUI()
        {
            var api = ApiClient.Instance;
            if (api == null) return;
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(11, Screen.height / 70), fontStyle = FontStyle.Bold };
                _style.normal.textColor = Color.white;
            }

            bool production = api.EnvironmentName == "PRODUCCIÓN";
            string text = $"Backend: {api.EnvironmentName}  {api.BaseUrl}";
            var size = _style.CalcSize(new GUIContent(text));
            var rect = new Rect(6f, Screen.height - size.y - 6f, size.x + 12f, size.y + 2f);
            var previous = GUI.color;
            GUI.color = production ? new Color(0.75f, 0.15f, 0.15f, 0.85f) : new Color(0.1f, 0.5f, 0.2f, 0.85f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
            GUI.Label(new Rect(rect.x + 6f, rect.y, rect.width, rect.height), text, _style);
        }
    }
}
