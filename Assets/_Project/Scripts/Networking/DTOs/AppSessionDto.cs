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

/// <summary>Respuesta de POST /auth/refresh (mismos campos de tokens que el login).</summary>
[Serializable]
public class TokenResponseDto
{
    public string accessToken;
    public string refreshToken;
    public int expiresIn;
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
