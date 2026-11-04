using System;

/// <summary>Respuesta de POST /presence/sessions.</summary>
[Serializable]
public class StartSessionResponseDto
{
    public string sessionId;
    public string startedAt;
    public int heartbeatIntervalSeconds;
    public int timeoutSeconds;
}

/// <summary>
/// Respuesta de POST /auth/refresh: tokens nuevos y el perfil recalculado. Al reabrir la app con el token guardado
/// es la única fuente de profileType y audience (el login no se repite).
/// </summary>
[Serializable]
public class TokenResponseDto
{
    public string accessToken;
    public string refreshToken;
    public int expiresIn;
    public string profileType;
    public string audience;
    public UserDataDto user;
}

/// <summary>Error de una petición al backend, con el código HTTP y el `code` del backend si lo trae.</summary>
public struct ApiError
{
    public long status;          // 0 = sin respuesta (red caída, timeout)
    public string code;          // p. ej. SESSION_ENDED
    public string message;
    public bool isNetworkError;

    public override string ToString() => $"[{status}{(string.IsNullOrEmpty(code) ? "" : " " + code)}] {message}";
}
