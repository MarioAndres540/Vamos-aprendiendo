using UnityEngine;

public class PlayerSession : MonoBehaviour
{
    private const string RefreshTokenKey = "refreshToken";

    public static PlayerSession Instance { get; private set; }

    public string AccessToken { get; private set; }
    public string RefreshToken { get; private set; }
    public UserDataDto CurrentUser { get; private set; }
    public string ProfileType { get; private set; }
    public ProfileDataDto Profile { get; private set; }
    public bool IsOfflineMode { get; private set; }
    public bool IsLoggedIn => IsOfflineMode || !string.IsNullOrEmpty(AccessToken);

    /// <summary>Hijo que juega (opcional). Se envía como studentId al abrir la sesión de app.</summary>
    public string CurrentStudentId { get; set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>Sesión local sin backend (solo desarrollo): sin tokens y sin llamadas de red.</summary>
    public void SetOfflineSession(UserDataDto user)
    {
        AccessToken = null;
        RefreshToken = null;
        CurrentUser = user;
        IsOfflineMode = true;
    }

    public void SetSession(LoginResponseDto response)
    {
        IsOfflineMode = false;
        AccessToken = response.accessToken;
        RefreshToken = response.refreshToken;
        CurrentUser = response.user;
        ProfileType = response.profileType;
        Profile = response.profile;

        // Persistencia mínima para poder renovar sesión más adelante (en producción, almacenamiento seguro).
        SaveRefreshToken();
    }

    /// <summary>Recupera el refresh token guardado de un login anterior (para reabrir sesión sin pedir login).</summary>
    public bool TryLoadSavedRefreshToken()
    {
        string saved = PlayerPrefs.GetString(RefreshTokenKey, null);
        if (string.IsNullOrEmpty(saved)) return false;
        RefreshToken = saved;
        return true;
    }

    /// <summary>Guarda los tokens nuevos tras un refresh (el refresh token anterior queda revocado).</summary>
    public void UpdateTokens(string accessToken, string refreshToken)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        SaveRefreshToken();
    }

    /// <summary>Borra la sesión y los tokens guardados (cierre de sesión o sesión expirada).</summary>
    public void Clear()
    {
        AccessToken = null;
        RefreshToken = null;
        CurrentUser = null;
        ProfileType = null;
        Profile = null;
        CurrentStudentId = null;
        IsOfflineMode = false;
        PlayerPrefs.DeleteKey(RefreshTokenKey);
        PlayerPrefs.Save();
    }

    private void SaveRefreshToken()
    {
        if (string.IsNullOrEmpty(RefreshToken)) return;
        PlayerPrefs.SetString(RefreshTokenKey, RefreshToken);
        PlayerPrefs.Save();
    }
}
