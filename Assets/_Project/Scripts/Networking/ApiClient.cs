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
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(string.IsNullOrEmpty(json) ? "{}" : json)),
            downloadHandler = new DownloadHandlerBuffer(),
            timeout = timeoutSeconds
        };
        request.SetRequestHeader("Content-Type", "application/json");

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
