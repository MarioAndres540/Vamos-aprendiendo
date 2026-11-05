using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ApiClient : MonoBehaviour
{
    public static ApiClient Instance { get; private set; }

    [Header("Configuración de entorno")]
    [Tooltip("En un celular físico usar la IP del equipo, no localhost")]
    [SerializeField] private string baseUrl = "http://localhost:3000/api/v1";
    [SerializeField] private int timeoutSeconds = 10;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        ApplyConfiguredUrl();
    }

    /// <summary>Clave (EditorPrefs) del entorno elegido en el Editor: 0 = local, 1 = producción. Ver Tools/Backend.</summary>
    public const string EditorEnvironmentKey = "Vamos.BackendEnvironment";

    /// <summary>Dirección y entorno en uso, para mostrarlos (por ejemplo en la insignia de pruebas).</summary>
    public string BaseUrl => baseUrl;
    public string EnvironmentName { get; private set; } = "escena";

    /// <summary>
    /// Elige el backend. En una compilación se usa la dirección de Resources/api_base_url.txt (el servidor publicado) si existe, y si
    /// no la de la escena. En el Editor se elige con Tools/Backend: Local (la dirección de la escena, localhost) o Producción (la del
    /// archivo), para validar contra los dos antes de compilar. El archivo se crea con Tools/Build/Set Backend URL from Clipboard.
    /// </summary>
    private void ApplyConfiguredUrl()
    {
#if UNITY_EDITOR
        if (UnityEditor.EditorPrefs.GetInt(EditorEnvironmentKey, 0) == 0)
        {
            EnvironmentName = "LOCAL";
            Debug.Log($"[Api] Entorno LOCAL: {baseUrl}");
            return;
        }
#endif
        var config = Resources.Load<TextAsset>("api_base_url");
        if (config == null)
        {
            Debug.Log($"[Api] Entorno (escena): {baseUrl}");
            return;
        }
        string url = config.text.Trim().TrimEnd('/');
        if (url.StartsWith("http://") || url.StartsWith("https://"))
        {
            baseUrl = url;
            EnvironmentName = "PRODUCCIÓN";
            Debug.Log($"[Api] Entorno PRODUCCIÓN: {baseUrl}");
            // Un servidor publicado en un plan gratuito se duerme y tarda en despertar: se le avisa de entrada, sin esperar la respuesta
            StartCoroutine(WarmUp());
        }
        else Debug.LogWarning("[Api] api_base_url.txt no trae una dirección http(s) válida: se ignora.");
    }

    private IEnumerator WarmUp()
    {
        using (var request = UnityWebRequest.Get(baseUrl))
        {
            request.timeout = 60;
            yield return request.SendWebRequest();
        }
    }

    /// <summary>POST con cuerpo serializado por JsonUtility y respuesta tipada (login, etc.).</summary>
    public IEnumerator Post<TRequest, TResponse>(
        string endpoint, TRequest body,
        Action<TResponse> onSuccess, Action<string> onError)
    {
        string json = JsonUtility.ToJson(body);

        yield return Send("POST", endpoint, json,
            onSuccess: raw =>
            {
                try
                {
                    onSuccess?.Invoke(JsonUtility.FromJson<TResponse>(raw));
                }
                catch (Exception e)
                {
                    onError?.Invoke($"Error al procesar la respuesta: {e.Message}");
                }
            },
            onError: error => onError?.Invoke(string.IsNullOrEmpty(error.message) ? $"Error {error.status}" : error.message));
    }

    /// <summary>
    /// Petición genérica con JSON ya construido. Añade el token si existe y, ante un 401, refresca los tokens
    /// una vez y reintenta (salvo en los endpoints /auth/*). Devuelve el JSON crudo o un <see cref="ApiError"/>.
    /// </summary>
    public IEnumerator Send(
        string method, string endpoint, string json,
        Action<string> onSuccess, Action<ApiError> onError,
        bool withAuth = true, bool allowRefresh = true)
    {
        bool isAuthEndpoint = endpoint.StartsWith("/auth/");
        bool refreshedOnce = false;

        while (true)
        {
            using (UnityWebRequest request = BuildRequest(method, endpoint, json, withAuth))
            {
                yield return request.SendWebRequest();

                long status = request.responseCode;
                bool canRefresh = status == 401 && withAuth && allowRefresh && !isAuthEndpoint && !refreshedOnce
                                  && PlayerSession.Instance != null && !string.IsNullOrEmpty(PlayerSession.Instance.RefreshToken)
                                  && AuthService.Instance != null;

                if (canRefresh)
                {
                    refreshedOnce = true;
                    yield return AuthService.Instance.RefreshTokens();
                    if (PlayerSession.Instance.IsLoggedIn) continue; // tokens nuevos: se reintenta una vez
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    onSuccess?.Invoke(request.downloadHandler.text);
                    yield break;
                }

                onError?.Invoke(BuildError(request));
                yield break;
            }
        }
    }

    private UnityWebRequest BuildRequest(string method, string endpoint, string json, bool withAuth)
    {
        var request = new UnityWebRequest($"{baseUrl}{endpoint}", method)
        {
            downloadHandler = new DownloadHandlerBuffer(),
            // El backend gratuito de Render se duerme tras 15 min sin uso y la primera petición tarda ~1 min:
            // contra un servidor público se espera más de lo que dice la escena (10 s) para no fallar en frío.
            timeout = baseUrl.Contains("localhost") ? timeoutSeconds : Mathf.Max(timeoutSeconds, 75)
        };

        // GET sin cuerpo: en Android e iOS una petición GET con cuerpo puede fallar
        if (method != UnityWebRequest.kHttpVerbGET)
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(string.IsNullOrEmpty(json) ? "{}" : json));
            request.SetRequestHeader("Content-Type", "application/json");
        }

        if (withAuth && PlayerSession.Instance != null && !string.IsNullOrEmpty(PlayerSession.Instance.AccessToken))
        {
            request.SetRequestHeader("Authorization", $"Bearer {PlayerSession.Instance.AccessToken}");
        }
        return request;
    }

    private ApiError BuildError(UnityWebRequest request)
    {
        bool network = request.result == UnityWebRequest.Result.ConnectionError;
        var error = new ApiError
        {
            status = request.responseCode,
            isNetworkError = network,
            message = request.error
        };

        string raw = request.downloadHandler != null ? request.downloadHandler.text : null;
        if (!string.IsNullOrEmpty(raw))
        {
            try
            {
                var dto = JsonUtility.FromJson<NestErrorDto>(raw);
                if (dto != null)
                {
                    if (!string.IsNullOrEmpty(dto.message)) error.message = dto.message;
                    error.code = dto.code;
                }
            }
            catch { /* respuesta que no es JSON: se queda el texto de UnityWebRequest */ }
        }
        return error;
    }
}
