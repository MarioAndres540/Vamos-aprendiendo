using UnityEngine;
using System;
using System.Collections;

public class AuthService : MonoBehaviour
{
    public static AuthService Instance {get; private set;}

    /// <summary>Se dispara cuando el refresh falla: hay que pedir login de nuevo.</summary>
    public static event Action SessionExpired;

    private bool _refreshing;

    private void Awake()
    {
        if (Instance != null && Instance != this) {Destroy(gameObject); return;}
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // TEMPORAL (sin servidor): usuario de pruebas que entra sin validar contra el backend.
    // Quitar o proteger con #if UNITY_EDITOR || DEVELOPMENT_BUILD antes de publicar.
    private const string OfflineTestUser = "test";
    private const string OfflineTestPassword = "test1";

    public void Login(string email, string password, Action<LoginResponseDto> onSuccess, Action<string> onError)
    {
        if (string.Equals(email, OfflineTestUser, StringComparison.OrdinalIgnoreCase) && password == OfflineTestPassword)
        {
            var user = new UserDataDto { id = "offline-test", email = OfflineTestUser, role = "student", isActive = true };
            PlayerSession.Instance.SetOfflineSession(user);
            onSuccess?.Invoke(new LoginResponseDto { user = user });
            return;
        }

        var requestBody = new LoginRequestDto{email = email, password = password};

        StartCoroutine(ApiClient.Instance.Post<LoginRequestDto, LoginResponseDto>(
            "/auth/login",
            requestBody,
            (response) =>
            {
                PlayerSession.Instance.SetSession(response);
                onSuccess?.Invoke(response);
            },
            onError
        ));
    }

    /// <summary>
    /// Reabre la sesión con el refresh token guardado de un login anterior (la app abre con tokens guardados).
    /// Devuelve true si quedó con sesión válida.
    /// </summary>
    public IEnumerator RestoreSession(Action<bool> onDone)
    {
        var session = PlayerSession.Instance;
        if (session == null || session.IsLoggedIn || !session.TryLoadSavedRefreshToken())
        {
            onDone?.Invoke(session != null && session.IsLoggedIn);
            yield break;
        }

        yield return RefreshTokens();
        onDone?.Invoke(session.IsLoggedIn);
    }

    /// <summary>
    /// Renueva los tokens (POST /auth/refresh). Cada refresh devuelve un refreshToken nuevo y revoca el anterior,
    /// así que solo puede haber uno a la vez: las demás llamadas esperan y reutilizan el resultado.
    /// </summary>
    public IEnumerator RefreshTokens()
    {
        var session = PlayerSession.Instance;
        if (session == null || session.IsOfflineMode) yield break;

        string tokenAtEntry = session.RefreshToken;
        if (string.IsNullOrEmpty(tokenAtEntry)) yield break;

        // Otro refresh en curso: esperar y, si ya cambió el token, no repetir.
        while (_refreshing) yield return null;
        if (session.RefreshToken != tokenAtEntry) yield break;

        _refreshing = true;
        string body = new JsonBody().Add("refreshToken", session.RefreshToken).Build();

        yield return ApiClient.Instance.Send("POST", "/auth/refresh", body,
            onSuccess: raw =>
            {
                var tokens = JsonUtility.FromJson<TokenResponseDto>(raw);
                session.UpdateTokens(tokens.accessToken, tokens.refreshToken); // guardar siempre el último
            },
            onError: error =>
            {
                Debug.LogWarning($"[Auth] La sesión expiró, hay que iniciar sesión de nuevo: {error}");
                session.Clear();
                SessionExpired?.Invoke();
            },
            withAuth: false, allowRefresh: false);

        _refreshing = false;
    }

    /// <summary>
    /// Cierra la sesión: termina la sesión de app (motivo "logout"), avisa al backend (POST /auth/logout)
    /// y borra los tokens locales. Se puede lanzar y dejar correr durante un cambio de escena.
    /// </summary>
    public void Logout()
    {
        StartCoroutine(LogoutRoutine());
    }

    private IEnumerator LogoutRoutine()
    {
        var session = PlayerSession.Instance;
        if (session == null) yield break;

        if (PresenceService.Instance != null)
        {
            yield return PresenceService.Instance.EndAppSessionRoutine("logout");
        }

        if (!session.IsOfflineMode && !string.IsNullOrEmpty(session.RefreshToken))
        {
            string body = new JsonBody().Add("refreshToken", session.RefreshToken).Build();
            yield return ApiClient.Instance.Send("POST", "/auth/logout", body,
                onSuccess: _ => { },
                onError: error => Debug.LogWarning($"[Auth] logout en el servidor falló: {error}"),
                allowRefresh: false);
        }

        session.Clear();
    }
}
